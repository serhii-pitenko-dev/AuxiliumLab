using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.Sb3Contract.Commands;

public record RequestSimulationCloseCommand(Guid Id, Guid GymId) : Command(Id);
