namespace AuxiliumLab.AiSandbox.SharedContracts;

public class StartPpoTrainingCommand
{
    public PpoHyperparametersDto     Hyperparameters { get; set; } = new();
    public TrainingSandboxSettingsDto SandboxSettings { get; set; } = new();
    public RewardSettingsDto         RewardSettings  { get; set; } = new();
    /// <summary>Which agent type to train. Default is Hero.</summary>
    public TraineeAgentType          TraineeAgent    { get; set; } = TraineeAgentType.Hero;
    /// <summary>AI configuration for the opponent (non-trainee) agent.</summary>
    public AgentAiConfigDto          OpponentAi      { get; set; } = new();
}
