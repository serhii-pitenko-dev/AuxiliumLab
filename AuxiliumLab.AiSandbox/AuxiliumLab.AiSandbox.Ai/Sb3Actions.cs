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

namespace AuxiliumLab.AiSandbox.Ai;

/// <summary>
/// Bridges Python Stable-Baselines3 gym environments with the .NET simulation.
/// One instance per parallel gym (each has a unique GymId).
/// </summary>
public class Sb3Actions : IAiActions
{
    // ── Dependencies ──────────────────────────────────────────────────────────
    private readonly IMessageBroker _messageBroker;
    private readonly IMemoryDataManager<AgentStateForAIDecision> _agentStateMemoryRepository;

    // ── Reward settings ───────────────────────────────────────────────────────
    private readonly float _stepPenalty;
    private readonly float _winReward;
    private readonly float _lossReward;

    // ── State ─────────────────────────────────────────────────────────────────
    private Guid _playgroundId = Guid.Empty;
    private Func<Task>? _episodeCallback;
    private volatile bool _isWaitingForResetObservation;
    private float[] _lastObservation = [];

    // TCS for the current Reset call (SimulationService awaits SimulationResetResponse)
    private TaskCompletionSource<SimulationResetResponse>? _resetResponseTcs;
    private Guid _resetCorrelationId;

    // TCS for the current Step call (SimulationService awaits SimulationStepResponse)
    private TaskCompletionSource<SimulationStepResponse>? _stepResponseTcs;
    private Guid _stepCorrelationId;

    // TCS bridging Python Step action to executor DecisionMakeCommand
    private TaskCompletionSource<int>? _actionTcs;

    // ── IAiActions ────────────────────────────────────────────────────────────
    public AiConfiguration AiConfiguration { get; init; }

    // ── Extra public API ──────────────────────────────────────────────────────
    public Guid GymId { get; }
    public ModelType ModelType { get; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public Sb3Actions(
        IMessageBroker messageBroker,
        IMemoryDataManager<AgentStateForAIDecision> agentStateMemoryRepository,
        ModelType modelType,
        AiConfiguration aiConfiguration,
        Guid gymId,
        float stepPenalty,
        float winReward,
        float lossReward)
    {
        _messageBroker = messageBroker;
        _agentStateMemoryRepository = agentStateMemoryRepository;
        ModelType = modelType;
        GymId = gymId;
        AiConfiguration = aiConfiguration;
        _stepPenalty = stepPenalty;
        _winReward = winReward;
        _lossReward = lossReward;

        _messageBroker.Subscribe<GameStartedEvent>(OnGameStarted);
        _messageBroker.Subscribe<RequestAgentDecisionMakeCommand>(OnDecisionRequest);
        _messageBroker.Subscribe<HeroWonEvent>(OnHeroWon);
        _messageBroker.Subscribe<HeroLostEvent>(OnHeroLost);
        _messageBroker.Subscribe<RequestSimulationResetCommand>(OnSimulationReset);
        _messageBroker.Subscribe<RequestSimulationStepCommand>(OnSimulationStep);
        _messageBroker.Subscribe<RequestSimulationCloseCommand>(OnSimulationClose);
    }

    /// <summary>Called by RunTraining to restart simulation episodes.</summary>
    public void SetEpisodeCallback(Func<Task> callback) => _episodeCallback = callback;

    // ── Initialize ────────────────────────────────────────────────────────────
    // IAiActions.Initialize() is an executor lifecycle hook called at the start
    // of every episode via Executor.StartSimulationPreparationsAsync().
    //
    // For RandomActions it is meaningful: RandomActions is recreated per episode
    // by ExecutorFactory, so it uses Initialize() to set up its broker subscriptions
    // for that specific episode's playground.
    //
    // For Sb3Actions it is a no-op: Sb3Actions is long-lived (one instance per gym
    // for the entire training session) and subscribes to the broker once in its
    // constructor. The executor still calls Initialize() on every episode, but
    // Sb3Actions simply ignores it.
    public void Initialize() { }

    // ── AiContract handlers ───────────────────────────────────────────────────

    private void OnGameStarted(GameStartedEvent evt)
    {
        // With a per-gym isolated broker, every GameStartedEvent on this broker
        // was fired by this gym's own executor — no filtering needed.
        _playgroundId = evt.PlaygroundId;
        _messageBroker.Publish(new AiReadyToActionsResponse(Guid.NewGuid(), evt.PlaygroundId, evt.Id));
    }

    private void OnDecisionRequest(RequestAgentDecisionMakeCommand cmd)
    {
        if (cmd.PlaygroundId != _playgroundId) return;

        Console.WriteLine($"[Sb3Actions:{GymId:N}] OnDecisionRequest playground={cmd.PlaygroundId:N} waitingForReset={_isWaitingForResetObservation}");

        var agent = _agentStateMemoryRepository.LoadObject(cmd.AgentId);
        if (agent is null) return;

        var obs = BuildObservation(agent);
        _lastObservation = obs;

        if (_isWaitingForResetObservation)
        {
            // First decision after Reset -> deliver initial observation to Python
            _isWaitingForResetObservation = false;
            var resetTcs = _resetResponseTcs;
            _resetResponseTcs = null;
            resetTcs?.TrySetResult(new SimulationResetResponse(
                Guid.NewGuid(), GymId, _resetCorrelationId, obs, new Dictionary<string, string>()));
        }
        else
        {
            // Subsequent decision -> deliver step result (step penalty reward)
            var stepTcs = _stepResponseTcs;
            _stepResponseTcs = null;
            stepTcs?.TrySetResult(new SimulationStepResponse(
                Guid.NewGuid(), GymId, _stepCorrelationId, obs,
                _stepPenalty, false, false,
                new Dictionary<string, string>()));
        }

        // Asynchronously wait for Python's next action
        var actionTcs = new TaskCompletionSource<int>();
        _actionTcs = actionTcs;

        var agentId = cmd.AgentId;
        var corrId = cmd.Id;
        var agentPos = agent.Coordinates;

        _ = Task.Run(async () =>
        {
            var action = await actionTcs.Task.ConfigureAwait(false);
            var response = BuildDecisionResponse(corrId, agentId, agentPos, action);
            _messageBroker.Publish(response);
        });
    }

    private void OnHeroWon(HeroWonEvent evt)
    {
        if (evt.PlaygroundId != _playgroundId) return;
        CompleteStep(_winReward, terminated: true);
        _playgroundId = Guid.Empty; // cleared; next Reset will re-capture via _isExpectingNewPlayground
    }

    private void OnHeroLost(HeroLostEvent evt)
    {
        if (evt.PlaygroundId != _playgroundId) return;
        CompleteStep(_lossReward, terminated: true);
        _playgroundId = Guid.Empty;
    }

    private void CompleteStep(float reward, bool terminated)
    {
        var stepTcs = _stepResponseTcs;
        _stepResponseTcs = null;
        stepTcs?.TrySetResult(new SimulationStepResponse(
            Guid.NewGuid(), GymId, _stepCorrelationId, _lastObservation,
            reward, terminated, false, new Dictionary<string, string>()));
    }

    // ── Sb3Contract handlers ──────────────────────────────────────────────────

    private void OnSimulationReset(RequestSimulationResetCommand cmd)
    {
        if (cmd.GymId != GymId) return;

        Console.WriteLine($"[Sb3Actions:{GymId:N}] OnSimulationReset firing, starting episode...");

        _resetCorrelationId = cmd.Id;
        _isWaitingForResetObservation = true;

        var tcs = new TaskCompletionSource<SimulationResetResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _resetResponseTcs = tcs;

        // Start a new episode in the background; propagate failures to reset TCS so Python gets an error
        var capturedTcs = tcs;
        _ = Task.Run(async () =>
        {
            try
            {
                if (_episodeCallback != null)
                    await _episodeCallback().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sb3Actions:{GymId:N}] Episode callback FAILED: {ex.Message}");
                capturedTcs.TrySetException(ex);
            }
        });

        // Await initial obs then publish response to SimulationService
        _ = Task.Run(async () =>
        {
            var response = await tcs.Task.ConfigureAwait(false);
            Console.WriteLine($"[Sb3Actions:{GymId:N}] Reset TCS resolved, publishing SimulationResetResponse");
            _messageBroker.Publish(response);
        });
    }

    private void OnSimulationStep(RequestSimulationStepCommand cmd)
    {
        if (cmd.GymId != GymId) return;

        _stepCorrelationId = cmd.Id;

        var tcs = new TaskCompletionSource<SimulationStepResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _stepResponseTcs = tcs;

        // Provide action to executor (wakes pending _actionTcs)
        _actionTcs?.TrySetResult(cmd.Action);

        // Await step result then publish response to SimulationService
        _ = Task.Run(async () =>
        {
            var response = await tcs.Task.ConfigureAwait(false);
            _messageBroker.Publish(response);
        });
    }

    private void OnSimulationClose(RequestSimulationCloseCommand cmd)
    {
        if (cmd.GymId != GymId) return;

        _actionTcs?.TrySetCanceled();
        _resetResponseTcs?.TrySetCanceled();
        _stepResponseTcs?.TrySetCanceled();

        _messageBroker.Publish(new SimulationCloseResponse(Guid.NewGuid(), GymId, cmd.Id, true));
    }

    // ── Observation builder (delegated to shared ObservationBuilder) ──────────

    private static float[] BuildObservation(AgentStateForAIDecision agent)
        => ObservationBuilder.Build(agent);

    // ── Action decoder (delegated to shared ObservationBuilder) ──────────────

    private static AgentDecisionBaseResponse BuildDecisionResponse(
        Guid correlationId, Guid agentId, Coordinates from, int action)
        => ObservationBuilder.BuildDecisionResponse(correlationId, agentId, from, action);
}
