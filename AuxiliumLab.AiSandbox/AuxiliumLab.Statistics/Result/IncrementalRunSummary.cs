namespace AuxiliumLab.AiSandbox.Domain.Statistics.Result;

/// <summary>
/// Aggregated summary for one incremental property sweep within a mass run.
/// One record is produced per swept property (and one for the joint area sweep when enabled).
/// Contains the list of <see cref="BatchSummary"/> per step and metadata about the sweep.
/// </summary>
public record IncrementalRunSummary(
    Guid Id,
    int Number,
    IReadOnlyList<BatchSummary> Batches,
    string Description,
    TimeSpan ExecutionTime,
    string Property,
    int BatchRunCount,
    string Min,
    string Step,
    string Max);
