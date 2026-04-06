namespace AuxiliumLab.AiSandbox.SharedContracts;

public class StartSingleSimulationCommand
{
    public SimulationKind                Kind            { get; set; } = SimulationKind.RandomAI;
    public ModelType                     Algorithm       { get; set; } = ModelType.PPO;
    /// <summary>Experiment folder name that identifies the trained model to use (e.g. "ppo_100000_4_…").</summary>
    public string?                       ExperimentId    { get; set; }
    public SimulationSandboxOverrideDto  SandboxSettings { get; set; } = new();
    /// <summary>Delay in milliseconds applied between each agent action during presentation.</summary>
    public int                           ActionDelayMs   { get; set; } = 500;

    /// <summary>AI configuration for the Hero agent.</summary>
    public AgentAiConfigDto HeroAi  { get; set; } = new();
    /// <summary>AI configuration for the Enemy agent.</summary>
    public AgentAiConfigDto EnemyAi { get; set; } = new();
}
