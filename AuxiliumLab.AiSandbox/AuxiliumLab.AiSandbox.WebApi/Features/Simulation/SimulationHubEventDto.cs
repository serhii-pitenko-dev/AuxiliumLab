namespace AuxiliumLab.AiSandbox.WebApi.Features.Simulation;

/// <summary>Describes a single cell transmitted to clients via SignalR.</summary>
public record SimulationCellDto(
    int X,
    int Y,
    string ObjectType,
    string[] Effects);

/// <summary>Initial position and stats of an agent at simulation start.</summary>
public record InitialAgentDto(
    string AgentId,
    string AgentType,
    int X,
    int Y,
    AgentSnapshotDto Snapshot);

/// <summary>Sent once when the simulation starts; contains the full initial map and agent positions.</summary>
public record SimulationStartedDto(
    string JobId,
    int Width,
    int Height,
    int MaxTurns,
    SimulationCellDto[] Cells,
    InitialAgentDto[] Agents);

/// <summary>Sent after each agent move action.</summary>
public record AgentMovedDto(
    string JobId,
    string AgentId,
    string AgentType,
    int FromX,
    int FromY,
    int ToX,
    int ToY,
    bool IsSuccess,
    AgentSnapshotDto Agent,
    /// <summary>Full cell snapshot after the move, including updated path/vision effects.</summary>
    SimulationCellDto[] UpdatedCells);

/// <summary>Sent when an agent toggle-action (Run/RunOff) fires.</summary>
public record AgentToggledDto(
    string JobId,
    string AgentId,
    string AgentType,
    string Action,
    bool IsActivated,
    AgentSnapshotDto Agent);

/// <summary>Snapshot of agent state accompanying action events.</summary>
public record AgentSnapshotDto(
    string Id,
    string Type,
    int Speed,
    int SightRange,
    bool IsRun,
    int Stamina,
    int MaxStamina,
    int OrderInTurnQueue);

/// <summary>Sent after every turn is completed.</summary>
public record TurnCompletedDto(
    string JobId,
    int TurnNumber,
    SimulationCellDto[] UpdatedCells);

/// <summary>Sent when the simulation ends (win, loss, or cancellation).</summary>
public record SimulationEndedDto(
    string JobId,
    string Outcome,
    string? Reason,
    int FinalTurn);
