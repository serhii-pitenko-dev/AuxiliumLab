namespace AuxiliumLab.AiSandbox.Startup.Configuration;

/// <summary>
/// Groups incremental-sweep settings: how many simulation runs per step
/// and which property names are swept.
/// </summary>
public class IncrementalPropertiesSettings
{
    /// <summary>
    /// Number of simulation runs executed for every individual step value of a swept property.
    /// E.g. if SimulationCount = 5 and MaxTurns has 38 steps, total incremental runs = 190.
    /// </summary>
    public int SimulationCount { get; set; } = 1;

    /// <summary>Property names from <c>IncrementalPropertyNames</c> to sweep.</summary>
    public List<string> Properties { get; set; } = [];
}
