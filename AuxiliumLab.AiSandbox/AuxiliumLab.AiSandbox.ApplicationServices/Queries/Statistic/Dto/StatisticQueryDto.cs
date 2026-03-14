namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Statistic.Dto;

/// <summary>Summary of a single completed batch simulation run.</summary>
public class CompletedSimulationRunDto
{
    public Guid JobId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int TotalRuns { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinPercentage => TotalRuns > 0 ? (double)Wins / TotalRuns * 100.0 : 0.0;
    public double AverageTurns { get; set; }
}

/// <summary>Summary of a completed aggregation run with per-step results.</summary>
public class CompletedAggregationRunDto
{
    public Guid JobId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<AggregationStepResultDto> Steps { get; set; } = [];
}

/// <summary>Result of a single step within an aggregation run.</summary>
public class AggregationStepResultDto
{
    public string StepName { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public int TotalRuns { get; set; }
    public int Wins { get; set; }
    public double WinPercentage => TotalRuns > 0 ? (double)Wins / TotalRuns * 100.0 : 0.0;
    public double AverageTurns { get; set; }
    /// <summary>Training experiment ID, if this step was a training step.</summary>
    public string? TrainingExperimentId { get; set; }
}
