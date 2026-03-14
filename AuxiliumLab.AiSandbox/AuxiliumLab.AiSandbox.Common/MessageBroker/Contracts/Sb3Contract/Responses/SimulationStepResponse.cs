using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.Sb3Contract.Responses;

public record SimulationStepResponse(
    Guid Id,
    Guid GymId,
    Guid CorrelationId,
    float[] Observation,
    float Reward,
    bool Terminated,
    bool Truncated,
    Dictionary<string, string> Info) : Response(Id, CorrelationId);
