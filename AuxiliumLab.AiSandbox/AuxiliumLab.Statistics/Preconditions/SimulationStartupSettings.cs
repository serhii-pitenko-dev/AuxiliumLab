namespace AuxiliumLab.AiSandbox.Statistics.Preconditions;

/// <summary>
/// Captures the startup settings relevant to a mass run simulation.
/// Mirrors the <c>StartupSettings</c> configuration block without
/// <c>IsPreconditionStart</c> and <c>PresentationMode</c>.
/// </summary>
public class SimulationStartupSettings
{
    /// <summary>AI policy type used for the simulation (e.g. MLP).</summary>
    public string PolicyType { get; set; } = string.Empty;

    /// <summary>Execution mode for the batch (e.g. MassRandomAISimulation).</summary>
    public string ExecutionMode { get; set; } = string.Empty;

    /// <summary>Number of standard (baseline) simulation runs.</summary>
    public int StandardSimulationCount { get; set; }

    /// <summary>Incremental sweep settings: simulation count per step and property names.</summary>
    public SimulationIncrementalPropertiesSettings IncrementalProperties { get; set; } = new();
}
