namespace AuxiliumLab.AiSandbox.SharedContracts;

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
