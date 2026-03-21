namespace AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation;

/// <summary>
/// Application-level simulation commands for the Simulation feature slice.
/// Implementations launch background simulation tasks and return job descriptors immediately.
/// </summary>
public interface ISimulationCommands
{
    /// <summary>Starts a single simulation run. PPO only for trained; others throw NotImplementedException.</summary>
    Task<SimulationJobStartedDto> StartSingleSimulationAsync(StartSingleSimulationCommand command, CancellationToken ct = default);

    /// <summary>Starts a mass (batch) simulation run. PPO only for trained; others throw NotImplementedException.</summary>
    Task<SimulationJobStartedDto> StartMassSimulationAsync(StartMassSimulationCommand command, CancellationToken ct = default);

    /// <summary>Requests cancellation of a running simulation job. Returns false if job is not found.</summary>
    Task<bool> StopSimulationAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>Pauses a running visualization simulation. Returns false if job is not found.</summary>
    Task<bool> PauseSimulationAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>Resumes a paused visualization simulation. Returns false if job is not found.</summary>
    Task<bool> ResumeSimulationAsync(Guid jobId, CancellationToken ct = default);
}
