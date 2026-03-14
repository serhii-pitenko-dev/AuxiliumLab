namespace AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Configuration;

public class RewardSettings
{
    /// <summary>Reward applied on every regular step (agent survived the turn).</summary>
    public float StepPenalty { get; set; } = -0.1f;

    /// <summary>Reward applied when the hero reaches the exit.</summary>
    public float WinReward { get; set; } = 10f;

    /// <summary>Reward applied when the hero is caught or the turn limit is reached.</summary>
    public float LossReward { get; set; } = -10f;
}
