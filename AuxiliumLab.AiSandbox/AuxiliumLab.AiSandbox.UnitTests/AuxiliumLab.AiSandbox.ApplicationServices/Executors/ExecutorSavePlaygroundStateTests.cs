using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground;
using AuxiliumLab.AiSandbox.ApplicationServices.Executors;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto.Performance;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.TestPreconditionSet;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.Mappers;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Responses;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.ApplicationServices.Executors;

/// <summary>
/// Verifies that playground state is persisted to the file repository only when
/// an executor requires it for presentation (<see cref="ExecutorForPresentation"/>),
/// and is intentionally skipped for non-interactive executors (<see cref="StandardExecutor"/>).
///
/// Root cause of the original bug: <c>SaveAsync</c> was commented out, causing
/// <c>ConsoleRunner.OnGameStarted</c> to throw <see cref="FileNotFoundException"/>
/// when it tried to load the state that was never written.
/// </summary>
[TestClass]
public class ExecutorSavePlaygroundStateTests
{
    // â”€â”€ shared mocks / state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private Mock<IPlaygroundCommandsHandleService> _mockPlaygroundCommands = null!;
    private Mock<IMemoryDataManager<StandardPlayground>> _mockPlaygroundRepository = null!;
    private Mock<IAiActions> _mockAiActions = null!;
    private Mock<IFileDataManager<StandardPlaygroundState>> _mockPlaygroundStateFileRepo = null!;
    private Mock<IMemoryDataManager<AgentStateForAIDecision>> _mockAgentStateMemoryRepo = null!;
    private Mock<IBrokerRpcClient> _mockBrokerRpcClient = null!;
    private Mock<IStandardPlaygroundMapper> _mockMapper = null!;
    private Mock<IFileDataManager<RawDataLog>> _mockRawDataLogRepo = null!;
    private Mock<IFileDataManager<TurnExecutionPerformance>> _mockTurnPerfRepo = null!;
    private Mock<IFileDataManager<SandboxExecutionPerformance>> _mockSandboxPerfRepo = null!;
    private Mock<ITestPreconditionData> _mockTestPreconditionData = null!;
    private IOptions<SandBoxConfiguration> _configuration = null!;
    private IMessageBroker _messageBroker = null!;

    private StandardPlayground _playground = null!;
    private StandardPlaygroundState _playgroundState = null!;
    private Guid _playgroundId;

    [TestInitialize]
    public void Setup()
    {
        _playgroundId = Guid.NewGuid();

        // Real playground with a hero so LookAroundEveryone() does not NRE
        var map = new MapSquareCells(7, 7);
        _playground = new StandardPlayground(map, new VisibilityService(), _playgroundId);
        var hero = new Hero(
            new InitialAgentCharacters(Speed: 3, SightRange: 2, Stamina: 10,
                PathToTarget: [], AgentActions: [], ExecutedActions: [], isRun: false, orderInTurnQueue: 0),
            Guid.NewGuid());
        _playground.PlaceHero(hero, new Coordinates(3, 3));

        _playgroundState = new StandardPlaygroundState
        {
            Id = _playgroundId,
            Map = new MapSquareCellsState { Width = 7, Height = 7 }
        };

        _messageBroker = new MessageBroker();

        _mockPlaygroundCommands = new Mock<IPlaygroundCommandsHandleService>();

        _mockPlaygroundRepository = new Mock<IMemoryDataManager<StandardPlayground>>();
        _mockPlaygroundRepository.Setup(r => r.LoadObject(_playgroundId)).Returns(_playground);

        _mockMapper = new Mock<IStandardPlaygroundMapper>();
        _mockMapper.Setup(m => m.ToState(It.IsAny<StandardPlayground>())).Returns(_playgroundState);

        _mockPlaygroundStateFileRepo = new Mock<IFileDataManager<StandardPlaygroundState>>();
        _mockPlaygroundStateFileRepo
            .Setup(r => r.SaveOrAppendAsync(It.IsAny<Guid>(), It.IsAny<StandardPlaygroundState>()))
            .Returns(Task.CompletedTask);

        _mockAiActions = new Mock<IAiActions>();
        _mockAgentStateMemoryRepo = new Mock<IMemoryDataManager<AgentStateForAIDecision>>();

        _mockBrokerRpcClient = new Mock<IBrokerRpcClient>();
        _mockBrokerRpcClient
            .Setup(c => c.RequestAsync<GameStartedEvent, AiReadyToActionsResponse>(
                It.IsAny<GameStartedEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameStartedEvent evt, CancellationToken _) =>
                new AiReadyToActionsResponse(Guid.NewGuid(), _playgroundId, evt.Id));

        _mockRawDataLogRepo = new Mock<IFileDataManager<RawDataLog>>();
        _mockTurnPerfRepo = new Mock<IFileDataManager<TurnExecutionPerformance>>();
        _mockSandboxPerfRepo = new Mock<IFileDataManager<SandboxExecutionPerformance>>();
        _mockTestPreconditionData = new Mock<ITestPreconditionData>();

        // MaxTurns.Current = 0 â†’ simulation loop exits on first check (Turn 0 >= 0)
        _configuration = Options.Create(new SandBoxConfiguration
        {
            MaxTurns = new IncrementalRange { Current = 0 }
        });
    }

    // â”€â”€ helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private ExecutorForPresentation BuildPresentationExecutor() =>
        new ExecutorForPresentation(
            _mockPlaygroundCommands.Object, _mockPlaygroundRepository.Object,
            _mockAiActions.Object, _configuration,
            _mockPlaygroundStateFileRepo.Object, _mockAgentStateMemoryRepo.Object,
            _messageBroker, _mockBrokerRpcClient.Object, _mockMapper.Object,
            _mockRawDataLogRepo.Object, _mockTurnPerfRepo.Object,
            _mockSandboxPerfRepo.Object, _mockTestPreconditionData.Object);

    private StandardExecutor BuildStandardExecutor() =>
        new StandardExecutor(
            _mockPlaygroundCommands.Object, _mockPlaygroundRepository.Object,
            _mockAiActions.Object, _configuration,
            _mockPlaygroundStateFileRepo.Object, _mockAgentStateMemoryRepo.Object,
            _messageBroker, _mockBrokerRpcClient.Object, _mockMapper.Object,
            _mockRawDataLogRepo.Object, _mockTurnPerfRepo.Object,
            _mockSandboxPerfRepo.Object, _mockTestPreconditionData.Object);

    // â”€â”€ ExecutorForPresentation: NeedsStatePersistence = true â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [TestMethod]
    public async Task ExecutorForPresentation_RunAsync_SavesPlaygroundStateToFileRepository()
    {
        var executor = BuildPresentationExecutor();

        await executor.RunAsync(_playgroundId);

        _mockPlaygroundStateFileRepo.Verify(
            r => r.SaveOrAppendAsync(_playgroundId, It.IsAny<StandardPlaygroundState>()),
            Times.AtLeastOnce,
            "ExecutorForPresentation must save state so ConsoleRunner.OnGameStarted can load it");
    }

    [TestMethod]
    public async Task ExecutorForPresentation_RunAsync_SavesCorrectMappedState()
    {
        var executor = BuildPresentationExecutor();

        await executor.RunAsync(_playgroundId);

        _mockMapper.Verify(m => m.ToState(_playground), Times.AtLeastOnce,
            "Mapper must be called before saving");
        _mockPlaygroundStateFileRepo.Verify(
            r => r.SaveOrAppendAsync(_playgroundId, _playgroundState),
            Times.AtLeastOnce,
            "The mapped state object must be written to the repository");
    }

    [TestMethod]
    public async Task ExecutorForPresentation_RunAsync_SavesStateBeforePublishingGameStartedEvent()
    {
        var callOrder = new List<string>();

        _mockPlaygroundStateFileRepo
            .Setup(r => r.SaveOrAppendAsync(It.IsAny<Guid>(), It.IsAny<StandardPlaygroundState>()))
            .Callback(() => callOrder.Add("save"))
            .Returns(Task.CompletedTask);

        _mockBrokerRpcClient
            .Setup(c => c.RequestAsync<GameStartedEvent, AiReadyToActionsResponse>(
                It.IsAny<GameStartedEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("gameStartedEvent"))
            .ReturnsAsync((GameStartedEvent evt, CancellationToken _) =>
                new AiReadyToActionsResponse(Guid.NewGuid(), _playgroundId, evt.Id));

        var executor = BuildPresentationExecutor();
        await executor.RunAsync(_playgroundId);

        int saveIndex = callOrder.IndexOf("save");
        int eventIndex = callOrder.IndexOf("gameStartedEvent");

        saveIndex.Should().BeGreaterThanOrEqualTo(0, "SaveOrAppendAsync must be called");
        eventIndex.Should().BeGreaterThanOrEqualTo(0, "GameStartedEvent RequestAsync must be called");
        saveIndex.Should().BeLessThan(eventIndex,
            $"State must be saved (index {saveIndex}) before GameStartedEvent (index {eventIndex}); " +
            "otherwise ConsoleRunner.OnGameStarted throws FileNotFoundException");
    }

    // â”€â”€ StandardExecutor: NeedsStatePersistence = false â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [TestMethod]
    public async Task StandardExecutor_RunAsync_DoesNotSavePlaygroundStateToFileRepository()
    {
        // StandardExecutor is used for mass / training runs; no presentation layer
        // needs to load the state file, so the write must be skipped entirely.
        var executor = BuildStandardExecutor();

        await executor.RunAsync(_playgroundId);

        _mockPlaygroundStateFileRepo.Verify(
            r => r.SaveOrAppendAsync(It.IsAny<Guid>(), It.IsAny<StandardPlaygroundState>()),
            Times.Never,
            "StandardExecutor must not write the state file â€” it is only needed by presentation executors");
    }

    [TestMethod]
    public async Task StandardExecutor_RunAsync_DoesNotCallMapper()
    {
        // Mapping is expensive at scale; it must not be triggered in non-interactive runs.
        var executor = BuildStandardExecutor();

        await executor.RunAsync(_playgroundId);

        _mockMapper.Verify(m => m.ToState(It.IsAny<StandardPlayground>()), Times.Never,
            "Mapper must not be called when state persistence is not needed");
    }

    [TestMethod]
    public async Task StandardExecutor_RunAsync_StillPublishesGameStartedEvent()
    {
        // Skipping the file save must not break the simulation flow â€”
        // GameStartedEvent must still reach the AI module.
        var executor = BuildStandardExecutor();

        await executor.RunAsync(_playgroundId);

        _mockBrokerRpcClient.Verify(
            c => c.RequestAsync<GameStartedEvent, AiReadyToActionsResponse>(
                It.IsAny<GameStartedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "GameStartedEvent must still be published even when state persistence is skipped");
    }
}

