using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Responses;

public record AiReadyToActionsResponse(Guid Id, Guid PlaygroundId, Guid CorrelationId): Response(Id, CorrelationId);