namespace AuxiliumLab.AiSandbox.Domain.Statistics.Result;

/// <summary>
/// The captured output of a single <c>MassRunner.RunManyAsync</c> call.
/// Returned so an <c>AggregationRunner</c> can collect results from multiple run types
/// and produce a combined report.
/// </summary>
public record MassRunCapturedResult(
    BatchSummary StandardBatch,
    IReadOnlyList<IncrementalRunSummary> SweepSummaries,
    IncrementalRunSummary? AreaSweepSummary);
