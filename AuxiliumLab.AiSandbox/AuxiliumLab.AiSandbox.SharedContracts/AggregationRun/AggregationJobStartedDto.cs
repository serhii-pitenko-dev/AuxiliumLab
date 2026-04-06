namespace AuxiliumLab.AiSandbox.SharedContracts;

public class AggregationJobStartedDto
{
    public Guid                  JobId     { get; set; }
    public IReadOnlyList<string> StepNames { get; set; } = [];
    public DateTime              StartedAt { get; set; }
}
