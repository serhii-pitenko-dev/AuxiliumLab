using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.PolicyTrainer;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;

namespace AuxiliumLab.AiSandbox.AiTrainingOrchestrator;

/// <summary>
/// Single source of truth for the environment contract sent to the Python RL service.
/// </summary>
/// <remarks>
/// The Python side independently verifies the formula:
///   observation_dim = 5 + (2 × sight_range + 1)²
/// Any deviation is treated as a hard error on both sides.
///
/// The 5 scalar features at the front of every observation vector are:
///   [0] x             — absolute column on the map (0-based)
///   [1] y             — absolute row on the map (0-based)
///   [2] is_run        — 1.0 if hero is in run mode, 0.0 otherwise
///   [3] stamina_frac  — current stamina / max stamina ∈ [0, 1]
///   [4] speed         — cells traversable per turn
/// Followed by (2×SightRange+1)² vision-grid cells in row-major order.
/// </remarks>
public static class EnvironmentSpecBuilder
{
    /// <summary>Number of scalar features preceding the vision grid in the observation vector.</summary>
    public const int ScalarFeatureCount = 5;

    /// <summary>Expected number of discrete actions.</summary>
    public const int ActionDim = 5;

    /// <summary>
    /// Build an <see cref="EnvironmentSpec"/> from the current sandbox settings.
    /// </summary>
    /// <param name="settings">The sandbox configuration (read from appsettings.json → SandBox).</param>
    /// <param name="experimentId">Experiment ID that will be used in the subsequent TrainingRequest.</param>
    public static EnvironmentSpec Build(SandBoxConfiguration settings, string experimentId)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentException.ThrowIfNullOrWhiteSpace(experimentId, nameof(experimentId));

        int sightRange = settings.Hero.SightRange.Current;
        int gridSize   = 2 * sightRange + 1;
        int obsDim     = ScalarFeatureCount + gridSize * gridSize;

        var spec = new EnvironmentSpec
        {
            ObservationDim = obsDim,
            ActionDim      = ActionDim,
            SightRange     = sightRange,
        };

        // ── Scalar feature names (indices 0–4) ─────────────────────────────
        spec.ObservationFeatureNames.AddRange(["x", "y", "is_run", "stamina_frac", "speed"]);

        // ── Vision grid names (indices 5 … obsDim−1) ───────────────────────
        for (int row = 0; row < gridSize; row++)
            for (int col = 0; col < gridSize; col++)
                spec.ObservationFeatureNames.Add($"grid_{row}_{col}");

        return spec;
    }

    /// <summary>
    /// Verifies the echoed spec round-trips exactly.
    /// Throws <see cref="InvalidOperationException"/> with a detailed diagnostics message on mismatch.
    /// </summary>
    public static void AssertEchoMatches(EnvironmentSpec sent, EnvironmentSpec echoed, string experimentId)
    {
        ArgumentNullException.ThrowIfNull(sent,    nameof(sent));
        ArgumentNullException.ThrowIfNull(echoed,  nameof(echoed));

        if (echoed.ObservationDim != sent.ObservationDim
            || echoed.ActionDim   != sent.ActionDim
            || echoed.SightRange  != sent.SightRange)
        {
            throw new InvalidOperationException(
                $"Environment spec echo mismatch for experiment '{experimentId}'. " +
                $"Sent: obs_dim={sent.ObservationDim}, action_dim={sent.ActionDim}, " +
                $"sight_range={sent.SightRange}. " +
                $"Echoed: obs_dim={echoed.ObservationDim}, action_dim={echoed.ActionDim}, " +
                $"sight_range={echoed.SightRange}. " +
                "The Python service returned a modified spec — aborting training.");
        }
    }
}
