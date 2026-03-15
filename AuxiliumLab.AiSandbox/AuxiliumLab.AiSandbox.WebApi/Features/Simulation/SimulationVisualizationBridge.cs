using AuxiliumLab.AiSandbox.ApplicationServices.Converters.Maps;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Maps;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Lost;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Win;
using AuxiliumLab.AiSandbox.Common.SimulationVisualizationBridge;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace AuxiliumLab.AiSandbox.WebApi.Features.Simulation;

/// <summary>
/// Subscribes to global broker events while a single-simulation job is running
/// and forwards them to <see cref="ISimulationHubNotifier"/> so the Blazor
/// frontend receives live grid updates.
/// </summary>
public sealed class SimulationVisualizationBridge : ISimulationVisualizationBridge
{
    private readonly IMessageBroker _broker;
    private readonly ISimulationHubNotifier _notifier;
    private readonly IMapQueriesHandleService _mapQueries;
    private readonly IOptions<SandBoxConfiguration> _sandboxConfig;

    // Jobs waiting for their first GameStartedEvent to fire
    private readonly ConcurrentQueue<Guid> _pendingAttachments = new();
    // Maps PlaygroundId → JobId while a simulation is running
    private readonly ConcurrentDictionary<Guid, Guid> _playgroundToJob = new();

    private int _attachedCount;

    // Stable delegates stored once so we can pass the same reference to Unsubscribe.
    private readonly Action<GameStartedEvent>        _onGameStarted;
    private readonly Action<OnAgentMoveActionEvent>  _onAgentMoved;
    private readonly Action<OnAgentToggleActionEvent> _onAgentToggled;
    private readonly Action<TurnExecutedEvent>        _onTurnExecuted;
    private readonly Action<HeroWonEvent>             _onHeroWon;
    private readonly Action<HeroLostEvent>            _onHeroLost;

    public SimulationVisualizationBridge(
        IMessageBroker broker,
        ISimulationHubNotifier notifier,
        IMapQueriesHandleService mapQueries,
        IOptions<SandBoxConfiguration> sandboxConfig)
    {
        _broker        = broker;
        _notifier      = notifier;
        _mapQueries    = mapQueries;
        _sandboxConfig = sandboxConfig;

        _onGameStarted  = HandleGameStarted;
        _onAgentMoved   = HandleAgentMoved;
        _onAgentToggled = HandleAgentToggled;
        _onTurnExecuted = HandleTurnExecuted;
        _onHeroWon      = HandleHeroWon;
        _onHeroLost     = HandleHeroLost;
    }

    // ── ISimulationVisualizationBridge ──────────────────────────────────────

    public void Attach(Guid jobId)
    {
        _pendingAttachments.Enqueue(jobId);
        if (Interlocked.Increment(ref _attachedCount) == 1)
        {
            _broker.Subscribe(_onGameStarted);
            _broker.Subscribe(_onAgentMoved);
            _broker.Subscribe(_onAgentToggled);
            _broker.Subscribe(_onTurnExecuted);
            _broker.Subscribe(_onHeroWon);
            _broker.Subscribe(_onHeroLost);
        }
    }

    public void Detach(Guid jobId)
    {
        // Remove playground mapping for this job
        foreach (var key in _playgroundToJob
            .Where(kv => kv.Value == jobId)
            .Select(kv => kv.Key)
            .ToList())
        {
            _playgroundToJob.TryRemove(key, out _);
        }

        if (Interlocked.Decrement(ref _attachedCount) <= 0)
        {
            _attachedCount = 0;
            _broker.Unsubscribe(_onGameStarted);
            _broker.Unsubscribe(_onAgentMoved);
            _broker.Unsubscribe(_onAgentToggled);
            _broker.Unsubscribe(_onTurnExecuted);
            _broker.Unsubscribe(_onHeroWon);
            _broker.Unsubscribe(_onHeroLost);
        }
    }

    // ── Broker event handlers ───────────────────────────────────────────────

    private void HandleGameStarted(GameStartedEvent e)
    {
        if (!_pendingAttachments.TryDequeue(out var jobId)) return;

        _playgroundToJob[e.PlaygroundId] = jobId;

        var layout = _mapQueries.MapLayoutQuery.GetFromMemory(e.PlaygroundId);
        var cells  = BuildAllCells(layout.Cells);
        var dto    = new SimulationStartedDto(
            jobId.ToString(),
            layout.Cells.GetLength(0),
            layout.Cells.GetLength(1),
            _sandboxConfig.Value.MaxTurns.Current,
            cells);

        _ = Task.Run(() => _notifier.NotifySimulationStartedAsync(dto));
    }

    private void HandleAgentMoved(OnAgentMoveActionEvent e)
    {
        if (!_playgroundToJob.TryGetValue(e.PlaygroundId, out var jobId)) return;

        var notification = new SimulationAgentMovedNotification(
            jobId.ToString(),
            e.AgentId.ToString(),
            e.AgentSnapshot.Type.ToString(),
            e.From.X, e.From.Y,
            e.To.X,   e.To.Y,
            e.IsSuccess,
            ToSnapshotDto(e.AgentSnapshot));

        _ = Task.Run(() => _notifier.NotifyAgentMovedAsync(notification));
    }

    private void HandleAgentToggled(OnAgentToggleActionEvent e)
    {
        if (!_playgroundToJob.TryGetValue(e.PlaygroundId, out var jobId)) return;

        var notification = new SimulationAgentToggledNotification(
            jobId.ToString(),
            e.AgentId.ToString(),
            e.AgentSnapshot.Type.ToString(),
            e.AgentAction.ToString(),
            e.IsActivated,
            ToSnapshotDto(e.AgentSnapshot));

        _ = Task.Run(() => _notifier.NotifyAgentToggledAsync(notification));
    }

    private void HandleTurnExecuted(TurnExecutedEvent e)
    {
        if (!_playgroundToJob.TryGetValue(e.PlaygroundId, out var jobId)) return;

        var layout = _mapQueries.MapLayoutQuery.GetFromMemory(e.PlaygroundId);
        var cells  = BuildAllCells(layout.Cells);
        var notification = new SimulationTurnCompletedNotification(
            jobId.ToString(),
            e.TurnNumber,
            cells);

        _ = Task.Run(() => _notifier.NotifyTurnCompletedAsync(notification));
    }

    private void HandleHeroWon(HeroWonEvent e)
    {
        if (!_playgroundToJob.TryGetValue(e.PlaygroundId, out var jobId)) return;

        var dto = new SimulationEndedDto(
            jobId.ToString(),
            "Won",
            e.WinReason.ToString(),
            0); // final turn not available here — clients already know it from last TurnCompleted

        _ = Task.Run(() => _notifier.NotifySimulationEndedAsync(dto));
    }

    private void HandleHeroLost(HeroLostEvent e)
    {
        if (!_playgroundToJob.TryGetValue(e.PlaygroundId, out var jobId)) return;

        var dto = new SimulationEndedDto(
            jobId.ToString(),
            "Lost",
            e.LostReason.ToString(),
            0);

        _ = Task.Run(() => _notifier.NotifySimulationEndedAsync(dto));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static SimulationCellDto[] BuildAllCells(
        ApplicationServices.Queries.Simulation.Map.Entities.MapCell[,] cells)
    {
        int w = cells.GetLength(0);
        int h = cells.GetLength(1);
        var result = new SimulationCellDto[w * h];
        int i = 0;
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var cell    = cells[x, y];
            var effects = cell.Effects
                .SelectMany(ae => ae.Effects.Select(ef => $"{ae.AgentType}:{ef}"))
                .ToArray();
            result[i++] = new SimulationCellDto(x, y, cell.ObjectType.ToString(), effects);
        }
        return result;
    }

    private static AgentSnapshotDto ToSnapshotDto(
        SharedBaseTypes.ValueObjects.AgentSnapshot s) =>
        new(s.Id.ToString(),
            s.Type.ToString(),
            s.Speed,
            s.SightRange,
            s.IsRun,
            s.Stamina,
            s.MaxStamina,
            s.OrderInTurnQueue);
}
