using Microsoft.AspNetCore.SignalR;

namespace AuxiliumLab.AiSandbox.WebApi.Features.Simulation;

/// <summary>
/// SignalR hub for real-time simulation visualization.
/// Clients connect and join a group named after the simulation job ID to receive events scoped to that run.
/// </summary>
public sealed class SimulationHub : Hub
{
    private readonly ISimulationStateCache _stateCache;
    private readonly ISimulationHubNotifier _notifier;

    public SimulationHub(ISimulationStateCache stateCache, ISimulationHubNotifier notifier)
    {
        _stateCache = stateCache;
        _notifier   = notifier;
    }

    /// <summary>Client method name constants — used by both hub and Blazor client.</summary>
    public static class Methods
    {
        public const string SimulationStarted = nameof(SimulationStarted);
        public const string AgentMoved        = nameof(AgentMoved);
        public const string AgentToggled      = nameof(AgentToggled);
        public const string TurnCompleted     = nameof(TurnCompleted);
        public const string SimulationEnded   = nameof(SimulationEnded);
    }

    /// <summary>
    /// Joins the group for a specific simulation job.
    /// If the simulation already started (or even ended) before this client connected,
    /// the cached snapshots are replayed so the client always sees the grid and outcome.
    /// </summary>
    public async Task JoinSimulation(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, jobId);

        // Replay initial state for clients that connected after SimulationStarted was broadcast
        var cachedStart = _stateCache.GetCachedStart(jobId);
        if (cachedStart is not null)
            await Clients.Caller.SendAsync(Methods.SimulationStarted, cachedStart);

        // Replay final cell-grid snapshot so the board shows the last known layout
        var lastTurn = _stateCache.GetCachedLastTurn(jobId);
        if (lastTurn is not null)
            await Clients.Caller.SendAsync(Methods.TurnCompleted, lastTurn);

        // Replay each agent's last known move so circles render at the final position
        foreach (var agentMove in _stateCache.GetCachedLastAgentMoves(jobId))
            await Clients.Caller.SendAsync(Methods.AgentMoved, agentMove);

        // Also replay the final outcome if the simulation already ended
        var cachedEnd = _stateCache.GetCachedEnd(jobId);
        if (cachedEnd is not null)
            await Clients.Caller.SendAsync(Methods.SimulationEnded, cachedEnd);
    }

    /// <summary>Leaves the group for a specific simulation job.</summary>
    public async Task LeaveSimulation(string jobId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobId);
}
