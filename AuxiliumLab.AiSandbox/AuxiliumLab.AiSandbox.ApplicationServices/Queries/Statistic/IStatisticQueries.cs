using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Statistic.Dto;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Statistic;

/// <summary>
/// Read-only statistic queries for the Statistic feature slice.
/// </summary>
public interface IStatisticQueries
{
    /// <summary>Returns data about all completed simulation runs (single + mass).</summary>
    Task<IReadOnlyList<CompletedSimulationRunDto>> GetCompletedSimulationRunsAsync(CancellationToken ct = default);

    /// <summary>Returns data about all completed aggregation runs.</summary>
    Task<IReadOnlyList<CompletedAggregationRunDto>> GetCompletedAggregationRunsAsync(CancellationToken ct = default);
}
