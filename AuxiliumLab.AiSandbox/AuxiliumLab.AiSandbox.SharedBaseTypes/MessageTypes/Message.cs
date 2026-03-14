namespace AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

/// <summary>
/// Message base type for all messages sent via the message broker.
/// </summary>
/// <param name="Id">Not null if this message is part of a request-response pair, otherwise null.</param>
public abstract record Message(Guid Id);
