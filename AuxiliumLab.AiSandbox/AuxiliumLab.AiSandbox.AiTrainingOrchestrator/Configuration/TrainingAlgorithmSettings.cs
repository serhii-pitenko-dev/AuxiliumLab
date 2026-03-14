namespace AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Configuration;

public class TrainingAlgorithmSettings
{
    /// <summary>"PPO" | "A2C" | "DQN"</summary>
    public string Algorithm { get; set; } = string.Empty;

    public List<TrainingParameter> Parameters { get; set; } = [];
}
