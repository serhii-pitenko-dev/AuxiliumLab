using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Dto;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation;

/// <summary>
/// Read-only simulation queries for the Simulation feature slice.
/// </summary>
public interface ISimulationQueries
{
    /// <summary>Returns the current status of all simulation jobs (running and recently completed).</summary>
    Task<IReadOnlyList<SimulationJobStatusDto>> GetSimulationStatusesAsync(CancellationToken ct = default);
}
