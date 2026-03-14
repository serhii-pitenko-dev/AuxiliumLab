using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Responses;

public record AgentDecisionUseAbilityResponse(
    Guid Id,
    Guid AgentId,
    bool IsActivated,
    AgentAction ActionType,
    Guid CorrelationId,
    bool IsSuccess) : AgentDecisionBaseResponse(Id, AgentId, ActionType, CorrelationId, IsSuccess);