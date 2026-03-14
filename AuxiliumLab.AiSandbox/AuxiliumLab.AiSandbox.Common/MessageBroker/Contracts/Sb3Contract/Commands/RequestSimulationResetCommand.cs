using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.Sb3Contract.Commands;

public record RequestSimulationResetCommand(Guid Id, Guid GymId, int? Seed) : Command(Id);
