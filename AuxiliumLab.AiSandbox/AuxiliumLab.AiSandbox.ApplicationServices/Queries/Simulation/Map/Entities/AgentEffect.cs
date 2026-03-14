using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.Entities;

public record struct AgentEffect(Guid AgentId, ObjectType AgentType, EEffect[] Effects);



