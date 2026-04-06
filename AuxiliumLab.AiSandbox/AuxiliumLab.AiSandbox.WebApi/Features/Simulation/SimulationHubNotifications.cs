using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.WebApi.Features.Simulation;

/// <summary>Notification payload for agent move events.</summary>
public record SimulationAgentMovedNotification(
    string JobId,
    string AgentId,
    ObjectType AgentType,
    Coordinates From,
    Coordinates To,
    bool IsSuccess,
    AgentSnapshotDto Agent,
    SimulationCellDto[] UpdatedCells);

/// <summary>Notification payload for agent toggle events.</summary>
public record SimulationAgentToggledNotification(
    string JobId,
    string AgentId,
    ObjectType AgentType,
    string Action,
    bool IsActivated,
    AgentSnapshotDto Agent);

/// <summary>Notification payload for turn-completed events.</summary>
public record SimulationTurnCompletedNotification(
    string JobId,
    int TurnNumber,
    SimulationCellDto[] UpdatedCells);
