namespace AuxiliumLab.AiSandbox.SharedContracts;

public class StartAggregationCommand
{
    public List<AggregationStepDto>          Steps                   { get; set; } = [];
    public int                               StandardSimulationCount { get; set; } = 100;
    public ModelType                         Algorithm               { get; set; } = ModelType.PPO;
    public AiPolicy                          PolicyType              { get; set; } = AiPolicy.MLP;
    public AggregationIncrementalSweeperDto? IncrementalSweep        { get; set; }
    public StartPpoTrainingCommand           TrainingOverrides       { get; set; } = new();
    public TrainingSandboxSettingsDto         SandboxSettings         { get; set; } = new();
}
