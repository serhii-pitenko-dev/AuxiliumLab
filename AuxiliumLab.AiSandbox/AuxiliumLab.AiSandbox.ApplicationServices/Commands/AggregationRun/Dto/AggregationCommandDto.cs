using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Training.Dto;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Commands.AggregationRun.Dto;

/// <summary>A single step in an aggregation run.</summary>
public class AggregationStepDto
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Execution mode string: "Training", "MassRandomAISimulation", "MassTrainedAISimulation".</summary>
    public string Mode { get; set; } = string.Empty;
}

/// <summary>Command to start an aggregation run.</summary>
public class StartAggregationCommand
{
    /// <summary>Ordered steps to execute. Falls back to aggregation-settings.json when empty.</summary>
    public List<AggregationStepDto> Steps { get; set; } = [];

    /// <summary>Number of standard simulation runs per mass-run step.</summary>
    public int StandardSimulationCount { get; set; } = 100;

    /// <summary>RL algorithm for training and trained simulation steps.</summary>
    public ModelType Algorithm { get; set; } = ModelType.PPO;

    /// <summary>Policy type (e.g. MLP) for inference.</summary>
    public AiPolicy PolicyType { get; set; } = AiPolicy.MLP;

    /// <summary>Optional incremental sweep settings for mass-run steps.</summary>
    public AggregationIncrementalSweeperDto? IncrementalSweep { get; set; }

    /// <summary>
    /// Optional PPO hyperparameter overrides applied to the Training step.
    /// When null the defaults in training-settings.json are used.
    /// Set <see cref="PpoHyperparametersDto.NEnvs"/> to 1 for lightweight / test runs.
    /// </summary>
    public StartPpoTrainingCommand? TrainingOverrides { get; set; }
}

/// <summary>Incremental sweep settings embedded in an aggregation command.</summary>
public class AggregationIncrementalSweeperDto
{
    public int SimulationCount { get; set; } = 1;
    public List<string> Properties { get; set; } = [];
}

/// <summary>Returned immediately when an aggregation job is accepted.</summary>
public record AggregationJobStartedDto(
    Guid JobId,
    IReadOnlyList<string> StepNames,
    DateTime StartedAt);
