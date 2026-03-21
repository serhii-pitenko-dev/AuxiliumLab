namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.AggregationRun;

/// <summary>
/// Read-only aggregation run queries for the AggregationRun feature slice.
/// </summary>
public interface IAggregationRunQueries
{
    /// <summary>Returns the current status of all aggregation jobs (running and recently completed).</summary>
    Task<IReadOnlyList<AggregationJobStatusDto>> GetAggregationStatusesAsync(CancellationToken ct = default);
}
