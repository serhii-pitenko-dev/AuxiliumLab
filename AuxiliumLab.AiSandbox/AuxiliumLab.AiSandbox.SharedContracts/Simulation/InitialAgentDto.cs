using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.SharedContracts;

/// <summary>Initial position and stats of an agent at simulation start.</summary>
public class InitialAgentDto
{
    public string          AgentId   { get; set; } = string.Empty;
    public ObjectType      AgentType { get; set; }
    public Coordinates     Position  { get; set; } = null!;
    public AgentSnapshotDto Snapshot { get; set; } = new();
}
