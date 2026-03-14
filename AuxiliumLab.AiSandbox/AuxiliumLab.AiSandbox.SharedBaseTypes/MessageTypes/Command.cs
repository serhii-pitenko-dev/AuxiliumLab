namespace AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

public record Command(Guid Id) : Message(Id);