using System.Collections.Concurrent;

namespace AuxiliumLab.AiSandbox.Common.MessageBroker;

/// <summary>
/// Maps each gym's unique ID to its isolated <see cref="IMessageBroker"/> instance.
///
/// During training, <c>TrainingRunner</c> creates one isolated <c>MessageBroker</c> per gym
/// and registers it here. <c>SimulationService</c> (gRPC) uses this registry to route
/// Reset / Step / Close calls to the correct per-gym broker instead of broadcasting on the
/// shared singleton broker — which would never be seen by the per-gym <c>Sb3Actions</c>.
/// </summary>
public sealed class GymBrokerRegistry
{
    private readonly ConcurrentDictionary<Guid, IMessageBroker> _brokers = new();

    public void Register(Guid gymId, IMessageBroker broker)
        => _brokers[gymId] = broker;

    public void Unregister(Guid gymId)
        => _brokers.TryRemove(gymId, out _);

    /// <summary>Returns the isolated broker for the given gym, or <c>null</c> if not registered.</summary>
    public IMessageBroker? Get(Guid gymId)
        => _brokers.TryGetValue(gymId, out var broker) ? broker : null;
}
