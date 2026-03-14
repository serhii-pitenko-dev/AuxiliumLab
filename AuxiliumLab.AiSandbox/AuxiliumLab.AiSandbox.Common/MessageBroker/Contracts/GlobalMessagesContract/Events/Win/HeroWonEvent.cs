using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Win;

public record class HeroWonEvent(Guid Id, Guid PlaygroundId, WinReason WinReason): Event(Id);
