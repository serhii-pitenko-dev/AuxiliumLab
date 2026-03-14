namespace AuxiliumLab.AiSandbox.Domain.Statistics.Result;

/// <summary>
/// Aggregated summary for an entire mass run (all phases combined).
/// Saved once at the end of <c>MassRunner.RunManyAsync</c>.
/// </summary>
public record MassRunSummary(
    int BatchesCount,
    TimeSpan TimeExecution,
    string Description);
