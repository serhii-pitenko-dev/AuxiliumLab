using Microsoft.AspNetCore.SignalR;

namespace AuxiliumLab.AiSandbox.WebApi.Features.Simulation;

/// <summary>
/// Forwards simulation events to SignalR clients scoped to the correct job group.
/// </summary>
public sealed class SimulationHubNotifier : ISimulationHubNotifier
{
    private readonly IHubContext<SimulationHub> _hub;

    public SimulationHubNotifier(IHubContext<SimulationHub> hub)
        => _hub = hub;

    public Task NotifySimulationStartedAsync(SimulationStartedDto dto, CancellationToken ct = default)
        => _hub.Clients.Group(dto.JobId)
               .SendAsync(SimulationHub.Methods.SimulationStarted, dto, ct);

    public Task NotifyAgentMovedAsync(SimulationAgentMovedNotification n, CancellationToken ct = default)
        => _hub.Clients.Group(n.JobId)
               .SendAsync(SimulationHub.Methods.AgentMoved,
                   new AgentMovedDto { JobId = n.JobId, AgentId = n.AgentId, AgentType = n.AgentType,
                       From = n.From, To = n.To,
                       IsSuccess = n.IsSuccess, Agent = n.Agent, UpdatedCells = n.UpdatedCells }, ct);

    public Task NotifyAgentToggledAsync(SimulationAgentToggledNotification n, CancellationToken ct = default)
        => _hub.Clients.Group(n.JobId)
               .SendAsync(SimulationHub.Methods.AgentToggled,
                   new AgentToggledDto { JobId = n.JobId, AgentId = n.AgentId, AgentType = n.AgentType,
                       Action = n.Action, IsActivated = n.IsActivated, Agent = n.Agent }, ct);

    public Task NotifyTurnCompletedAsync(SimulationTurnCompletedNotification n, CancellationToken ct = default)
        => _hub.Clients.Group(n.JobId)
               .SendAsync(SimulationHub.Methods.TurnCompleted,
                   new TurnCompletedDto { JobId = n.JobId, TurnNumber = n.TurnNumber, UpdatedCells = n.UpdatedCells }, ct);

    public Task NotifySimulationEndedAsync(SimulationEndedDto dto, CancellationToken ct = default)
        => _hub.Clients.Group(dto.JobId)
               .SendAsync(SimulationHub.Methods.SimulationEnded, dto, ct);
}
