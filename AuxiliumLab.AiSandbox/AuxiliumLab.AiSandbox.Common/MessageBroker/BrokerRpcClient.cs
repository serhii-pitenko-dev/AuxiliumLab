using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;
using System.Collections.Concurrent;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker;

public class BrokerRpcClient : IBrokerRpcClient, IDisposable
{
    private readonly IMessageBroker _broker;

    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Response>> _pending
        = new();

    private readonly IDisposable _sub;

    public BrokerRpcClient(IMessageBroker broker)
    {
        _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        _sub = Subscribe();
    }
    /// <summary>
    /// Sends a request message and waits for a response asynchronously using a correlation-based RPC pattern.
    /// </summary>
    public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : notnull, Message
        where TResponse : notnull, Response 

    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request is not TRequest)
            throw new InvalidOperationException($"Request type mismatch. Expected {typeof(TRequest).Name}, got {request.GetType().Name}");

        var correlationId = request.Id;
        var tcs = new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Register the TaskCompletionSource so OnResponse can complete it when the correlated response arrives
        _pending[correlationId] = tcs;

        try
        {
            // Publish request with correlation ID
            _broker.Publish(request);

            // Wait for response with timeout support
            using var ctr = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            var response = await tcs.Task.ConfigureAwait(false);

            if (response is TResponse typedResponse)
            {
                return typedResponse;
            }

            throw new InvalidOperationException($"Response type mismatch. Expected {typeof(Response).Name}, got {response.GetType().Name}");
        }
        finally
        {
            _pending.TryRemove(correlationId, out _);
        }
    }

    private IDisposable Subscribe()
    {
        // Subscribe to all responses
        Action<Response> handler = OnResponse;
        _broker.Subscribe(handler);

        return new UnsubscribeToken(_broker, handler);
    }

    private void OnResponse<TResponse>(TResponse resp)  where TResponse : notnull, Response 
    {
        if (resp is not Response response)
            throw new InvalidOperationException("Received message is not of type Response.");

        if (resp?.CorrelationId == null)
            throw new InvalidOperationException("Response correlation ID is missing.");

        if (_pending.TryRemove(response.CorrelationId, out var tcs))
        {
            tcs.SetResult(response); // ← Completes the waiting Task
        }
    }

    public void Dispose()
    {
        _sub?.Dispose();

        // Cancel all pending requests
        foreach (var kvp in _pending)
        {
            kvp.Value.TrySetCanceled();
        }

        _pending.Clear();
    }

    /// <summary>
    /// A disposable token that encapsulates the unsubscription logic for a message broker handler.
    /// When disposed, it automatically unsubscribes the handler from the broker, ensuring proper cleanup
    /// and preventing memory leaks by breaking the reference chain between the broker and the handler.
    /// </summary>
    private class UnsubscribeToken : IDisposable
    {
        private readonly IMessageBroker _broker;
        private readonly Action<Response> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsubscribeToken"/> class.
        /// </summary>
        /// <param name="broker">The message broker from which to unsubscribe.</param>
        /// <param name="handler">The handler to unsubscribe when this token is disposed.</param>
        public UnsubscribeToken(IMessageBroker broker, Action<Response> handler)
        {
            _broker = broker;
            _handler = handler;
        }

        /// <summary>
        /// Unsubscribes the handler from the message broker, releasing the subscription.
        /// This ensures that the handler will no longer receive messages and allows
        /// the handler and its associated objects to be garbage collected.
        /// </summary>
        public void Dispose()
        {
            _broker.Unsubscribe(_handler);
        }
    }
}