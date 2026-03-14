namespace AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Configuration;

public class TrainingSettings
{
    public List<TrainingAlgorithmSettings> Algorithms { get; set; } = [];
    public RewardSettings Rewards { get; set; } = new();
}
