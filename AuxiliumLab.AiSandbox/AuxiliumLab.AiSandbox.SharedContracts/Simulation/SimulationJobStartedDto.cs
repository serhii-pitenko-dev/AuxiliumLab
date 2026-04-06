namespace AuxiliumLab.AiSandbox.SharedContracts;

public class SimulationJobStartedDto
{
    public Guid           JobId     { get; set; }
    public SimulationKind Kind      { get; set; }
    public DateTime       StartedAt { get; set; }
}
