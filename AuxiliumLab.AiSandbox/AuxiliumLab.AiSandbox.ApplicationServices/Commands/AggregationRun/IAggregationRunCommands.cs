namespace AuxiliumLab.AiSandbox.ApplicationServices.Commands.AggregationRun;

/// <summary>
/// Application-level aggregation run commands for the AggregationRun feature slice.
/// </summary>
public interface IAggregationRunCommands
{
    /// <summary>Starts an aggregation run in the background and returns immediately.</summary>
    Task<AggregationJobStartedDto> StartAggregationAsync(StartAggregationCommand command, CancellationToken ct = default);

    /// <summary>Requests cancellation of a running aggregation job. Returns false if job is not found.</summary>
    Task<bool> StopAggregationAsync(Guid jobId, CancellationToken ct = default);
}
