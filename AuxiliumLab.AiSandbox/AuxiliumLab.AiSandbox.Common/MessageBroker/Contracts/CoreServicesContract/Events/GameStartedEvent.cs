using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;

public record GameStartedEvent(Guid Id, Guid PlaygroundId) : Event(Id);

