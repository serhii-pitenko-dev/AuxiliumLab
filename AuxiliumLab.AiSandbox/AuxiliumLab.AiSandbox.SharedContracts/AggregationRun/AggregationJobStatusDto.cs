namespace AuxiliumLab.AiSandbox.SharedContracts;

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
