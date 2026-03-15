namespace AuxiliumLab.AiSandbox.Common.SimulationVisualizationBridge;

/// <summary>
/// Bridges broker simulation events to the SignalR visualization layer.
/// Defined in Common so ApplicationServices can depend on it without a
/// circular reference to WebApi.
/// </summary>
public interface ISimulationVisualizationBridge
{
    /// <summary>Register a job that is about to start executing.</summary>
    void Attach(Guid jobId);

    /// <summary>Unregister a job that has finished (or failed).</summary>
    void Detach(Guid jobId);
}
