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
    /// If the simulation already started before this client connected, the cached
    /// initial state is replayed directly to this client so it never misses it.
    /// </summary>
    public async Task JoinSimulation(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, jobId);

        // Replay initial state for clients that connected after SimulationStarted was broadcast
        var cached = _stateCache.GetCachedStart(jobId);
        if (cached is not null)
            await Clients.Caller.SendAsync(Methods.SimulationStarted, cached);
    }

    /// <summary>Leaves the group for a specific simulation job.</summary>
    public async Task LeaveSimulation(string jobId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobId);
}
