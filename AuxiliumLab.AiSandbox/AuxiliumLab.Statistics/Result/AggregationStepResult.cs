namespace AuxiliumLab.AiSandbox.Domain.Statistics.Result;

/// <summary>
/// Captures the outcome of one step (job) within an aggregation run.
/// A step is either a training job or a mass simulation job.
/// </summary>
public record AggregationStepResult(
    /// <summary>Human-readable label configured in aggregation-settings.json (e.g. "Random AI", "PPO - AI").</summary>
    string StepName,

    /// <summary>The execution mode string for this step (e.g. "Training", "MassRandomAISimulation").</summary>
    string ExecutionMode,

    /// <summary>Set when this step is a training job; <see langword="null"/> otherwise.</summary>
    TrainingRunInfo? TrainingInfo,

    /// <summary>Set when this step is a mass simulation job; <see langword="null"/> for training steps.</summary>
    MassRunCapturedResult? MassRunResult)
{
    /// <summary>Returns <see langword="true"/> when this step represents a training job.</summary>
    public bool IsTraining => TrainingInfo is not null;
}
