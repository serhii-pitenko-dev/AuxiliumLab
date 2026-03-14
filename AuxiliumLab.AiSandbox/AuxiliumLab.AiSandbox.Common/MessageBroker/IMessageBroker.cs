using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker;

public interface IMessageBroker
{
    void Publish<TMessage>(TMessage message) where TMessage : Message;
    void Subscribe<TMessage>(Action<TMessage> handler) where TMessage : notnull, Message;
    void Unsubscribe<TMessage>(Action<TMessage> handler) where TMessage : Message;
}