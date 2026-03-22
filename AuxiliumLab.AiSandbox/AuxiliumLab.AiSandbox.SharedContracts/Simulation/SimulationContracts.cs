namespace AuxiliumLab.AiSandbox.SharedContracts;

public enum ModelType { Random = 0, PPO, A2C, DQN }

public enum AiPolicy { MLP = 0, LSTM }

public enum SimulationKind { RandomAI, TrainedAI }

public class SimulationSandboxOverrideDto
{
    public int    MaxTurns       { get; set; }
    public int    MapWidth       { get; set; }
    public int    MapHeight      { get; set; }
    public double BlocksPercent  { get; set; }
    public double EnemiesPercent { get; set; }
    public int    HeroSpeed      { get; set; }
    public int    HeroSightRange { get; set; }
    public int    HeroStamina      { get; set; }
    public int    EnemySpeed       { get; set; }
    public int    EnemySightRange  { get; set; }
    public int    EnemyStamina     { get; set; }
}

public class StartSingleSimulationCommand
{
    public SimulationKind                Kind            { get; set; } = SimulationKind.RandomAI;
    public ModelType                     Algorithm       { get; set; } = ModelType.PPO;
    /// <summary>Experiment folder name that identifies the trained model to use (e.g. "ppo_100000_4_…").</summary>
    public string?                       ExperimentId    { get; set; }
    public SimulationSandboxOverrideDto  SandboxSettings { get; set; } = new();
    /// <summary>Delay in milliseconds applied between each agent action during presentation.</summary>
    public int                           ActionDelayMs   { get; set; } = 500;
}

public class StartMassSimulationCommand
{
    public SimulationKind                Kind             { get; set; } = SimulationKind.RandomAI;
    public int                           SimulationCount  { get; set; } = 100;
    public ModelType                     Algorithm        { get; set; } = ModelType.PPO;
    /// <summary>Experiment folder name that identifies the trained model to use.</summary>
    public string?                       ExperimentId     { get; set; }
    public SimulationSandboxOverrideDto  SandboxSettings  { get; set; } = new();
    public IncrementalSweeperDto?        IncrementalSweep { get; set; }
}

public class IncrementalSweeperDto
{
    public int          SimulationCount { get; set; } = 1;
    public List<string> Properties      { get; set; } = [];
}

public class SimulationJobStartedDto
{
    public Guid           JobId     { get; set; }
    public SimulationKind Kind      { get; set; }
    public DateTime       StartedAt { get; set; }
}

public class SimulationJobStatusDto
{
    public Guid                                          JobId                { get; set; }
    public SimulationKind                                Kind                 { get; set; }
    public SharedBaseTypes.ValueObjects.SandboxStatus    State                { get; set; }
    public DateTime           StartedAt            { get; set; }
    public DateTime?          CompletedAt          { get; set; }
    public string?            ErrorMessage         { get; set; }
    public int                CompletedRuns        { get; set; }
    public int                TotalRuns            { get; set; }
    public long?              EstimatedRemainingMs { get; set; }
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
    public int    EnemySpeed       { get; set; }
    public int    EnemySightRange  { get; set; }
    public int    EnemyStamina     { get; set; }
}
