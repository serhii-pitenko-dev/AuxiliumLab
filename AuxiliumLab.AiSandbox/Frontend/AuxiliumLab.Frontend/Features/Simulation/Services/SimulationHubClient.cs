using AuxiliumLab.Frontend.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace AuxiliumLab.Frontend.Features.Simulation.Services;

/// <summary>
/// Manages a SignalR connection to the backend <c>/hubs/simulation</c> hub.
/// Provides a typed event surface for the visualization component.
/// </summary>
public interface ISimulationHubClient : IAsyncDisposable
{
    event Action<SimulationStartedDto>? OnSimulationStarted;
    event Action<AgentMovedDto>?        OnAgentMoved;
    event Action<AgentToggledDto>?      OnAgentToggled;
    event Action<TurnCompletedDto>?     OnTurnCompleted;
    event Action<SimulationEndedDto>?   OnSimulationEnded;
    /// <summary>Fires for every raw SignalR message received, before the typed handler. Reports "[raw] {name} ok" or "[raw] {name} ERROR: {ex}".</summary>
    event Action<string>?               OnDebugMessage;

    Task ConnectAsync(string jobId, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class SimulationHubClient : ISimulationHubClient
{
    private readonly IApiContextProvider _context;
    private HubConnection? _connection;

    public event Action<SimulationStartedDto>? OnSimulationStarted;
    public event Action<AgentMovedDto>?        OnAgentMoved;
    public event Action<AgentToggledDto>?      OnAgentToggled;
    public event Action<TurnCompletedDto>?     OnTurnCompleted;
    public event Action<SimulationEndedDto>?   OnSimulationEnded;
    public event Action<string>?               OnDebugMessage;

    public SimulationHubClient(IApiContextProvider context)
        => _context = context;

    public async Task ConnectAsync(string jobId, CancellationToken ct = default)
    {
        if (_connection is not null)
            await DisconnectAsync(ct);

        _connection = new HubConnectionBuilder()
            .WithUrl(_context.SimulationHubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<SimulationStartedDto>("SimulationStarted", dto =>
        {
            try { OnSimulationStarted?.Invoke(dto); OnDebugMessage?.Invoke("[raw] SimulationStarted ok"); }
            catch (Exception ex) { OnDebugMessage?.Invoke($"[raw] SimulationStarted ERROR: {ex.Message}"); }
        });
        _connection.On<AgentMovedDto>("AgentMoved", dto =>
        {
            try { OnDebugMessage?.Invoke($"[raw] AgentMoved {dto?.AgentId?[..Math.Min(6, dto.AgentId?.Length ?? 0)]} ({dto?.From?.X},{dto?.From?.Y})→({dto?.To?.X},{dto?.To?.Y})"); OnAgentMoved?.Invoke(dto!); }
            catch (Exception ex) { OnDebugMessage?.Invoke($"[raw] AgentMoved ERROR: {ex.Message}"); }
        });
        _connection.On<AgentToggledDto>("AgentToggled", dto =>
        {
            try { OnAgentToggled?.Invoke(dto); }
            catch (Exception ex) { OnDebugMessage?.Invoke($"[raw] AgentToggled ERROR: {ex.Message}"); }
        });
        _connection.On<TurnCompletedDto>("TurnCompleted", dto =>
        {
            try { OnDebugMessage?.Invoke($"[raw] TurnCompleted turn={dto?.TurnNumber} cells={dto?.UpdatedCells?.Length}"); OnTurnCompleted?.Invoke(dto!); }
            catch (Exception ex) { OnDebugMessage?.Invoke($"[raw] TurnCompleted ERROR: {ex.Message}"); }
        });
        _connection.On<SimulationEndedDto>("SimulationEnded", dto =>
        {
            try { OnSimulationEnded?.Invoke(dto); OnDebugMessage?.Invoke("[raw] SimulationEnded ok"); }
            catch (Exception ex) { OnDebugMessage?.Invoke($"[raw] SimulationEnded ERROR: {ex.Message}"); }
        });

        await _connection.StartAsync(ct);
        await _connection.InvokeAsync("JoinSimulation", jobId, ct);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_connection is null) return;
        await _connection.StopAsync(ct);
        await _connection.DisposeAsync();
        _connection = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}
