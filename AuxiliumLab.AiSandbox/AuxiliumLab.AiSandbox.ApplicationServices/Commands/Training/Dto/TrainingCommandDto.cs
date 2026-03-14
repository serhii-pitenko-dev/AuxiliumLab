namespace AuxiliumLab.AiSandbox.ApplicationServices.Commands.Training.Dto;

/// <summary>
/// PPO-specific hyperparameters for a training request.
/// Mirrors the PPO block in training-settings.json.
/// All fields are optional; omitted values fall back to training-settings.json defaults.
/// </summary>
public class PpoHyperparametersDto
{
    public int? TotalTimesteps { get; set; }
    public double? LearningRate { get; set; }
    public int? NSteps { get; set; }
    public int? BatchSize { get; set; }
    public int? NEpochs { get; set; }
    public double? Gamma { get; set; }
    public double? GaeLambda { get; set; }
    public double? ClipRange { get; set; }
    public double? EntCoef { get; set; }
    public int? Seed { get; set; }
    /// <summary>
    /// Number of parallel gym environments. Defaults to the physical core count.
    /// Set to 1 for lightweight or test runs.
    /// </summary>
    public int? NEnvs { get; set; }
}

/// <summary>Sandbox (map + agent) parameters for a training request.</summary>
public class TrainingSandboxSettingsDto
{
    public int? MaxTurns { get; set; }
    public int? MapWidth { get; set; }
    public int? MapHeight { get; set; }
    public double? BlocksPercent { get; set; }
    public double? EnemiesPercent { get; set; }
    public int? HeroSpeed { get; set; }
    public int? HeroSightRange { get; set; }
    public int? HeroStamina { get; set; }
    public int? EnemySpeed { get; set; }
}

/// <summary>Reward shaping overrides for a training request.</summary>
public class RewardSettingsDto
{
    public float? StepPenalty { get; set; }
    public float? WinReward { get; set; }
    public float? LossReward { get; set; }
}

/// <summary>Command to start a PPO training run.</summary>
public class StartPpoTrainingCommand
{
    /// <summary>PPO hyperparameter overrides. Null fields use training-settings.json defaults.</summary>
    public PpoHyperparametersDto? Hyperparameters { get; set; }

    /// <summary>Sandbox configuration overrides. Null fields use appsettings.json SandBox defaults.</summary>
    public TrainingSandboxSettingsDto? SandboxSettings { get; set; }

    /// <summary>Reward shaping overrides.</summary>
    public RewardSettingsDto? RewardSettings { get; set; }
}

/// <summary>Generic command placeholder for unimplemented algorithms.</summary>
public class StartGenericTrainingCommand { }

/// <summary>Returned immediately when a training job is accepted (202 pattern).</summary>
public record TrainingJobStartedDto(
    Guid JobId,
    string Algorithm,
    string ExperimentId,
    DateTime StartedAt);
