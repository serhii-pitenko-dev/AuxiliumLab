using AuxiliumLab.AiSandbox.ApplicationServices.Jobs.AggregationRun;
using AuxiliumLab.AiSandbox.ApplicationServices.Jobs.Simulation;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.AggregationRun.Dto;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Dto;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Statistic;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Statistic.Dto;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Jobs.Statistic;

/// <summary>
/// Implements <see cref="IStatisticQueries"/> by reading completed-job data from
/// the in-memory job stores held by <see cref="SimulationJobService"/> and
/// <see cref="AggregationJobService"/>.
/// </summary>
public sealed class StatisticQueryService : IStatisticQueries
{
    private readonly SimulationJobService _simulationJobs;
    private readonly AggregationJobService _aggregationJobs;

    public StatisticQueryService(
        SimulationJobService simulationJobs,
        AggregationJobService aggregationJobs)
    {
        _simulationJobs  = simulationJobs;
        _aggregationJobs = aggregationJobs;
    }

    public async Task<IReadOnlyList<CompletedSimulationRunDto>> GetCompletedSimulationRunsAsync(
        CancellationToken ct = default)
    {
        var allJobs = await _simulationJobs.GetSimulationStatusesAsync(ct);
        return allJobs
            .Where(j => j.State == SimulationJobState.Completed)
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
        var allJobs = await _aggregationJobs.GetAggregationStatusesAsync(ct);
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
