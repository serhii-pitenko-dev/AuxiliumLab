namespace AuxiliumLab.AiSandbox.SharedContracts;

public class SimulationJobStatusDto
{
    public Guid                                          JobId                { get; set; }
    public SimulationKind                                Kind                 { get; set; }
    public SharedBaseTypes.ValueObjects.SandboxStatus    State                { get; set; }
    public DateTime           StartedAt            { get; set; }
    public DateTime?          CompletedAt          { get; set; }
    public string?            ErrorMessage         { get; set; }
    public int                CompletedRuns        { get; set; }
    public int                TotalRuns            { get; set; }
    public long?              EstimatedRemainingMs { get; set; }
}
