using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Commands;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Responses;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Lost;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Win;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.Sb3Contract.Commands;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.Sb3Contract.Responses;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using AuxiliumLab.AiSandbox.SharedContracts;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.Ai;

[TestClass]
public class Sb3ActionsTests
{
    private MessageBroker _broker = null!;
    private MemoryDataManager<AgentStateForAIDecision> _agentStore = null!;
    private Sb3Actions _sut = null!;
    private Guid _gymId;

    private const float StepPenalty = -0.1f;
    private const float WinReward = 100f;
    private const float LossReward = -100f;

    [TestInitialize]
    public void Setup()
    {
        _broker = new MessageBroker();
        _agentStore = new MemoryDataManager<AgentStateForAIDecision>();
        _gymId = Guid.NewGuid();

        _sut = new Sb3Actions(
            _broker,
            _agentStore,
            ModelType.PPO,
            new AiConfiguration { ModelType = ModelType.PPO, Version = "1.0", PolicyType = AiPolicy.MLP },
            _gymId,
            StepPenalty,
            WinReward,
            LossReward,
            ObjectType.Hero);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AgentStateForAIDecision CreateHeroState(Guid playgroundId, Guid agentId)
    {
        return new AgentStateForAIDecision(
            playgroundId, agentId, ObjectType.Hero,
            new Coordinates(5, 5), Speed: 2, SightRange: 5,
            IsRun: false, Stamina: 15, MaxStamina: 15,
            VisibleCells: [], AvailableLimitedActions: [AgentAction.Move],
            ExecutedActions: []);
    }

    /// <summary>
    /// Simulates the full Reset flow: publishes RequestSimulationResetCommand,
    /// waits for the episode callback, triggers GameStartedEvent + DecisionRequest,
    /// and returns the SimulationResetResponse.
    /// </summary>
    private async Task<SimulationResetResponse> PerformResetAsync(
        Guid playgroundId, Guid heroAgentId, Func<Task>? episodeCallback = null)
    {
        var resetTcs = new TaskCompletionSource<SimulationResetResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        _broker.Subscribe<SimulationResetResponse>(msg =>
        {
            if (msg.GymId == _gymId)
                resetTcs.TrySetResult(msg);
        });

        // The episode callback simulates what the executor does:
        // publishes GameStartedEvent, stores agent state, publishes RequestAgentDecisionMakeCommand.
        _sut.SetEpisodeCallback(episodeCallback ?? (async () =>
        {
            // Simulate executor: GameStartedEvent → wait for AiReadyToActionsResponse → DecisionRequest
            var rpcClient = new BrokerRpcClient(_broker);
            var gameStartedEventId = Guid.NewGuid();
            await rpcClient.RequestAsync<GameStartedEvent, AiReadyToActionsResponse>(
                new GameStartedEvent(gameStartedEventId, playgroundId));

            _agentStore.AddOrUpdate(heroAgentId, CreateHeroState(playgroundId, heroAgentId));
            _broker.Publish(new RequestAgentDecisionMakeCommand(Guid.NewGuid(), playgroundId, heroAgentId));

            // Wait for the action from Python (simulates executor blocking)
            // In real flow, executor waits for AgentDecisionBaseResponse via BrokerRpcClient.
            // For test, just wait briefly.
            await Task.Delay(50);
        }));

        _broker.Publish(new RequestSimulationResetCommand(Guid.NewGuid(), _gymId, 42));

        var response = await resetTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        return response;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task OnSimulationReset_DeliversInitialObservation_WhenEpisodeCallbackSucceeds()
    {
        var playgroundId = Guid.NewGuid();
        var heroAgentId = Guid.NewGuid();

        var response = await PerformResetAsync(playgroundId, heroAgentId);

        response.Should().NotBeNull();
        response.GymId.Should().Be(_gymId);
        response.Observation.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task OnSimulationReset_PropagatesError_WhenEpisodeCallbackFails()
    {
        // Episode callback that fails immediately (before delivering initial obs).
        _sut.SetEpisodeCallback(() => throw new InvalidOperationException("Executor crashed on init"));

        var resetResponseTcs = new TaskCompletionSource<SimulationResetResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        _broker.Subscribe<SimulationResetResponse>(msg =>
        {
            if (msg.GymId == _gymId)
                resetResponseTcs.TrySetResult(msg);
        });

        _broker.Publish(new RequestSimulationResetCommand(Guid.NewGuid(), _gymId, 42));

        // The reset TCS should fault because the episode callback threw before
        // delivering the initial observation. Wait a bit for the background task.
        await Task.Delay(200);

        // Since the TCS faulted, no SimulationResetResponse should be published.
        resetResponseTcs.Task.IsCompleted.Should().BeFalse(
            "No SimulationResetResponse should be published when the episode callback fails before reset obs");
    }

    [TestMethod]
    public async Task OnSimulationStep_PropagatesError_WhenEpisodeFaultedMidStep()
    {
        var playgroundId = Guid.NewGuid();
        var heroAgentId = Guid.NewGuid();

        // Set up an episode callback that succeeds initially (delivers reset obs)
        // but then crashes mid-episode.
        var episodeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _sut.SetEpisodeCallback(async () =>
        {
            // Simulate executor: game started → first decision
            var rpcClient = new BrokerRpcClient(_broker);
            await rpcClient.RequestAsync<GameStartedEvent, AiReadyToActionsResponse>(
                new GameStartedEvent(Guid.NewGuid(), playgroundId));

            _agentStore.AddOrUpdate(heroAgentId, CreateHeroState(playgroundId, heroAgentId));
            _broker.Publish(new RequestAgentDecisionMakeCommand(Guid.NewGuid(), playgroundId, heroAgentId));

            // Signal that episode has started
            episodeStarted.TrySetResult();

            // Wait briefly then crash (simulates mid-episode executor failure)
            await Task.Delay(100);
            throw new InvalidOperationException("Executor crashed mid-episode");
        });

        // Start reset
        var resetTcs = new TaskCompletionSource<SimulationResetResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _broker.Subscribe<SimulationResetResponse>(msg =>
        {
            if (msg.GymId == _gymId) resetTcs.TrySetResult(msg);
        });
        _broker.Publish(new RequestSimulationResetCommand(Guid.NewGuid(), _gymId, 42));

        // Wait for reset to complete (initial obs delivered)
        var resetResponse = await resetTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        resetResponse.Should().NotBeNull();

        // Wait for episode to actually start and then crash
        await episodeStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Delay(300); // Wait for the crash to propagate

        // Now send a Step command — it should detect the faulted episode
        // and NOT hang. The step TCS should be faulted.
        var stepTcs = new TaskCompletionSource<SimulationStepResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        _broker.Subscribe<SimulationStepResponse>(msg =>
        {
            if (msg.GymId == _gymId) stepTcs.TrySetResult(msg);
        });

        _broker.Publish(new RequestSimulationStepCommand(Guid.NewGuid(), _gymId, 0));

        // The step should fail fast (not hang for 120s).
        // Give it a reasonable timeout — if it exceeds 2s, it's hanging.
        var completed = await Task.WhenAny(stepTcs.Task, Task.Delay(2000));
        completed.Should().NotBe(stepTcs.Task,
            "Step should NOT produce a normal response when the episode has faulted");
    }

    [TestMethod]
    public async Task OnHeroWon_CompletesStepWithWinReward()
    {
        var playgroundId = Guid.NewGuid();
        var heroAgentId = Guid.NewGuid();

        // Set up a full episode flow where hero wins after one step.
        var actionReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        _sut.SetEpisodeCallback(async () =>
        {
            var rpcClient = new BrokerRpcClient(_broker);
            await rpcClient.RequestAsync<GameStartedEvent, AiReadyToActionsResponse>(
                new GameStartedEvent(Guid.NewGuid(), playgroundId));

            _agentStore.AddOrUpdate(heroAgentId, CreateHeroState(playgroundId, heroAgentId));

            // First decision → reset obs delivered. Then executor waits for action.
            var decisionCmd = new RequestAgentDecisionMakeCommand(Guid.NewGuid(), playgroundId, heroAgentId);

            // Subscribe for the action response to know when Python provides an action
            var actionResponseTcs = new TaskCompletionSource<AgentDecisionBaseResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _broker.Subscribe<AgentDecisionBaseResponse>(msg =>
            {
                if (msg.CorrelationId == decisionCmd.Id)
                    actionResponseTcs.TrySetResult(msg);
            });

            _broker.Publish(decisionCmd);

            // Wait for the action from Python
            await actionResponseTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            actionReceived.TrySetResult(0);

            // Simulate hero reaching exit → publish HeroWonEvent
            _broker.Publish(new HeroWonEvent(Guid.NewGuid(), playgroundId, WinReason.ExitReached));
        });

        // 1. Reset
        var resetTcs = new TaskCompletionSource<SimulationResetResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _broker.Subscribe<SimulationResetResponse>(msg =>
        {
            if (msg.GymId == _gymId) resetTcs.TrySetResult(msg);
        });
        _broker.Publish(new RequestSimulationResetCommand(Guid.NewGuid(), _gymId, 42));
        await resetTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // 2. Step (provide action=1)
        var stepTcs = new TaskCompletionSource<SimulationStepResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _broker.Subscribe<SimulationStepResponse>(msg =>
        {
            if (msg.GymId == _gymId) stepTcs.TrySetResult(msg);
        });
        _broker.Publish(new RequestSimulationStepCommand(Guid.NewGuid(), _gymId, 1));

        var stepResponse = await stepTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        stepResponse.Reward.Should().Be(WinReward);
        stepResponse.Terminated.Should().BeTrue();
    }

    [TestMethod]
    public async Task OnHeroLost_CompletesStepWithLossReward()
    {
        var playgroundId = Guid.NewGuid();
        var heroAgentId = Guid.NewGuid();

        _sut.SetEpisodeCallback(async () =>
        {
            var rpcClient = new BrokerRpcClient(_broker);
            await rpcClient.RequestAsync<GameStartedEvent, AiReadyToActionsResponse>(
                new GameStartedEvent(Guid.NewGuid(), playgroundId));

            _agentStore.AddOrUpdate(heroAgentId, CreateHeroState(playgroundId, heroAgentId));

            var decisionCmd = new RequestAgentDecisionMakeCommand(Guid.NewGuid(), playgroundId, heroAgentId);

            var actionResponseTcs = new TaskCompletionSource<AgentDecisionBaseResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _broker.Subscribe<AgentDecisionBaseResponse>(msg =>
            {
                if (msg.CorrelationId == decisionCmd.Id)
                    actionResponseTcs.TrySetResult(msg);
            });

            _broker.Publish(decisionCmd);
            await actionResponseTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Hero gets caught
            _broker.Publish(new HeroLostEvent(Guid.NewGuid(), playgroundId, LostReason.HeroCatched));
        });

        // Reset
        var resetTcs = new TaskCompletionSource<SimulationResetResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _broker.Subscribe<SimulationResetResponse>(msg =>
        {
            if (msg.GymId == _gymId) resetTcs.TrySetResult(msg);
        });
        _broker.Publish(new RequestSimulationResetCommand(Guid.NewGuid(), _gymId, 42));
        await resetTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Step
        var stepTcs = new TaskCompletionSource<SimulationStepResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _broker.Subscribe<SimulationStepResponse>(msg =>
        {
            if (msg.GymId == _gymId) stepTcs.TrySetResult(msg);
        });
        _broker.Publish(new RequestSimulationStepCommand(Guid.NewGuid(), _gymId, 0));

        var stepResponse = await stepTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        stepResponse.Reward.Should().Be(LossReward);
        stepResponse.Terminated.Should().BeTrue();
    }

    [TestMethod]
    public void OnSimulationClose_CancelsPendingTasks()
    {
        var closeResponseTcs = new TaskCompletionSource<SimulationCloseResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _broker.Subscribe<SimulationCloseResponse>(msg =>
        {
            if (msg.GymId == _gymId)
                closeResponseTcs.TrySetResult(msg);
        });

        _broker.Publish(new RequestSimulationCloseCommand(Guid.NewGuid(), _gymId));

        var response = closeResponseTcs.Task.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
        response.Success.Should().BeTrue();
    }

    [TestMethod]
    public void OnSimulationStep_IgnoresOtherGymId()
    {
        var otherGymId = Guid.NewGuid();
        var stepResponseReceived = false;

        _broker.Subscribe<SimulationStepResponse>(msg =>
        {
            if (msg.GymId == otherGymId) stepResponseReceived = true;
        });

        _broker.Publish(new RequestSimulationStepCommand(Guid.NewGuid(), otherGymId, 0));

        // Give time for any async processing
        Thread.Sleep(100);
        stepResponseReceived.Should().BeFalse();
    }
}
