using Microsoft.AspNetCore.SignalR;

namespace AuxiliumLab.AiSandbox.WebApi.Features.Simulation;

/// <summary>
/// SignalR hub for real-time simulation visualization.
/// Clients connect and join a group named after the simulation job ID to receive events scoped to that run.
/// </summary>
public sealed class SimulationHub : Hub
{
    /// <summary>Client method name constants — used by both hub and Blazor client.</summary>
    public static class Methods
    {
        public const string SimulationStarted = nameof(SimulationStarted);
        public const string AgentMoved        = nameof(AgentMoved);
        public const string AgentToggled      = nameof(AgentToggled);
        public const string TurnCompleted     = nameof(TurnCompleted);
        public const string SimulationEnded   = nameof(SimulationEnded);
    }

    /// <summary>Joins the group for a specific simulation job, so only relevant events are received.</summary>
    public async Task JoinSimulation(string jobId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, jobId);

    /// <summary>Leaves the group for a specific simulation job.</summary>
    public async Task LeaveSimulation(string jobId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobId);
}
