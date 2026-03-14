namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration;

/// <summary>
/// Top-level settings loaded from <c>aggregation-settings.json</c>.
/// Defines the ordered sequence of job steps that the AggregationRun executes.
/// </summary>
public class AggregationSettings
{
    public const string SectionName = "AggregationSettings";

    /// <summary>Ordered list of steps to run in sequence during an aggregation run.</summary>
    public List<AggregationStep> Steps { get; set; } = [];
}

/// <summary>
/// A single step in an aggregation run sequence.
/// </summary>
public class AggregationStep
{
    /// <summary>
    /// Human-readable label used in the CSV report header (e.g. "Random AI", "PPO - AI").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The <see cref="AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects.StartupSettings.ExecutionMode"/>
    /// value as a string (e.g. "Training", "MassRandomAISimulation", "MassTrainedAISimulation").
    /// </summary>
    public string Mode { get; set; } = string.Empty;
}
