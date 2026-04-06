using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.SharedContracts;

/// <summary>Sent after each agent move action.</summary>
public class AgentMovedDto
{
    public string           JobId        { get; set; } = string.Empty;
    public string           AgentId      { get; set; } = string.Empty;
    public ObjectType       AgentType    { get; set; }
    public Coordinates      From         { get; set; } = null!;
    public Coordinates      To           { get; set; } = null!;
    public bool             IsSuccess    { get; set; }
    public AgentSnapshotDto Agent        { get; set; } = new();
    public SimulationCellDto[] UpdatedCells { get; set; } = [];
}
