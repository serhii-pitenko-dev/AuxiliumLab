namespace AuxiliumLab.AiSandbox.SharedContracts;

public class AggregationStepDto
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Execution mode string: "Training", "MassRandomAISimulation", "MassTrainedAISimulation".</summary>
    public string Mode { get; set; } = string.Empty;
}

public class AggregationIncrementalSweeperDto
{
    public int          SimulationCount { get; set; } = 1;
    public List<string> Properties      { get; set; } = [];
}

public class StartAggregationCommand
{
    public List<AggregationStepDto>          Steps                   { get; set; } = [];
    public int                               StandardSimulationCount { get; set; } = 100;
    public ModelType                         Algorithm               { get; set; } = ModelType.PPO;
    public AiPolicy                          PolicyType              { get; set; } = AiPolicy.MLP;
    public AggregationIncrementalSweeperDto? IncrementalSweep        { get; set; }
    public StartPpoTrainingCommand?          TrainingOverrides       { get; set; }
}

public class AggregationJobStartedDto
{
    public Guid                  JobId     { get; set; }
    public IReadOnlyList<string> StepNames { get; set; } = [];
    public DateTime              StartedAt { get; set; }
}

public enum AggregationJobState { Running, Completed, Failed }

public class AggregationJobStatusDto
{
    public Guid                JobId          { get; set; }
    public AggregationJobState State          { get; set; }
    public DateTime            StartedAt      { get; set; }
    public DateTime?           CompletedAt    { get; set; }
    public string?             ErrorMessage   { get; set; }
    public List<string>        StepNames      { get; set; } = [];
    public string?             CurrentStep    { get; set; }
    public int                 CompletedSteps { get; set; }
}
