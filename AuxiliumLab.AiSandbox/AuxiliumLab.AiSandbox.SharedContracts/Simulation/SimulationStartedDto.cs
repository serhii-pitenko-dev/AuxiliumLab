namespace AuxiliumLab.AiSandbox.SharedContracts;

/// <summary>Sent once when the simulation starts; contains the full initial map and agent positions.</summary>
public class SimulationStartedDto
{
    public string             JobId    { get; set; } = string.Empty;
    public int                Width    { get; set; }
    public int                Height   { get; set; }
    public int                MaxTurns { get; set; }
    public SimulationCellDto[]  Cells  { get; set; } = [];
    public InitialAgentDto[]    Agents { get; set; } = [];
}
