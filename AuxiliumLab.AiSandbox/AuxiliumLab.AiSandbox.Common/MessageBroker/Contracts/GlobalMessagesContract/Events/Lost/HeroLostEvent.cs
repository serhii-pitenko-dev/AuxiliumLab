using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Lost;

public record HeroLostEvent(Guid Id, Guid PlaygroundId, LostReason LostReason): Event(Id);