namespace AuxiliumLab.AiSandbox.SharedContracts;

/// <summary>Per-agent AI configuration. Determines whether an agent uses Random or a pre-trained model.</summary>
public class AgentAiConfigDto
{
    public ModelType  ModelType    { get; set; } = ModelType.Random;
    /// <summary>Experiment folder name. Required when <see cref="ModelType"/> is not Random.</summary>
    public string?    ExperimentId { get; set; }
    /// <summary>Agent type subfolder (HERO or ENEMY) under the algorithm folder.</summary>
    public TraineeAgentType AgentType { get; set; } = TraineeAgentType.Hero;
}
