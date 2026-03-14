using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;

public record OnAgentMoveActionEvent(
    Guid Id, 
    Guid PlaygroundId, 
    Guid AgentId,
    Coordinates From,
    Coordinates To,
    bool IsSuccess,
    AgentSnapshot AgentSnapshot) : OnBaseAgentActionEvent(Id, PlaygroundId, AgentId, AgentSnapshot);
