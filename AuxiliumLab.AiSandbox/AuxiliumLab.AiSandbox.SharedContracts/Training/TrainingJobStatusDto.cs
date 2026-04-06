namespace AuxiliumLab.AiSandbox.SharedContracts;

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
