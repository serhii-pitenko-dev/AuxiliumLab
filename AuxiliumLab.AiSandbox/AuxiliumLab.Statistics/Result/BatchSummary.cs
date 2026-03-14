namespace AuxiliumLab.AiSandbox.Domain.Statistics.Result;

/// <summary>
/// Contains aggregated summary information for a single execution phase within a mass run.
/// One record is saved per phase (standard runs, each property sweep, area sweep).
/// </summary>
public record BatchSummary(
    Guid Id,
    int Number,
    int TotalRuns,
    int Wins,
    int Losses,
    double AverageTurns,
    int MaxTurns,
    TimeSpan ExecutionTime)
{
    /// <summary>Wins as a percentage of TotalRuns. Returns 0 when TotalRuns is 0.</summary>
    public double WinPercentage => TotalRuns > 0 ? (double)Wins / TotalRuns * 100.0 : 0.0;
}
