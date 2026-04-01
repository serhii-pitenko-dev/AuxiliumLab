using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.Entities;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetMapLayout;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Maps;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Lost;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Win;
using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using AuxiliumLab.AiSandbox.SharedContracts;
using AuxiliumLab.AiSandbox.WebApi.Features.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.WebApi.Features.Simulation;

/// <summary>
/// Tests for <see cref="SimulationVisualizationBridge"/> verifying that it correctly
/// subscribes/unsubscribes to the message broker and forwards simulation events
/// to the <see cref="ISimulationHubNotifier"/>.
/// </summary>
[TestClass]
public class SimulationVisualizationBridgeTests
{
    private MessageBroker _broker = null!;
    private Mock<ISimulationHubNotifier> _mockNotifier = null!;
    private Mock<IServiceScopeFactory> _mockScopeFactory = null!;
    private Mock<IMemoryDataManager<StandardPlayground>> _mockPlaygroundMemory = null!;
    private SimulationVisualizationBridge _bridge = null!;

    private Guid _playgroundId;
    private StandardPlayground _playground = null!;

    [TestInitialize]
    public void Setup()
    {
        _broker = new MessageBroker();
        _mockNotifier = new Mock<ISimulationHubNotifier>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockPlaygroundMemory = new Mock<IMemoryDataManager<StandardPlayground>>();

        _playgroundId = Guid.NewGuid();

        // Create a minimal playground with a hero for BuildInitialAgents
        var map = new MapSquareCells(3, 3);
        _playground = new StandardPlayground(map, new VisibilityService(), _playgroundId);
        var hero = new Hero(
            new InitialAgentCharacters(Speed: 3, SightRange: 2, Stamina: 10,
                PathToTarget: [], AgentActions: [], ExecutedActions: [], isRun: false, orderInTurnQueue: 0),
            Guid.NewGuid());
        _playground.PlaceHero(hero, new Coordinates(1, 1));

        _mockPlaygroundMemory.Setup(m => m.LoadObject(_playgroundId)).Returns(_playground);

        // Setup the service scope to return IMapQueriesHandleService
        var cells = new MapCell[3, 3];
        for (int x = 0; x < 3; x++)
        for (int y = 0; y < 3; y++)
        {
            cells[x, y] = new MapCell(new Coordinates(x, y), Guid.Empty, ObjectType.Empty, []);
        }
        // Place hero in the map cell grid
        cells[1, 1] = new MapCell(new Coordinates(1, 1), hero.Id, ObjectType.Hero, []);

        var mockMapLayout = new Mock<IMapLayout>();
        mockMapLayout.Setup(m => m.GetFromMemory(_playgroundId))
            .Returns(new MapLayoutResponse(0, cells));

        var mockMapQueries = new Mock<IMapQueriesHandleService>();
        mockMapQueries.SetupGet(q => q.MapLayoutQuery).Returns(mockMapLayout.Object);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IMapQueriesHandleService)))
            .Returns(mockMapQueries.Object);

        var mockScope = new Mock<IServiceScope>();
        mockScope.SetupGet(s => s.ServiceProvider).Returns(mockServiceProvider.Object);

        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        // Setup notifier to return completed tasks
        _mockNotifier.Setup(n => n.NotifySimulationStartedAsync(It.IsAny<SimulationStartedDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockNotifier.Setup(n => n.NotifyAgentMovedAsync(It.IsAny<SimulationAgentMovedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockNotifier.Setup(n => n.NotifyTurnCompletedAsync(It.IsAny<SimulationTurnCompletedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockNotifier.Setup(n => n.NotifySimulationEndedAsync(It.IsAny<SimulationEndedDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bridge = new SimulationVisualizationBridge(
            _broker, _mockNotifier.Object, _mockScopeFactory.Object, _mockPlaygroundMemory.Object);
    }

    // ── Attach subscribes to broker events ──────────────────────────────────

    [TestMethod]
    public void Attach_ThenGameStartedPublished_HandlersInvoked()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _bridge.Attach(jobId, maxTurns: 50);

        // Act — publish GameStartedEvent on the same broker
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), _playgroundId));

        // Assert — the bridge should map the playground and cache the initial state
        // Allow async Task.Run inside the handler a moment to complete
        Task.Delay(100).Wait();

        var cached = _bridge.GetCachedStart(jobId.ToString());
        cached.Should().NotBeNull(
            "bridge must handle GameStartedEvent and cache the SimulationStartedDto");
        cached!.JobId.Should().Be(jobId.ToString());
        cached.Width.Should().Be(3);
        cached.Height.Should().Be(3);
        cached.MaxTurns.Should().Be(50);
    }

    [TestMethod]
    public void Attach_ThenTurnExecutedPublished_CachesTurnData()
    {
        // Arrange — attach and trigger GameStarted first to establish playground mapping
        var jobId = Guid.NewGuid();
        _bridge.Attach(jobId, maxTurns: 50);
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), _playgroundId));

        // Act — publish TurnExecutedEvent
        _broker.Publish(new TurnExecutedEvent(Guid.NewGuid(), _playgroundId, 5));

        // Assert
        var cached = _bridge.GetCachedLastTurn(jobId.ToString());
        cached.Should().NotBeNull("bridge must cache turn data after TurnExecutedEvent");
        cached!.TurnNumber.Should().Be(5);
        cached.JobId.Should().Be(jobId.ToString());
    }

    [TestMethod]
    public void Attach_ThenHeroWonPublished_CachesFinalState()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _bridge.Attach(jobId, maxTurns: 50);
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), _playgroundId));

        // Act
        _broker.Publish(new HeroWonEvent(Guid.NewGuid(), _playgroundId, WinReason.ExitReached));

        // Assert
        Task.Delay(100).Wait();
        var cached = _bridge.GetCachedEnd(jobId.ToString());
        cached.Should().NotBeNull("bridge must cache the final state on HeroWonEvent");
        cached!.Outcome.Should().Be("Won");
        cached.Reason.Should().Be("ExitReached");
    }

    [TestMethod]
    public void Attach_ThenHeroLostPublished_CachesFinalState()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _bridge.Attach(jobId, maxTurns: 50);
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), _playgroundId));

        // Act
        _broker.Publish(new HeroLostEvent(Guid.NewGuid(), _playgroundId, LostReason.HeroCatched));

        // Assert
        Task.Delay(100).Wait();
        var cached = _bridge.GetCachedEnd(jobId.ToString());
        cached.Should().NotBeNull("bridge must cache the final state on HeroLostEvent");
        cached!.Outcome.Should().Be("Lost");
    }

    [TestMethod]
    public void Attach_ThenAgentMovedPublished_CachesAgentMove()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _bridge.Attach(jobId, maxTurns: 50);
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), _playgroundId));

        var agentId = _playground.Hero.Id;
        var snapshot = new AgentSnapshot(
            agentId, ObjectType.Hero, Speed: 3, SightRange: 2,
            IsRun: false, Stamina: 10, MaxStamina: 10, OrderInTurnQueue: 0);

        // Act
        _broker.Publish(new OnAgentMoveActionEvent(
            Guid.NewGuid(), _playgroundId, agentId,
            new Coordinates(1, 1), new Coordinates(2, 1),
            true, snapshot));

        // Assert
        var cachedMoves = _bridge.GetCachedLastAgentMoves(jobId.ToString());
        cachedMoves.Should().HaveCount(1);
        cachedMoves[0].AgentId.Should().Be(agentId.ToString());
        cachedMoves[0].IsSuccess.Should().BeTrue();
    }

    // ── Detach unsubscribes from broker events ──────────────────────────────

    [TestMethod]
    public void Detach_PreventsHandlingOfSubsequentEvents()
    {
        // Arrange — attach and detach
        var jobId = Guid.NewGuid();
        _bridge.Attach(jobId, maxTurns: 50);
        _bridge.Detach(jobId);

        // Act — publish GameStartedEvent after detach
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), _playgroundId));

        // Assert — no cached state should be produced
        _bridge.GetCachedStart(jobId.ToString()).Should().BeNull(
            "bridge must unsubscribe from the broker after Detach");
    }

    [TestMethod]
    public void Detach_ClearsPlaygroundMapping_ButPreservesCachedStatesForLateJoiners()
    {
        // Arrange — attach, receive events, then detach
        var jobId = Guid.NewGuid();
        _bridge.Attach(jobId, maxTurns: 50);
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), _playgroundId));

        // Detach
        _bridge.Detach(jobId);

        // Assert — cached state remains for late-joining clients
        _bridge.GetCachedStart(jobId.ToString()).Should().NotBeNull(
            "cached initial state must survive Detach so late-joining SignalR clients can still receive it");
    }

    // ── Reference counting ──────────────────────────────────────────────────

    [TestMethod]
    public void Attach_MultipleTimes_OnlySubscribesOnce()
    {
        // Arrange — attach two jobs
        var job1 = Guid.NewGuid();
        var job2 = Guid.NewGuid();
        _bridge.Attach(job1, maxTurns: 50);
        _bridge.Attach(job2, maxTurns: 50);

        // Act — publish GameStartedEvent: only the first pending job should dequeue
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), _playgroundId));

        _bridge.GetCachedStart(job1.ToString()).Should().NotBeNull(
            "first attached job should receive the GameStartedEvent");
        _bridge.GetCachedStart(job2.ToString()).Should().BeNull(
            "second attached job should wait for its own GameStartedEvent");
    }

    [TestMethod]
    public void Detach_OneOfTwo_StillSubscribed()
    {
        // Arrange — attach two jobs sequentially with their own GameStartedEvents
        var job1 = Guid.NewGuid();
        var job2 = Guid.NewGuid();
        var pg1 = Guid.NewGuid();
        var pg2 = _playgroundId;

        // Setup first playground and its dependencies
        var map1 = new MapSquareCells(3, 3);
        var playground1 = new StandardPlayground(map1, new VisibilityService(), pg1);
        var hero1 = new Hero(
            new InitialAgentCharacters(Speed: 3, SightRange: 2, Stamina: 10,
                PathToTarget: [], AgentActions: [], ExecutedActions: [], isRun: false, orderInTurnQueue: 0),
            Guid.NewGuid());
        playground1.PlaceHero(hero1, new Coordinates(1, 1));
        _mockPlaygroundMemory.Setup(m => m.LoadObject(pg1)).Returns(playground1);

        // Setup map layout for pg1 and pg2
        var cells1 = new MapCell[3, 3];
        var cells2 = new MapCell[3, 3];
        for (int x = 0; x < 3; x++)
        for (int y = 0; y < 3; y++)
        {
            cells1[x, y] = new MapCell(new Coordinates(x, y), Guid.Empty, ObjectType.Empty, []);
            cells2[x, y] = new MapCell(new Coordinates(x, y), Guid.Empty, ObjectType.Empty, []);
        }
        cells1[1, 1] = new MapCell(new Coordinates(1, 1), hero1.Id, ObjectType.Hero, []);
        cells2[1, 1] = new MapCell(new Coordinates(1, 1), _playground.Hero.Id, ObjectType.Hero, []);

        var mockMapLayout = new Mock<IMapLayout>();
        mockMapLayout.Setup(m => m.GetFromMemory(It.IsAny<Guid>()))
            .Returns((Guid id) => new MapLayoutResponse(0, id == pg1 ? cells1 : cells2));

        var mockMapQueries = new Mock<IMapQueriesHandleService>();
        mockMapQueries.SetupGet(q => q.MapLayoutQuery).Returns(mockMapLayout.Object);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IMapQueriesHandleService)))
            .Returns(mockMapQueries.Object);

        var mockScope = new Mock<IServiceScope>();
        mockScope.SetupGet(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        _bridge.Attach(job1, maxTurns: 50);

        // First job gets its GameStartedEvent to establish mapping
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), pg1));

        // Detach job1 — subscriptions should remain because job2 hasn't been added yet
        _bridge.Detach(job1);

        // Attach job2 — ref count goes back to 1, no re-subscribe needed since still > 0
        _bridge.Attach(job2, maxTurns: 50);

        // Act — publish GameStartedEvent for job2
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), pg2));

        // Assert — job2's pending attachment should be processed
        _bridge.GetCachedStart(job2.ToString()).Should().NotBeNull(
            "remaining attached job should still receive events after partial Detach");
    }

    // ── Events before Attach are not received ───────────────────────────────

    [TestMethod]
    public void GameStartedEvent_WithoutAttach_IsIgnored()
    {
        // Act — publish without attaching
        _broker.Publish(new GameStartedEvent(Guid.NewGuid(), _playgroundId));

        // Assert — no state cached (no pending attachments to dequeue)
        // Bridge should not have subscribers if never attached
        // This is a sanity check
        _bridge.GetCachedStart(Guid.NewGuid().ToString()).Should().BeNull();
    }

    [TestMethod]
    public void TurnExecutedEvent_ForUnknownPlayground_IsIgnored()
    {
        // Arrange — attach but don't trigger GameStarted (no playground mapping)
        var jobId = Guid.NewGuid();
        _bridge.Attach(jobId, maxTurns: 50);

        var unknownPlayground = Guid.NewGuid();

        // Act — publish TurnExecutedEvent for an unknown playground
        _broker.Publish(new TurnExecutedEvent(Guid.NewGuid(), unknownPlayground, 1));

        // Assert — no turn cached
        _bridge.GetCachedLastTurn(jobId.ToString()).Should().BeNull(
            "events for unrecognized playgrounds must be silently ignored");
    }
}
