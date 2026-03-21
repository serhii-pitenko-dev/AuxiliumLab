namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation;

/// <summary>
/// Read-only simulation queries for the Simulation feature slice.
/// </summary>
public interface ISimulationQueries
{
    /// <summary>Returns the current status of all simulation jobs (running and recently completed).</summary>
    Task<IReadOnlyList<SimulationJobStatusDto>> GetSimulationStatusesAsync(CancellationToken ct = default);

    /// <summary>Returns the default sandbox configuration values from appsettings.json.</summary>
    Task<SandboxDefaultsDto> GetSandboxDefaultsAsync(CancellationToken ct = default);
}
