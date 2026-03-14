namespace AuxiliumLab.AiSandbox.Domain.Statistics.Result;

/// <summary>
/// Information captured from a completed training run, embedded in an <see cref="AggregationStepResult"/>
/// for inclusion in the aggregation report.
/// </summary>
public record TrainingRunInfo(
    string AlgorithmName,
    string ExperimentId,
    IReadOnlyDictionary<string, string> Parameters);
