namespace AuxiliumLab.Frontend.Features.Simulation.Dto;

// ── Simulation Commands ──────────────────────────────────────────────────────

public enum SimulationKind { RandomAI, TrainedAI }
public enum ModelType { PPO, A2C, DQN }

public class SimulationSandboxOverrideDto
{
    public int?    MaxTurns       { get; set; }
    public int?    MapWidth       { get; set; }
    public int?    MapHeight      { get; set; }
    public double? BlocksPercent  { get; set; }
    public double? EnemiesPercent { get; set; }
    public int?    HeroSpeed      { get; set; }
    public int?    HeroSightRange { get; set; }
    public int?    HeroStamina    { get; set; }
    public int?    EnemySpeed     { get; set; }
}

public class StartSingleSimulationCommand
{
    public SimulationKind                Kind           { get; set; } = SimulationKind.RandomAI;
    public ModelType                     Algorithm      { get; set; } = ModelType.PPO;
    public SimulationSandboxOverrideDto? SandboxSettings { get; set; }
}

public class StartMassSimulationCommand
{
    public SimulationKind                Kind            { get; set; } = SimulationKind.RandomAI;
    public int                           SimulationCount { get; set; } = 100;
    public ModelType                     Algorithm       { get; set; } = ModelType.PPO;
    public SimulationSandboxOverrideDto? SandboxSettings  { get; set; }
    public IncrementalSweeperDto?        IncrementalSweep { get; set; }
}

public class IncrementalSweeperDto
{
    public int          SimulationCount { get; set; } = 1;
    public List<string> Properties      { get; set; } = [];
}

// ── Simulation Queries ────────────────────────────────────────────────────────

public enum SimulationJobState { Running, Completed, Failed }

public class SimulationJobStatusDto
{
    public Guid              JobId               { get; set; }
    public SimulationKind    Kind                { get; set; }
    public SimulationJobState State              { get; set; }
    public DateTime          StartedAt           { get; set; }
    public DateTime?         CompletedAt         { get; set; }
    public string?           ErrorMessage        { get; set; }
    public int               CompletedRuns       { get; set; }
    public int               TotalRuns           { get; set; }
    public long?             EstimatedRemainingMs { get; set; }
}

public class SimulationJobStartedDto
{
    public Guid           JobId     { get; set; }
    public SimulationKind Kind      { get; set; }
    public DateTime       StartedAt { get; set; }
}

public class SandboxDefaultsDto
{
    public int    MaxTurns       { get; set; }
    public int    MapWidth       { get; set; }
    public int    MapHeight      { get; set; }
    public double BlocksPercent  { get; set; }
    public double EnemiesPercent { get; set; }
    public int    HeroSpeed      { get; set; }
    public int    HeroSightRange { get; set; }
    public int    HeroStamina    { get; set; }
    public int    EnemySpeed     { get; set; }
}

// ── SignalR / Real-time events ────────────────────────────────────────────────

public class SimulationCellDto
{
    public int      X          { get; set; }
    public int      Y          { get; set; }
    public string   ObjectType { get; set; } = "Empty";
    public string[] Effects    { get; set; } = [];
}

public class AgentSnapshotDto
{
    public string Id              { get; set; } = string.Empty;
    public string Type            { get; set; } = string.Empty;
    public int    Speed           { get; set; }
    public int    SightRange      { get; set; }
    public bool   IsRun           { get; set; }
    public int    Stamina         { get; set; }
    public int    MaxStamina      { get; set; }
    public int    OrderInTurnQueue { get; set; }
}

public class SimulationStartedDto
{
    public string             JobId    { get; set; } = string.Empty;
    public int                Width    { get; set; }
    public int                Height   { get; set; }
    public int                MaxTurns { get; set; }
    public SimulationCellDto[]  Cells  { get; set; } = [];
    public InitialAgentDto[]    Agents { get; set; } = [];
}

public class InitialAgentDto
{
    public string          AgentId   { get; set; } = string.Empty;
    public string          AgentType { get; set; } = string.Empty;
    public int             X         { get; set; }
    public int             Y         { get; set; }
    public AgentSnapshotDto Snapshot { get; set; } = new();
}

public class AgentMovedDto
{
    public string          JobId     { get; set; } = string.Empty;
    public string          AgentId   { get; set; } = string.Empty;
    public string          AgentType { get; set; } = string.Empty;
    public int             FromX     { get; set; }
    public int             FromY     { get; set; }
    public int             ToX       { get; set; }
    public int             ToY       { get; set; }
    public bool            IsSuccess { get; set; }
    public AgentSnapshotDto Agent    { get; set; } = new();
}

public class AgentToggledDto
{
    public string          JobId      { get; set; } = string.Empty;
    public string          AgentId    { get; set; } = string.Empty;
    public string          AgentType  { get; set; } = string.Empty;
    public string          Action     { get; set; } = string.Empty;
    public bool            IsActivated { get; set; }
    public AgentSnapshotDto Agent     { get; set; } = new();
}

public class TurnCompletedDto
{
    public string             JobId        { get; set; } = string.Empty;
    public int                TurnNumber   { get; set; }
    public SimulationCellDto[] UpdatedCells { get; set; } = [];
}

public class SimulationEndedDto
{
    public string  JobId      { get; set; } = string.Empty;
    public string  Outcome    { get; set; } = string.Empty;
    public string? Reason     { get; set; }
    public int     FinalTurn  { get; set; }
}
