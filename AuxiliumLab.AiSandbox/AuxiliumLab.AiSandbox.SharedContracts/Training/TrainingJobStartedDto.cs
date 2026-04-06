namespace AuxiliumLab.AiSandbox.SharedContracts;

public class TrainingJobStartedDto
{
    public Guid     JobId        { get; set; }
    public string   Algorithm    { get; set; } = string.Empty;
    public string   ExperimentId { get; set; } = string.Empty;
    public DateTime StartedAt    { get; set; }
}
