using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Responses;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Ai;

/// <summary>
/// Shared helpers for building agent observation vectors and decoding RL action integers.
/// Used by both <see cref="Sb3Actions"/> (training) and
/// <see cref="AuxiliumLab.AiSandbox.AiTrainingOrchestrator.InferenceActions"/>
/// (inference-only simulation) so the format is guaranteed to be identical.
/// </summary>
public static class ObservationBuilder
{
    /// <summary>
    /// Builds the float observation vector for the given agent state.
    /// Layout: [x, y, is_run, stamina_frac, speed, grid_0_0, ..., grid_n_n].
    /// Grid is row-major, centred on the agent, size = (2*SightRange+1)².
    /// Must remain in sync with Python's BuildObservation on the training side.
    /// </summary>
    public static float[] Build(AgentStateForAIDecision agent)
    {
        int gridSize        = 2 * agent.SightRange + 1;
        int observationSize = 5 + gridSize * gridSize;
        var obs             = new float[observationSize];

        // ── Scalar features (indices 0-4) ─────────────────────────────────────
        obs[0] = agent.Coordinates.X;
        obs[1] = agent.Coordinates.Y;
        obs[2] = agent.IsRun ? 1f : 0f;
        obs[3] = agent.MaxStamina > 0 ? (float)agent.Stamina / agent.MaxStamina : 0f;
        obs[4] = agent.Speed;

        // ── Vision grid (indices 5..observationSize-1) ────────────────────────
        // Default to -1 (not visible) then overwrite with actual cell values.
        for (int i = 5; i < observationSize; i++) obs[i] = -1f;

        foreach (var cell in agent.VisibleCells)
        {
            int dx = cell.Coordinates.X - agent.Coordinates.X;
            int dy = cell.Coordinates.Y - agent.Coordinates.Y;
            int gx = dx + agent.SightRange;
            int gy = dy + agent.SightRange;
            obs[5 + gy * gridSize + gx] = (float)cell.ObjectType;
        }

        return obs;
    }

    /// <summary>
    /// Decodes an RL action integer (0–4) into the appropriate agent decision response.
    /// <list type="bullet">
    ///   <item>0 = up    (Y−1)</item>
    ///   <item>1 = down  (Y+1)</item>
    ///   <item>2 = left  (X−1)</item>
    ///   <item>3 = right (X+1)</item>
    ///   <item>4 = toggle-run</item>
    /// </list>
    /// </summary>
    public static AgentDecisionBaseResponse BuildDecisionResponse(
        Guid correlationId, Guid agentId, Coordinates from, int action)
    {
        return action switch
        {
            0 => new AgentDecisionMoveResponse(Guid.NewGuid(), agentId,
                     from, new Coordinates(from.X, from.Y - 1), correlationId, true),
            1 => new AgentDecisionMoveResponse(Guid.NewGuid(), agentId,
                     from, new Coordinates(from.X, from.Y + 1), correlationId, true),
            2 => new AgentDecisionMoveResponse(Guid.NewGuid(), agentId,
                     from, new Coordinates(from.X - 1, from.Y), correlationId, true),
            3 => new AgentDecisionMoveResponse(Guid.NewGuid(), agentId,
                     from, new Coordinates(from.X + 1, from.Y), correlationId, true),
            _ => new AgentDecisionUseAbilityResponse(Guid.NewGuid(), agentId,
                     true, AgentAction.Run, correlationId, true)
        };
    }
}
