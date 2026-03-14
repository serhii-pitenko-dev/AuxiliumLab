namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Training.Dto;

/// <summary>Status of a single training job.</summary>
public enum TrainingJobState
{
    Running,
    Completed,
    Failed
}

/// <summary>Information about a trained model stored on disk.</summary>
public class TrainedModelInfoDto
{
    public string Algorithm { get; set; } = string.Empty;
    public string ExperimentId { get; set; } = string.Empty;
    public string ModelFilePath { get; set; } = string.Empty;
    public DateTime TrainedAt { get; set; }
    /// <summary>Precondition parameters snapshot saved alongside the model.</summary>
    public TrainingPreconditionsDto? Preconditions { get; set; }
}

/// <summary>Snapshot of training preconditions saved to preconditions.json.</summary>
public class TrainingPreconditionsDto
{
    public string Algorithm { get; set; } = string.Empty;
    public string ExperimentId { get; set; } = string.Empty;
    public Dictionary<string, string> Hyperparameters { get; set; } = [];
    public int MaxTurns { get; set; }
    public int MapWidth { get; set; }
    public int MapHeight { get; set; }
    public float StepPenalty { get; set; }
    public float WinReward { get; set; }
    public float LossReward { get; set; }
    public DateTime StartedAt { get; set; }
}

/// <summary>Current status of a training job.</summary>
public class TrainingJobStatusDto
{
    public Guid JobId { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public string ExperimentId { get; set; } = string.Empty;
    public TrainingJobState State { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>Estimated remaining milliseconds. Null when not yet available.</summary>
    public long? EstimatedRemainingMs { get; set; }
}
