namespace AuxiliumLab.AiSandbox.Domain.Statistics.Result;

/// <summary>
/// Contains general information about the map/playground configuration shared across all runs in a batch.
/// Saved once at the beginning of a batch run.
/// </summary>
public record GeneralBatchRunInformation(
    int BlocksCount,
    int EnemiesCount,
    int Area,
    int MapWidth,
    int MapHeight,
    int MapArea);
