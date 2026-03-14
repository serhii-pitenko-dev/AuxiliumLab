using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Commands;

public record RequestAgentDecisionMakeCommand(
    Guid Id,
    Guid PlaygroundId,
    Guid AgentId) : Command(Id);