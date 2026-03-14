namespace AuxiliumLab.AiSandbox.Statistics.Preconditions;

/// <summary>
/// Represents an incremental range setting mirroring <c>IncrementalRange</c>,
/// containing the Min, Current, Max and Step values used for sandbox configuration.
/// </summary>
public record RangeSettings(int Min, int Current, int Max, int Step);
