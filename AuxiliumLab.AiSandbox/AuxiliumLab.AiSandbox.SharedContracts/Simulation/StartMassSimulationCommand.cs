namespace AuxiliumLab.AiSandbox.SharedContracts;

public class StartMassSimulationCommand
{
    public SimulationKind                Kind             { get; set; } = SimulationKind.RandomAI;
    public int                           SimulationCount  { get; set; } = 100;
    public ModelType                     Algorithm        { get; set; } = ModelType.PPO;
    /// <summary>Experiment folder name that identifies the trained model to use.</summary>
    public string?                       ExperimentId     { get; set; }
    public SimulationSandboxOverrideDto  SandboxSettings  { get; set; } = new();
    public IncrementalSweeperDto?        IncrementalSweep { get; set; }

    /// <summary>AI configuration for the Hero agent.</summary>
    public AgentAiConfigDto HeroAi  { get; set; } = new();
    /// <summary>AI configuration for the Enemy agent.</summary>
    public AgentAiConfigDto EnemyAi { get; set; } = new();
}
