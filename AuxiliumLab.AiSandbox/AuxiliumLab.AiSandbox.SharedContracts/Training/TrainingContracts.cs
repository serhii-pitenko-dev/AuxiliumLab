namespace AuxiliumLab.AiSandbox.SharedContracts;

public class PpoHyperparametersDto
{
    public int    TotalTimesteps { get; set; }
    public double LearningRate   { get; set; }
    public int    NSteps         { get; set; }
    public int    BatchSize      { get; set; }
    public int    NEpochs        { get; set; }
    public double Gamma          { get; set; }
    public double GaeLambda      { get; set; }
    public double ClipRange      { get; set; }
    public double EntCoef        { get; set; }
    public int    Seed           { get; set; }
    /// <summary>Number of parallel gym environments.</summary>
    public int    NEnvs          { get; set; }
}

public class TrainingSandboxSettingsDto
{
    public int    MaxTurns       { get; set; }
    public int    MapWidth       { get; set; }
    public int    MapHeight      { get; set; }
    public double BlocksPercent  { get; set; }
    public double EnemiesPercent { get; set; }
    public int    HeroSpeed      { get; set; }
    public int    HeroSightRange { get; set; }
    public int    HeroStamina      { get; set; }
    public int    EnemySpeed       { get; set; }
    public int    EnemySightRange  { get; set; }
    public int    EnemyStamina     { get; set; }
}

public class RewardSettingsDto
{
    public float StepPenalty { get; set; }
    public float WinReward   { get; set; }
    public float LossReward  { get; set; }
}

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

public class StartGenericTrainingCommand { }

public class TrainingJobStartedDto
{
    public Guid     JobId        { get; set; }
    public string   Algorithm    { get; set; } = string.Empty;
    public string   ExperimentId { get; set; } = string.Empty;
    public DateTime StartedAt    { get; set; }
}

public enum TrainingJobState { Running, Completed, Failed }

public class TrainingJobStatusDto
{
    public Guid             JobId                { get; set; }
    public string           Algorithm            { get; set; } = string.Empty;
    public string           ExperimentId         { get; set; } = string.Empty;
    public TrainingJobState State                { get; set; }
    public DateTime         StartedAt            { get; set; }
    public DateTime?        CompletedAt          { get; set; }
    public string?          ErrorMessage         { get; set; }
    public long?            EstimatedRemainingMs { get; set; }
    public int              TimestepsDone        { get; set; }
    public int              TotalTimesteps       { get; set; }
    public int              NumEnvironments      { get; set; }
    public string?          RunId                { get; set; }
}

public class TrainedModelInfoDto
{
    public string                   Algorithm      { get; set; } = string.Empty;
    public string                   ExperimentId   { get; set; } = string.Empty;
    public string                   ModelFilePath  { get; set; } = string.Empty;
    public DateTime                 TrainedAt      { get; set; }
    public TrainingPreconditionsDto? Preconditions  { get; set; }
    public bool                     IsFailed       { get; set; }
    public string?                  ErrorMessage   { get; set; }
    /// <summary>Which agent this model was trained for (HERO or ENEMY).</summary>
    public string                   AgentType      { get; set; } = "HERO";
}

public class TrainingPreconditionsDto
{
    public string                     Algorithm       { get; set; } = string.Empty;
    public string                     ExperimentId    { get; set; } = string.Empty;
    public Dictionary<string, string> Hyperparameters { get; set; } = [];
    public int                        MaxTurns        { get; set; }
    public int                        MapWidth        { get; set; }
    public int                        MapHeight       { get; set; }
    public double                     BlocksPercent   { get; set; }
    public double                     EnemiesPercent  { get; set; }
    public int                        HeroSpeed       { get; set; }
    public int                        HeroSightRange  { get; set; }
    public int                        HeroStamina     { get; set; }
    public int                        EnemySpeed      { get; set; }
    public int                        EnemySightRange { get; set; }
    public int                        EnemyStamina    { get; set; }
    public float                      StepPenalty     { get; set; }
    public float                      WinReward       { get; set; }
    public float                      LossReward      { get; set; }
    public DateTime                   StartedAt       { get; set; }
    /// <summary>Which agent type was trained (HERO or ENEMY).</summary>
    public string                     TraineeAgent    { get; set; } = "HERO";
    /// <summary>Opponent AI type during training (Random / PPO / A2C / DQN).</summary>
    public string                     OpponentAiType  { get; set; } = "Random";
    /// <summary>Opponent model experiment id (null/empty if Random).</summary>
    public string?                    OpponentModelId { get; set; }
}
