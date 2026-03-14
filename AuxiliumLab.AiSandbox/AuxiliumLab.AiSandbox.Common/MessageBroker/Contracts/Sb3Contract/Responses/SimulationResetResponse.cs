using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.Sb3Contract.Responses;

public record SimulationResetResponse(
    Guid Id,
    Guid GymId,
    Guid CorrelationId,
    float[] Observation,
    Dictionary<string, string> Info) : Response(Id, CorrelationId);
