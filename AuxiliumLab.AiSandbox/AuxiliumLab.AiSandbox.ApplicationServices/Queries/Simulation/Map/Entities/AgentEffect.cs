using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.Entities;

public record struct AgentEffect(Guid AgentId, ObjectType AgentType, EEffect[] Effects)
{
    /// <summary>Converts this domain entity to a serialization-safe DTO.</summary>
    public AgentEffectDto ToDto() => new()
    {
        AgentId   = AgentId,
        AgentType = AgentType,
        Effects   = Effects
    };
}



