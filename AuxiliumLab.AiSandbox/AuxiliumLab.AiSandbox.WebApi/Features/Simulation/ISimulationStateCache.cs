namespace AuxiliumLab.AiSandbox.WebApi.Features.Simulation;

/// <summary>
/// Holds the last known initial state for each active simulation job so that
/// clients who connect after <see cref="SimulationHub.Methods.SimulationStarted"/>
/// was broadcast can still receive the initial grid.
/// </summary>
public interface ISimulationStateCache
{
    /// <summary>Returns the cached initial state for <paramref name="jobId"/>, or <c>null</c> if not found.</summary>
    SimulationStartedDto? GetCachedStart(string jobId);
}
