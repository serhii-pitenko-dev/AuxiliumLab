using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.SharedContracts;

/// <summary>Describes a single cell transmitted to clients via SignalR.</summary>
public class SimulationCellDto
{
    public Coordinates      Position   { get; set; } = null!;
    public ObjectType       ObjectType { get; set; }
    public AgentEffectDto[] Effects    { get; set; } = [];
}
