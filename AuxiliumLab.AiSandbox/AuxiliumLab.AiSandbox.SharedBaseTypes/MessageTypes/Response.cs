namespace AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

public record Response(Guid Id, Guid CorrelationId) : Message(Id);

