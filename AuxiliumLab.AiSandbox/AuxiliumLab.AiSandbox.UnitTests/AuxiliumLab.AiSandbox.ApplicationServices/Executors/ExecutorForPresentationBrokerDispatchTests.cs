using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.ApplicationServices.Executors;

/// <summary>
/// Regression tests for the bug where <c>ExecutorForPresentation</c> published
/// <c>OnAgentMoveActionEvent</c> / <c>OnAgentToggleActionEvent</c> as the base type
/// <c>OnBaseAgentActionEvent</c>, causing the <c>MessageBroker</c> to store the message
/// under the wrong key and never invoke subscribers registered for the concrete types.
///
/// Root cause: <c>_messageBroker.Publish&lt;OnBaseAgentActionEvent&gt;(concreteEvent)</c> uses
/// <c>typeof(OnBaseAgentActionEvent)</c> as the dictionary key, while
/// <c>Subscribe&lt;OnAgentMoveActionEvent&gt;</c> registers under <c>typeof(OnAgentMoveActionEvent)</c>.
/// Fix: publish without an explicit type argument so C# infers the concrete type.
/// </summary>
[TestClass]
public class ExecutorForPresentationBrokerDispatchTests
{
    private MessageBroker _broker = null!;
    private AgentSnapshot _agentSnapshot = null!;
    private Guid _playgroundId;
    private Guid _agentId;

    [TestInitialize]
    public void Setup()
    {
        _broker = new MessageBroker();
        _playgroundId = Guid.NewGuid();
        _agentId = Guid.NewGuid();
        _agentSnapshot = new AgentSnapshot(
            Id: _agentId,
            Type: ObjectType.Hero,
            Speed: 3,
            SightRange: 2,
            IsRun: false,
            Stamina: 10,
            MaxStamina: 10,
            OrderInTurnQueue: 0);
    }

    // ── OnAgentMoveActionEvent ──────────────────────────────────────────────

    [TestMethod]
    public void Publish_ConcreteType_OnAgentMoveActionEvent_InvokesConcreteSubscriber()
    {
        // Arrange
        OnAgentMoveActionEvent? received = null;
        _broker.Subscribe<OnAgentMoveActionEvent>(e => received = e);

        var moveEvent = new OnAgentMoveActionEvent(
            Guid.NewGuid(), _playgroundId, _agentId,
            new Coordinates(1, 1), new Coordinates(2, 2),
            true, _agentSnapshot);

        // Act — publish as concrete type (the fixed behaviour)
        _broker.Publish(moveEvent);

        // Assert
        received.Should().NotBeNull("subscriber must be invoked when publishing the concrete type");
        received!.AgentId.Should().Be(_agentId);
        received.From.Should().Be(new Coordinates(1, 1));
        received.To.Should().Be(new Coordinates(2, 2));
        received.IsSuccess.Should().BeTrue();
    }

    [TestMethod]
    public void Publish_BaseType_OnAgentMoveActionEvent_DoesNotInvokeConcreteSubscriber()
    {
        // Arrange — demonstrates the OLD bug: explicit base-type publish misses the subscriber
        OnAgentMoveActionEvent? received = null;
        _broker.Subscribe<OnAgentMoveActionEvent>(e => received = e);

        OnBaseAgentActionEvent moveEvent = new OnAgentMoveActionEvent(
            Guid.NewGuid(), _playgroundId, _agentId,
            new Coordinates(1, 1), new Coordinates(2, 2),
            true, _agentSnapshot);

        // Act — publish using base type (the OLD broken behaviour)
        _broker.Publish<OnBaseAgentActionEvent>(moveEvent);

        // Assert — subscriber is NOT called because the broker key is the base type
        received.Should().BeNull(
            "publishing as OnBaseAgentActionEvent must NOT reach an OnAgentMoveActionEvent subscriber — " +
            "this documents the root cause of the original bug");
    }

    // ── OnAgentToggleActionEvent ────────────────────────────────────────────

    [TestMethod]
    public void Publish_ConcreteType_OnAgentToggleActionEvent_InvokesConcreteSubscriber()
    {
        // Arrange
        OnAgentToggleActionEvent? received = null;
        _broker.Subscribe<OnAgentToggleActionEvent>(e => received = e);

        var toggleEvent = new OnAgentToggleActionEvent(
            Guid.NewGuid(), _playgroundId, _agentId,
            AgentAction.Run, true, _agentSnapshot);

        // Act
        _broker.Publish(toggleEvent);

        // Assert
        received.Should().NotBeNull("subscriber must be invoked when publishing the concrete type");
        received!.AgentId.Should().Be(_agentId);
        received.AgentAction.Should().Be(AgentAction.Run);
        received.IsActivated.Should().BeTrue();
    }

    [TestMethod]
    public void Publish_BaseType_OnAgentToggleActionEvent_DoesNotInvokeConcreteSubscriber()
    {
        // Arrange — demonstrates the OLD bug
        OnAgentToggleActionEvent? received = null;
        _broker.Subscribe<OnAgentToggleActionEvent>(e => received = e);

        OnBaseAgentActionEvent toggleEvent = new OnAgentToggleActionEvent(
            Guid.NewGuid(), _playgroundId, _agentId,
            AgentAction.Run, true, _agentSnapshot);

        // Act — explicit base-type publish (old broken code)
        _broker.Publish<OnBaseAgentActionEvent>(toggleEvent);

        // Assert
        received.Should().BeNull(
            "publishing as OnBaseAgentActionEvent must NOT reach an OnAgentToggleActionEvent subscriber — " +
            "this documents the root cause of the original bug");
    }

    // ── Cross-type isolation ────────────────────────────────────────────────

    [TestMethod]
    public void Publish_OnAgentMoveActionEvent_DoesNotInvoke_OnAgentToggleActionEventSubscriber()
    {
        // Concrete types must not bleed into each other's subscribers
        OnAgentToggleActionEvent? toggleReceived = null;
        _broker.Subscribe<OnAgentToggleActionEvent>(e => toggleReceived = e);

        _broker.Publish(new OnAgentMoveActionEvent(
            Guid.NewGuid(), _playgroundId, _agentId,
            new Coordinates(0, 0), new Coordinates(1, 1),
            true, _agentSnapshot));

        toggleReceived.Should().BeNull("a Move event must not trigger a Toggle subscriber");
    }
}
