namespace AuxiliumLab.AiSandbox.WebApi.Features.Simulation;

/// <summary>
/// Broadcasts simulation lifecycle events to connected SignalR clients.
/// Implementations subscribe to the in-process message broker and forward events to the hub.
/// </summary>
public interface ISimulationHubNotifier
{
    Task NotifySimulationStartedAsync(SimulationStartedDto dto, CancellationToken ct = default);
    Task NotifyAgentMovedAsync(SimulationAgentMovedNotification notification, CancellationToken ct = default);
    Task NotifyAgentToggledAsync(SimulationAgentToggledNotification notification, CancellationToken ct = default);
    Task NotifyTurnCompletedAsync(SimulationTurnCompletedNotification notification, CancellationToken ct = default);
    Task NotifySimulationEndedAsync(SimulationEndedDto dto, CancellationToken ct = default);
}
