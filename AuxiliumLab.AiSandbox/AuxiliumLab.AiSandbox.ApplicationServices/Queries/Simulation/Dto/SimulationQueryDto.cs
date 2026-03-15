using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Dto;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Dto;

/// <summary>State of a simulation job.</summary>
public enum SimulationJobState
{
    Running,
    Completed,
    Failed
}

/// <summary>Current status and result summary of a simulation job.</summary>
public class SimulationJobStatusDto
{
    public Guid JobId { get; set; }
    public SimulationKind Kind { get; set; }
    public SimulationJobState State { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>Number of simulation runs completed so far.</summary>
    public int CompletedRuns { get; set; }
    /// <summary>Total runs requested.</summary>
    public int TotalRuns { get; set; }
    /// <summary>Estimated remaining milliseconds. Null when not available.</summary>
    public long? EstimatedRemainingMs { get; set; }
}

/// <summary>Default sandbox configuration values read from appsettings.json.</summary>
public class SandboxDefaultsDto
{
    public int MaxTurns { get; set; }
    public int MapWidth { get; set; }
    public int MapHeight { get; set; }
    public double BlocksPercent { get; set; }
    public double EnemiesPercent { get; set; }
    public int HeroSpeed { get; set; }
    public int HeroSightRange { get; set; }
    public int HeroStamina { get; set; }
    public int EnemySpeed { get; set; }
}
