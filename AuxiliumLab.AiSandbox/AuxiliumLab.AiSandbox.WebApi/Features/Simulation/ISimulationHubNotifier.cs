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

/// <summary>Notification payload for agent move events.</summary>
public record SimulationAgentMovedNotification(
    string JobId,
    string AgentId,
    string AgentType,
    int FromX, int FromY,
    int ToX,   int ToY,
    bool IsSuccess,
    AgentSnapshotDto Agent,
    SimulationCellDto[] UpdatedCells);

/// <summary>Notification payload for agent toggle events.</summary>
public record SimulationAgentToggledNotification(
    string JobId,
    string AgentId,
    string AgentType,
    string Action,
    bool IsActivated,
    AgentSnapshotDto Agent);

/// <summary>Notification payload for turn-completed events.</summary>
public record SimulationTurnCompletedNotification(
    string JobId,
    int TurnNumber,
    SimulationCellDto[] UpdatedCells);
