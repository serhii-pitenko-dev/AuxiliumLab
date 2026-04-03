using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.PolicyTrainer;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Commands;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Responses;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using AuxiliumLab.AiSandbox.SharedContracts;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.AiTrainingOrchestrator;

/// <summary>
/// Tests that <see cref="InferenceActions"/> sends the correct <c>AlgorithmType</c>
/// in every <see cref="ActRequest"/> and logs diagnostic warnings when the Act RPC
/// returns <c>success=false</c> or throws.
/// Regression tests for the "hero always moves up" bug where the Python service could
/// not infer the algorithm from the filename <c>model.zip</c>.
/// </summary>
[TestClass]
public class InferenceActionsAlgorithmTypeTests
{
    private MessageBroker _broker = null!;
    private MemoryDataManager<AgentStateForAIDecision> _agentStore = null!;
    private Mock<IPolicyTrainerClient> _clientMock = null!;
    private Mock<ILogger<InferenceActions>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _broker = new MessageBroker();
        _agentStore = new MemoryDataManager<AgentStateForAIDecision>();
        _clientMock = new Mock<IPolicyTrainerClient>();
        _loggerMock = new Mock<ILogger<InferenceActions>>();
    }

    // ── AlgorithmType is sent correctly ──────────────────────────────────────

    [TestMethod]
    public async Task OnDecisionRequest_SendsAlgorithmTypeAsPpo_WhenModelTypeIsPPO()
    {
        await VerifyAlgorithmTypeSent(ModelType.PPO, "ppo");
    }

    [TestMethod]
    public async Task OnDecisionRequest_SendsAlgorithmTypeAsA2c_WhenModelTypeIsA2C()
    {
        await VerifyAlgorithmTypeSent(ModelType.A2C, "a2c");
    }

    [TestMethod]
    public async Task OnDecisionRequest_SendsAlgorithmTypeAsDqn_WhenModelTypeIsDQN()
    {
        await VerifyAlgorithmTypeSent(ModelType.DQN, "dqn");
    }

    // ── Successful response produces correct action ─────────────────────────

    [TestMethod]
    public async Task OnDecisionRequest_PublishesCorrectAction_WhenActSucceeds()
    {
        const int expectedAction = 3; // Move right
        _clientMock
            .Setup(c => c.ActAsync(It.IsAny<ActRequest>(), default))
            .ReturnsAsync(new ActResponse { Action = expectedAction, Success = true });

        var (sut, playgroundId, agentId) = CreateInitializedSut(ModelType.PPO);

        AgentDecisionBaseResponse? published = null;
        _broker.Subscribe<AgentDecisionBaseResponse>(r => published = r);

        var cmd = new RequestAgentDecisionMakeCommand(Guid.NewGuid(), playgroundId, agentId);
        _broker.Publish(cmd);

        // Wait for the async Task.Run inside OnDecisionRequest to complete
        await Task.Delay(500);

        Assert.IsNotNull(published, "Expected a decision response to be published");
    }

    // ── Failure logging (once per episode) ───────────────────────────────────

    [TestMethod]
    public async Task OnDecisionRequest_LogsWarningOnce_WhenActReturnsFalse()
    {
        _clientMock
            .Setup(c => c.ActAsync(It.IsAny<ActRequest>(), default))
            .ReturnsAsync(new ActResponse { Action = 0, Success = false, ErrorMessage = "model not found" });

        var (sut, playgroundId, agentId) = CreateInitializedSut(ModelType.PPO);

        var cmd = new RequestAgentDecisionMakeCommand(Guid.NewGuid(), playgroundId, agentId);

        // Publish twice — the warning should only fire once
        _broker.Publish(cmd);
        await Task.Delay(300);
        _broker.Publish(cmd);
        await Task.Delay(300);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Act RPC failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Warning should be logged exactly once per episode to avoid flooding");
    }

    [TestMethod]
    public async Task OnDecisionRequest_LogsWarningOnce_WhenActThrows()
    {
        _clientMock
            .Setup(c => c.ActAsync(It.IsAny<ActRequest>(), default))
            .ThrowsAsync(new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Unavailable, "connection refused")));

        var (sut, playgroundId, agentId) = CreateInitializedSut(ModelType.PPO);

        var cmd = new RequestAgentDecisionMakeCommand(Guid.NewGuid(), playgroundId, agentId);

        _broker.Publish(cmd);
        await Task.Delay(300);
        _broker.Publish(cmd);
        await Task.Delay(300);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("gRPC ActAsync threw")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Exception warning should be logged exactly once per episode");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task VerifyAlgorithmTypeSent(ModelType modelType, string expectedAlgorithmType)
    {
        ActRequest? captured = null;
        _clientMock
            .Setup(c => c.ActAsync(It.IsAny<ActRequest>(), default))
            .Callback<ActRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new ActResponse { Action = 0, Success = true });

        var (sut, playgroundId, agentId) = CreateInitializedSut(modelType);

        var cmd = new RequestAgentDecisionMakeCommand(Guid.NewGuid(), playgroundId, agentId);
        _broker.Publish(cmd);

        await Task.Delay(500);

        Assert.IsNotNull(captured, "ActAsync should have been called");
        Assert.AreEqual(expectedAlgorithmType, captured.AlgorithmType,
            $"AlgorithmType should be '{expectedAlgorithmType}' for ModelType.{modelType}");
    }

    /// <summary>
    /// Creates an InferenceActions, initializes it, fires a GameStarted event to set the
    /// playground ID, and stores a dummy agent so OnDecisionRequest doesn't bail early.
    /// </summary>
    private (InferenceActions sut, Guid playgroundId, Guid agentId) CreateInitializedSut(ModelType modelType)
    {
        var playgroundId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        // Store a minimal agent so the handler gets past the null check.
        var agent = new AgentStateForAIDecision(
            PlaygroundId: playgroundId,
            Id: agentId,
            Type: ObjectType.Hero,
            Coordinates: new Coordinates(5, 5),
            Speed: 1,
            SightRange: 2,
            IsRun: false,
            Stamina: 100,
            MaxStamina: 100,
            VisibleCells: CreateEmptyGrid(sightRange: 2, center: new Coordinates(5, 5)),
            AvailableLimitedActions: new List<AgentAction> { AgentAction.Run },
            ExecutedActions: new List<AgentAction>());

        _agentStore.AddOrUpdate(agentId, agent);

        var sut = new InferenceActions(
            _broker,
            _agentStore,
            _clientMock.Object,
            "/models/experiment/model.zip",
            new AiConfiguration
            {
                ModelType = modelType,
                Version = "1.0",
                PolicyType = AiPolicy.MLP
            },
            ObjectType.Hero,
            _loggerMock.Object);

        sut.Initialize();

        // Fire game started event to set _playgroundId
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), playgroundId));

        return (sut, playgroundId, agentId);
    }

    private static List<VisibleCellData> CreateEmptyGrid(int sightRange, Coordinates center)
    {
        var cells = new List<VisibleCellData>();
        int gridSize = 2 * sightRange + 1;
        for (int row = 0; row < gridSize; row++)
        for (int col = 0; col < gridSize; col++)
        {
            int x = center.X - sightRange + col;
            int y = center.Y - sightRange + row;
            if (x < 0 || y < 0) continue;
            cells.Add(new VisibleCellData(
                new Coordinates(x, y),
                ObjectType.Empty,
                Guid.Empty,
                true));
        }
        return cells;
    }
}
