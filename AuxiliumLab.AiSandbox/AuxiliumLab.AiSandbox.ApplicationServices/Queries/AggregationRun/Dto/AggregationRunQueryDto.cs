namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.AggregationRun.Dto;

/// <summary>State of an aggregation job.</summary>
public enum AggregationJobState
{
    Running,
    Completed,
    Failed
}

/// <summary>Current status of an aggregation run job.</summary>
public class AggregationJobStatusDto
{
    public Guid JobId { get; set; }
    public AggregationJobState State { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>Names of all defined steps.</summary>
    public List<string> StepNames { get; set; } = [];
    /// <summary>Name of the currently executing step, or null if not started.</summary>
    public string? CurrentStep { get; set; }
    /// <summary>Number of steps completed so far.</summary>
    public int CompletedSteps { get; set; }
}
