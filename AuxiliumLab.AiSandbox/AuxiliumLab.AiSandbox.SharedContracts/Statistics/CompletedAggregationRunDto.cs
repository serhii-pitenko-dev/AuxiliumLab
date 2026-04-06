namespace AuxiliumLab.AiSandbox.SharedContracts;

public sealed class CompletedAggregationRunDto
{
    public Guid                          JobId       { get; set; }
    public DateTime                      StartedAt   { get; set; }
    public DateTime                      CompletedAt { get; set; }
    public List<AggregationStepResultDto> Steps      { get; set; } = [];
}
