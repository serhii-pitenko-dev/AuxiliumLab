using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.SharedContracts;

/// <summary>Effect types that can be applied to map cells by agents.</summary>
public enum EEffect
{
    Path,
    Vision,
    Run
}

/// <summary>Describes a single agent's effects on a cell.</summary>
public class AgentEffectDto
{
    public Guid       AgentId   { get; set; }
    public ObjectType AgentType { get; set; }
    public EEffect[]  Effects   { get; set; } = [];
}

/// <summary>Describes a single cell transmitted to clients via SignalR.</summary>
public class SimulationCellDto
{
    public Coordinates      Position   { get; set; } = null!;
    public ObjectType       ObjectType { get; set; }
    public AgentEffectDto[] Effects    { get; set; } = [];
}

/// <summary>Snapshot of agent state accompanying action events.</summary>
public class AgentSnapshotDto
{
    public string     Id               { get; set; } = string.Empty;
    public ObjectType Type             { get; set; }
    public int        Speed            { get; set; }
    public int        SightRange       { get; set; }
    public bool       IsRun            { get; set; }
    public int        Stamina          { get; set; }
    public int        MaxStamina       { get; set; }
    public int        OrderInTurnQueue { get; set; }
}

/// <summary>Initial position and stats of an agent at simulation start.</summary>
public class InitialAgentDto
{
    public string          AgentId   { get; set; } = string.Empty;
    public ObjectType      AgentType { get; set; }
    public Coordinates     Position  { get; set; } = null!;
    public AgentSnapshotDto Snapshot { get; set; } = new();
}

/// <summary>Sent once when the simulation starts; contains the full initial map and agent positions.</summary>
public class SimulationStartedDto
{
    public string             JobId    { get; set; } = string.Empty;
    public int                Width    { get; set; }
    public int                Height   { get; set; }
    public int                MaxTurns { get; set; }
    public SimulationCellDto[]  Cells  { get; set; } = [];
    public InitialAgentDto[]    Agents { get; set; } = [];
}

/// <summary>Sent after each agent move action.</summary>
public class AgentMovedDto
{
    public string           JobId        { get; set; } = string.Empty;
    public string           AgentId      { get; set; } = string.Empty;
    public ObjectType       AgentType    { get; set; }
    public Coordinates      From         { get; set; } = null!;
    public Coordinates      To           { get; set; } = null!;
    public bool             IsSuccess    { get; set; }
    public AgentSnapshotDto Agent        { get; set; } = new();
    public SimulationCellDto[] UpdatedCells { get; set; } = [];
}

/// <summary>Sent when an agent toggle-action (Run/RunOff) fires.</summary>
public class AgentToggledDto
{
    public string           JobId       { get; set; } = string.Empty;
    public string           AgentId     { get; set; } = string.Empty;
    public ObjectType       AgentType   { get; set; }
    public string           Action      { get; set; } = string.Empty;
    public bool             IsActivated { get; set; }
    public AgentSnapshotDto Agent       { get; set; } = new();
}

/// <summary>Sent after every turn is completed.</summary>
public class TurnCompletedDto
{
    public string              JobId        { get; set; } = string.Empty;
    public int                 TurnNumber   { get; set; }
    public SimulationCellDto[] UpdatedCells { get; set; } = [];
}

/// <summary>Sent when the simulation ends (win, loss, or cancellation).</summary>
public class SimulationEndedDto
{
    public string  JobId     { get; set; } = string.Empty;
    public string  Outcome   { get; set; } = string.Empty;
    public string? Reason    { get; set; }
    public int     FinalTurn { get; set; }
}
