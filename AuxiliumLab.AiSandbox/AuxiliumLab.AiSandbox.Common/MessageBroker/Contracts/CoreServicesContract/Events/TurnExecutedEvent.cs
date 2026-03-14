using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;

public record TurnExecutedEvent(Guid Id, Guid PlaygroundId, int TurnNumber) : Event(Id);