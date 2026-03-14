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
