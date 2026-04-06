namespace AuxiliumLab.AiSandbox.SharedContracts;

/// <summary>Sent when the simulation ends (win, loss, or cancellation).</summary>
public class SimulationEndedDto
{
    public string  JobId     { get; set; } = string.Empty;
    public string  Outcome   { get; set; } = string.Empty;
    public string? Reason    { get; set; }
    public int     FinalTurn { get; set; }
}
