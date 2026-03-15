namespace AuxiliumLab.AiSandbox.WebApi.Features.Simulation;

/// <summary>
/// Holds snapshot state for simulation jobs so that clients who connect after
/// a broadcast can still receive the full final state.
/// </summary>
public interface ISimulationStateCache
{
    /// <summary>Returns the cached initial state (grid layout + initial agent positions).</summary>
    SimulationStartedDto? GetCachedStart(string jobId);

    /// <summary>Returns the latest TurnCompleted snapshot (full cell grid at the last processed turn).</summary>
    TurnCompletedDto? GetCachedLastTurn(string jobId);

    /// <summary>Returns the last-known AgentMoved event for every agent in this job.</summary>
    IReadOnlyList<AgentMovedDto> GetCachedLastAgentMoves(string jobId);

    /// <summary>Returns the cached end state, or <c>null</c> if the simulation has not yet ended.</summary>
    SimulationEndedDto? GetCachedEnd(string jobId);
}
