using AuxiliumLab.AiSandbox.ApplicationServices.Queries.AggregationRun;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Statistic;

/// <summary>
/// Implements <see cref="IStatisticQueries"/> by reading completed-job data from
/// <see cref="ISimulationQueries"/> and <see cref="IAggregationRunQueries"/>.
/// </summary>
public sealed class StatisticQueryService : IStatisticQueries
{
    private readonly ISimulationQueries _simulationQueries;
    private readonly IAggregationRunQueries _aggregationQueries;

    public StatisticQueryService(
        ISimulationQueries simulationQueries,
        IAggregationRunQueries aggregationQueries)
    {
        _simulationQueries  = simulationQueries;
        _aggregationQueries = aggregationQueries;
    }

    public async Task<IReadOnlyList<CompletedSimulationRunDto>> GetCompletedSimulationRunsAsync(
        CancellationToken ct = default)
    {
        var allJobs = await _simulationQueries.GetSimulationStatusesAsync(ct);
        return allJobs
            .Where(j => j.State != SandboxStatus.InProgress && j.State != SandboxStatus.Failed)
            .Select(j => new CompletedSimulationRunDto
            {
                JobId        = j.JobId,
                Kind         = j.Kind.ToString(),
                StartedAt    = j.StartedAt,
                CompletedAt  = j.CompletedAt!.Value,
                TotalRuns    = j.TotalRuns,
                Wins         = 0, // detailed stats are written to CSV by MassRunner
                Losses       = j.TotalRuns,
                AverageTurns = 0
            })
            .ToList();
    }

    public async Task<IReadOnlyList<CompletedAggregationRunDto>> GetCompletedAggregationRunsAsync(
        CancellationToken ct = default)
    {
        var allJobs = await _aggregationQueries.GetAggregationStatusesAsync(ct);
        return allJobs
            .Where(j => j.State == AggregationJobState.Completed)
            .Select(j => new CompletedAggregationRunDto
            {
                JobId       = j.JobId,
                StartedAt   = j.StartedAt,
                CompletedAt = j.CompletedAt!.Value,
                Steps       = j.StepNames.Select(name => new AggregationStepResultDto
                {
                    StepName = name,
                    Mode     = string.Empty
                }).ToList()
            })
            .ToList();
    }
}
