using AuxiliumLab.AiSandbox.ApplicationServices.Commands.AggregationRun;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.AggregationRun;

/// <summary>
/// Read-only query service for aggregation data. Reads job statuses from
/// <see cref="AggregationRunCommandService"/>.
/// </summary>
public sealed class AggregationRunQueryService : IAggregationRunQueries
{
    private readonly AggregationRunCommandService _commandService;

    public AggregationRunQueryService(AggregationRunCommandService commandService)
    {
        _commandService = commandService;
    }

    public Task<IReadOnlyList<AggregationJobStatusDto>> GetAggregationStatusesAsync(CancellationToken ct = default)
        => Task.FromResult(_commandService.GetJobStatuses());
}
