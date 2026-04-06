using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.SharedContracts;

/// <summary>Describes a single agent's effects on a cell.</summary>
public class AgentEffectDto
{
    public Guid       AgentId   { get; set; }
    public ObjectType AgentType { get; set; }
    public EEffect[]  Effects   { get; set; } = [];
}
