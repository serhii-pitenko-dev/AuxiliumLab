namespace AuxiliumLab.AiSandbox.Statistics.Preconditions;

/// <summary>
/// Mirrors the <c>IncrementalProperties</c> sub-block of <c>StartupSettings</c>.
/// </summary>
public class SimulationIncrementalPropertiesSettings
{
    /// <summary>Number of simulation runs executed for every individual step value of a swept property.</summary>
    public int SimulationCount { get; set; } = 1;

    /// <summary>Property names swept incrementally during the run.</summary>
    public List<string> Properties { get; set; } = [];
}
