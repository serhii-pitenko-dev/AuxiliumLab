namespace AuxiliumLab.AiSandbox.SharedContracts;

/// <summary>Sent after every turn is completed.</summary>
public class TurnCompletedDto
{
    public string              JobId        { get; set; } = string.Empty;
    public int                 TurnNumber   { get; set; }
    public SimulationCellDto[] UpdatedCells { get; set; } = [];
}
