using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Responses;

public record class AgentDecisionBaseResponse(
    Guid Id, 
    Guid AgentId, 
    AgentAction ActionType, 
    Guid CorrelationId,
    bool IsSuccess): Response(Id, CorrelationId);