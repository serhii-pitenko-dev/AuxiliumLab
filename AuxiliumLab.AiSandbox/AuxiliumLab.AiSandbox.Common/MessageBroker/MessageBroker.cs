using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;
using System.Collections.Concurrent;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker;

/// <summary>
/// Implements a thread-safe in-memory message broker using the publish-subscribe pattern.
/// This broker allows components to communicate without direct coupling by sending and receiving strongly-typed messages.
/// </summary>
/// <remarks>
/// The MessageBroker maintains a dictionary where each message type maps to a list of subscriber handlers.
/// Thread safety is achieved through:
/// - ConcurrentDictionary for managing the collection of message type subscriptions
/// - Lock statements when modifying or invoking handler lists to prevent race conditions
/// 
/// Message flow:
/// 1. Components subscribe to specific message types by providing a handler (Action delegate)
/// 2. When a message is published, all registered handlers for that message type are invoked synchronously
/// 3. Components can unsubscribe to stop receiving messages
/// 
/// All messages must inherit from BaseMessage and be non-null reference types.
/// </remarks>
public class MessageBroker : IMessageBroker
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();

    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribersOnResponse = new();

    public void Publish<TMessage>(TMessage message) where TMessage : notnull, Message
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (message is Response response)
        {
            PublishResponse(response);
        }

        var messageType = typeof(TMessage);
        if (_subscribers.TryGetValue(messageType, out var handlers))
        {
            lock (handlers)
            {
                foreach (var handler in handlers.ToList())
                {
                    ((Action<TMessage>)handler).Invoke(message);
                }
            }
        }
    }

    private void PublishResponse(Response message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        if (_subscribersOnResponse.TryGetValue(typeof(Response), out var handlers))
        {
            lock (handlers)
            {
                foreach (var handler in handlers.ToList())
                {
                    ((Action<Response>)handler).Invoke(message);
                }
            }
        }
    }

    public void Subscribe<TMessage>(Action<TMessage> handler) where TMessage : notnull, Message
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var messageType = typeof(TMessage);
        if (messageType == typeof(Response))
        {
            SubscribeResponse(handler);
            return;
        }

        var standardHandlers = _subscribers.GetOrAdd(messageType, _ => new List<Delegate>());

        lock (standardHandlers)
        {
            standardHandlers.Add(handler);
        }
    }

    private void SubscribeResponse<TMessage>(Action<TMessage> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        var messageType = typeof(Response);
        var responseHandlers = _subscribersOnResponse.GetOrAdd(messageType, _ => new List<Delegate>());

        lock (responseHandlers)
        {
            responseHandlers.Add(handler);
        }
    }

    public void Unsubscribe<TMessage>(Action<TMessage> handler) where TMessage : notnull, Message
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var messageType = typeof(TMessage);
        if (_subscribers.TryGetValue(messageType, out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }
    }
}