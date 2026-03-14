namespace AuxiliumLab.AiSandbox.Domain.Statistics.Result;

/// <summary>
/// The combined result of a single sandbox execution:
/// playground configuration snapshot and the individual run outcome.
/// Returned by <c>RunAndCaptureAsync()</c> so the batch coordinator can
/// write both pieces to the batch log without the executor knowing about batches.
/// </summary>
public record SandboxRunResult(
    GeneralBatchRunInformation MapInfo,
    ParticularRun Run);
