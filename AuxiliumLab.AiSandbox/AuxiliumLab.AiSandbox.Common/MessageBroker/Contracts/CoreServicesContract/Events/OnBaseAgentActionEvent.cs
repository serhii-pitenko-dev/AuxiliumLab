using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;

public record OnBaseAgentActionEvent(
    Guid Id, 
    Guid PlaygroundId, 
    Guid AgentId, 
    AgentSnapshot AgentSnapshot) : Event(Id);