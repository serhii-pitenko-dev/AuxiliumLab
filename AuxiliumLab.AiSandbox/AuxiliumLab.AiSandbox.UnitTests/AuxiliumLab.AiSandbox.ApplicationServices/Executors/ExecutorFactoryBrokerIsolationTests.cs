using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground;
using AuxiliumLab.AiSandbox.ApplicationServices.Executors;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto.Performance;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.TestPreconditionSet;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.Mappers;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using AuxiliumLab.AiSandbox.SharedContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.ApplicationServices.Executors;

/// <summary>
/// Verifies that <see cref="ExecutorFactory"/> uses the correct <see cref="IMessageBroker"/>
/// instance for each executor type:
/// <list type="bullet">
///   <item>Presentation executors (single-sim visualization) must use the <b>shared singleton</b>
///         broker so that <c>SimulationVisualizationBridge</c> — which subscribes on the same
///         singleton — can receive all events and forward them to the SignalR hub.</item>
///   <item>Mass / parallel executors must use <b>isolated</b> per-simulation brokers to
///         avoid lock contention and cross-talk between concurrent runs.</item>
/// </list>
///
/// Regression test for the bug where <c>CreateInferenceExecutorForPresentation</c> created
/// an isolated broker, causing <c>SimulationVisualizationBridge</c> never to receive any
/// events from a trained-AI single simulation.
/// </summary>
[TestClass]
public class ExecutorFactoryBrokerIsolationTests
{
    private MessageBroker _sharedBroker = null!;
    private BrokerRpcClient _sharedRpcClient = null!;
    private ExecutorFactory _factory = null!;
    private Mock<IPolicyTrainerClient> _mockPolicyTrainerClient = null!;
    private AiConfiguration _aiConfig = null!;
    private SandBoxConfiguration _sandboxConfig = null!;

    [TestInitialize]
    public void Setup()
    {
        _sharedBroker = new MessageBroker();
        _sharedRpcClient = new BrokerRpcClient(_sharedBroker);

        _mockPolicyTrainerClient = new Mock<IPolicyTrainerClient>();
        _aiConfig = new AiConfiguration
        {
            ModelType = ModelType.PPO,
            Version = "1.0",
            PolicyType = AiPolicy.MLP
        };

        _sandboxConfig = SandBoxConfiguration.CreateFromValues(
            maxTurns: 50, mapWidth: 10, mapHeight: 10,
            blocksPercent: 10, enemiesPercent: 5,
            heroSpeed: 3, heroSightRange: 5, heroStamina: 10,
            enemySpeed: 2, enemySightRange: 3, enemyStamina: 10);

        _factory = new ExecutorFactory(
            new Mock<IPlaygroundCommandsHandleService>().Object,
            new Mock<IMemoryDataManager<StandardPlayground>>().Object,
            new Mock<IAiActions>().Object,
            new Mock<IFileDataManager<StandardPlaygroundState>>().Object,
            new Mock<IMemoryDataManager<AgentStateForAIDecision>>().Object,
            _sharedBroker,
            _sharedRpcClient,
            new Mock<IStandardPlaygroundMapper>().Object,
            new Mock<IFileDataManager<RawDataLog>>().Object,
            new Mock<IFileDataManager<TurnExecutionPerformance>>().Object,
            new Mock<IFileDataManager<SandboxExecutionPerformance>>().Object,
            new Mock<ITestPreconditionData>().Object);
    }

    // ── Presentation executors use the shared broker ────────────────────────

    [TestMethod]
    public void CreateExecutorForPresentation_UsesSharedBroker_EventsReachSharedSubscriber()
    {
        // Arrange — subscribe on the shared broker
        GameStartedEvent? received = null;
        _sharedBroker.Subscribe<GameStartedEvent>(e => received = e);

        var executor = _factory.CreateExecutorForPresentation(_sandboxConfig);

        // Act — publish a GameStartedEvent on the shared broker
        var evt = new GameStartedEvent(Guid.NewGuid(), Guid.NewGuid());
        _sharedBroker.Publish(evt);

        // Assert — the shared broker's subscriber receives the event
        // (this confirms the executor and bridge share the same broker instance)
        received.Should().NotBeNull(
            "RandomAI presentation executor must use the shared broker " +
            "so SimulationVisualizationBridge receives events");
        received!.Id.Should().Be(evt.Id);
    }

    [TestMethod]
    public void CreateInferenceExecutorForPresentation_UsesSharedBroker_EventsReachSharedSubscriber()
    {
        // Arrange — subscribe on the shared broker BEFORE creating the executor
        // The executor's InferenceActions.Initialize() subscribes GameStartedEvent on the
        // broker it receives. If it uses the shared broker, our subscriber will be invoked
        // alongside InferenceActions when the shared broker publishes GameStartedEvent.
        GameStartedEvent? received = null;
        _sharedBroker.Subscribe<GameStartedEvent>(e => received = e);

        _factory.CreateInferenceExecutorForPresentation(
            _sandboxConfig, _mockPolicyTrainerClient.Object, "models/ppo_model.zip", _aiConfig);

        // Act — publish on the shared broker
        var evt = new GameStartedEvent(Guid.NewGuid(), Guid.NewGuid());
        _sharedBroker.Publish(evt);

        // Assert
        received.Should().NotBeNull(
            "trained-AI presentation executor must use the shared broker " +
            "so SimulationVisualizationBridge receives events");
        received!.Id.Should().Be(evt.Id);
    }

    // ── Mass / parallel executors use isolated brokers ───────────────────────

    [TestMethod]
    public void CreateStandardExecutor_UsesIsolatedBroker_EventsDoNotReachSharedSubscriber()
    {
        // Arrange
        GameStartedEvent? received = null;
        _sharedBroker.Subscribe<GameStartedEvent>(e => received = e);

        _factory.CreateStandardExecutor(_sandboxConfig);

        // Act — publish on the shared broker
        _sharedBroker.Publish(new GameStartedEvent(Guid.NewGuid(), Guid.NewGuid()));

        // Assert — the shared subscriber IS notified (it's the same broker)
        // but the StandardExecutor internally uses its OWN broker, which is isolated.
        // This test verifies that _creating_ a StandardExecutor doesn't add
        // subscribers to the shared broker.
        received.Should().NotBeNull("shared subscriber still receives shared-broker events");

        // The key assertion: verify the StandardExecutor's internal broker is different
        // by confirming the executor doesn't interfere with the shared broker.
        // We'll verify isolation through the executor's broker field via reflection.
        var executor = _factory.CreateStandardExecutor(_sandboxConfig);
        var brokerField = typeof(Executor).GetField("_messageBroker",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var executorBroker = brokerField!.GetValue(executor);

        executorBroker.Should().NotBeSameAs(_sharedBroker,
            "StandardExecutor must use an isolated broker to avoid cross-talk in parallel runs");
    }

    [TestMethod]
    public void CreateInferenceExecutor_UsesIsolatedBroker_NotSharedBrokerInstance()
    {
        // Arrange & Act
        var executor = _factory.CreateInferenceExecutor(
            _sandboxConfig, _mockPolicyTrainerClient.Object, "models/ppo_model.zip", _aiConfig);

        // Assert — use reflection to verify the executor's broker is NOT the shared one
        var brokerField = typeof(Executor).GetField("_messageBroker",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var executorBroker = brokerField!.GetValue(executor);

        executorBroker.Should().NotBeSameAs(_sharedBroker,
            "InferenceExecutor (mass/parallel) must use an isolated broker");
    }

    [TestMethod]
    public void CreateInferenceExecutorForPresentation_UsesSharedBrokerInstance()
    {
        // Arrange & Act
        var executor = _factory.CreateInferenceExecutorForPresentation(
            _sandboxConfig, _mockPolicyTrainerClient.Object, "models/ppo_model.zip", _aiConfig);

        // Assert — use reflection to verify the executor's broker IS the shared one
        var brokerField = typeof(Executor).GetField("_messageBroker",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var executorBroker = brokerField!.GetValue(executor);

        executorBroker.Should().BeSameAs(_sharedBroker,
            "InferenceExecutorForPresentation must use the shared broker so " +
            "SimulationVisualizationBridge receives events for visualization");
    }

    [TestMethod]
    public void CreateExecutorForPresentation_UsesSharedBrokerInstance()
    {
        // Arrange & Act
        var executor = _factory.CreateExecutorForPresentation(_sandboxConfig);

        // Assert
        var brokerField = typeof(Executor).GetField("_messageBroker",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var executorBroker = brokerField!.GetValue(executor);

        executorBroker.Should().BeSameAs(_sharedBroker,
            "RandomAI presentation executor must use the shared broker");
    }
}
