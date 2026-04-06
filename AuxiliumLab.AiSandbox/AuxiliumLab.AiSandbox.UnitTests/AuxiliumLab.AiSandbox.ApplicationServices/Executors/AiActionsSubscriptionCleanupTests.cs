using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.Ai.GrpcClients;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Commands;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Responses;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using AuxiliumLab.AiSandbox.SharedContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.ApplicationServices.Executors;

/// <summary>
/// Verifies that <see cref="InferenceActions"/> and <see cref="RandomActions"/> properly
/// unsubscribe from the shared <see cref="IMessageBroker"/> when disposed.
///
/// Regression test for the stop-and-restart bug: old handlers leaked on the shared broker,
/// causing the old <c>InferenceActions.OnDecisionRequest</c> to fire for a new simulation.
/// Because <see cref="MemoryDataManager{T}.LoadObject"/> throws <see cref="KeyNotFoundException"/>
/// for unknown agent IDs (not null), and <c>MessageBroker.Publish</c> has no try-catch,
/// the exception killed the handler chain and prevented the new handler from executing.
/// The <c>BrokerRpcClient.RequestAsync</c> then hung forever, silently killing the simulation.
/// </summary>
[TestClass]
public class AiActionsSubscriptionCleanupTests
{
    private MessageBroker _broker = null!;

    [TestInitialize]
    public void Setup()
    {
        _broker = new MessageBroker();
    }

    // ── InferenceActions ─────────────────────────────────────────────────────

    [TestMethod]
    public void InferenceActions_Initialize_SubscribesToGameStartedEvent()
    {
        var sut = CreateInferenceActions();
        sut.Initialize();

        int callCount = 0;
        _broker.Subscribe<AiReadyToActionsResponse>(_ => callCount++);

        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), Guid.NewGuid()));
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void InferenceActions_Dispose_UnsubscribesFromGameStartedEvent()
    {
        var sut = CreateInferenceActions();
        sut.Initialize();
        sut.Dispose();

        int callCount = 0;
        _broker.Subscribe<AiReadyToActionsResponse>(_ => callCount++);

        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), Guid.NewGuid()));
        Assert.AreEqual(0, callCount, "Disposed InferenceActions should not respond to GameStartedEvent");
    }

    [TestMethod]
    public void InferenceActions_Dispose_UnsubscribesFromDecisionRequest()
    {
        var agentId = Guid.NewGuid();
        var playgroundId = Guid.NewGuid();
        var agentStore = new MemoryDataManager<AgentStateForAIDecision>();

        var sut = CreateInferenceActions(agentStore);
        sut.Initialize();

        // Trigger GameStartedEvent to set _playgroundId
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), playgroundId));

        sut.Dispose();

        // Publishing a decision request to the old store should NOT throw
        // because the handler was unsubscribed
        var cmd = new RequestAgentDecisionMakeCommand(Guid.NewGuid(), playgroundId, agentId);
        _broker.Publish(cmd); // Would throw KeyNotFoundException if handler still active
    }

    [TestMethod]
    public void InferenceActions_SecondInstance_WorksAfterFirstDisposed()
    {
        // Simulate stop-and-restart: first instance subscribes then disposes,
        // second instance subscribes. Only the second should handle events.
        var sut1 = CreateInferenceActions();
        sut1.Initialize();
        sut1.Dispose();

        var sut2 = CreateInferenceActions();
        sut2.Initialize();

        int responseCount = 0;
        _broker.Subscribe<AiReadyToActionsResponse>(_ => responseCount++);

        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), Guid.NewGuid()));
        Assert.AreEqual(1, responseCount, "Only the second (active) instance should respond");

        sut2.Dispose();
    }

    // ── RandomActions ────────────────────────────────────────────────────────

    [TestMethod]
    public void RandomActions_Initialize_SubscribesToGameStartedEvent()
    {
        var sut = CreateRandomActions();
        sut.Initialize();

        int callCount = 0;
        _broker.Subscribe<AiReadyToActionsResponse>(_ => callCount++);

        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), Guid.NewGuid()));
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void RandomActions_Dispose_UnsubscribesFromGameStartedEvent()
    {
        var sut = CreateRandomActions();
        sut.Initialize();
        sut.Dispose();

        int callCount = 0;
        _broker.Subscribe<AiReadyToActionsResponse>(_ => callCount++);

        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), Guid.NewGuid()));
        Assert.AreEqual(0, callCount, "Disposed RandomActions should not respond to GameStartedEvent");
    }

    [TestMethod]
    public void RandomActions_Dispose_UnsubscribesFromDecisionRequest()
    {
        var agentStore = new MemoryDataManager<AgentStateForAIDecision>();

        var sut = CreateRandomActions(agentStore);
        sut.Initialize();
        sut.Dispose();

        // Should NOT throw because the handler was unsubscribed
        var cmd = new RequestAgentDecisionMakeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _broker.Publish(cmd);
    }

    [TestMethod]
    public void RandomActions_SecondInstance_WorksAfterFirstDisposed()
    {
        var sut1 = CreateRandomActions();
        sut1.Initialize();
        sut1.Dispose();

        var sut2 = CreateRandomActions();
        sut2.Initialize();

        int responseCount = 0;
        _broker.Subscribe<AiReadyToActionsResponse>(_ => responseCount++);

        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), Guid.NewGuid()));
        Assert.AreEqual(1, responseCount, "Only the second (active) instance should respond");

        sut2.Dispose();
    }

    // ── Executor IDisposable chain ───────────────────────────────────────────

    [TestMethod]
    public void DisposableAiActions_DisposedThroughExecutorPattern()
    {
        // Verify that casting to IDisposable and calling Dispose works correctly
        IAiActions actions = CreateInferenceActions();
        actions.Initialize();

        int callCount = 0;
        _broker.Subscribe<AiReadyToActionsResponse>(_ => callCount++);

        // Before dispose
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), Guid.NewGuid()));
        Assert.AreEqual(1, callCount);

        // Dispose via IDisposable (as Executor.Dispose does)
        (actions as IDisposable)?.Dispose();

        // After dispose — should NOT fire
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), Guid.NewGuid()));
        Assert.AreEqual(1, callCount, "Handler should be unsubscribed after dispose");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private InferenceActions CreateInferenceActions(MemoryDataManager<AgentStateForAIDecision>? agentStore = null)
    {
        return new InferenceActions(
            _broker,
            agentStore ?? new MemoryDataManager<AgentStateForAIDecision>(),
            new Mock<IPolicyTrainerClient>().Object,
            "test-model.zip",
            new AiConfiguration
            {
                ModelType = ModelType.PPO,
                Version = "1.0",
                PolicyType = AiPolicy.MLP
            });
    }

    private RandomActions CreateRandomActions(MemoryDataManager<AgentStateForAIDecision>? agentStore = null)
    {
        return new RandomActions(
            _broker,
            agentStore ?? new MemoryDataManager<AgentStateForAIDecision>());
    }
}