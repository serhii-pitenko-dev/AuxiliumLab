namespace AuxiliumLab.AiSandbox.SharedContracts;

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
