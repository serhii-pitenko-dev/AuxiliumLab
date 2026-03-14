using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.Sb3Contract.Commands;

public record RequestSimulationStepCommand(Guid Id, Guid GymId, int Action) : Command(Id);
