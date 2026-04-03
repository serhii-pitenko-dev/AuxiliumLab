using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.PolicyTrainer;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Commands;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Responses;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AuxiliumLab.AiSandbox.AiTrainingOrchestrator;

/// <summary>
/// <see cref="IAiActions"/> implementation that drives a pre-trained SB3 model via the
/// Python gRPC <c>Act</c> RPC for deterministic inference.
/// <para>
/// Used during <c>SingleTrainedAISimulation</c> and <c>MassTrainedAISimulation</c> modes.
/// </para>
/// <para>
/// The trained model is identified by its absolute file path, which is passed as the
/// <c>run_id</c> field in every <see cref="ActRequest"/>. The Python service auto-loads
/// and caches the model on the first call.
/// </para>
/// </summary>
public sealed class InferenceActions : IAiActions, IDisposable
{
    public ModelType ModelType { get; }
    public ObjectType TargetAgentType { get; }

    private readonly IMessageBroker                            _messageBroker;
    private readonly IMemoryDataManager<AgentStateForAIDecision> _agentStateRepository;
    private readonly IPolicyTrainerClient                      _policyTrainerClient;
    private readonly string                                    _modelPath;
    private readonly ILogger<InferenceActions>?                _logger;
    private readonly Action<GameStartedEvent>                  _onGameStartedHandler;
    private readonly Action<RequestAgentDecisionMakeCommand>   _onDecisionRequestHandler;
    private Guid _playgroundId = Guid.Empty;
    private bool _actFailureLogged;

    public AiConfiguration AiConfiguration { get; init; }

    public InferenceActions(
        IMessageBroker messageBroker,
        IMemoryDataManager<AgentStateForAIDecision> agentStateRepository,
        IPolicyTrainerClient policyTrainerClient,
        string modelPath,
        AiConfiguration aiConfiguration,
        ObjectType targetAgentType = ObjectType.Hero,
        ILogger<InferenceActions>? logger = null)
    {
        _messageBroker        = messageBroker;
        _agentStateRepository = agentStateRepository;
        _policyTrainerClient  = policyTrainerClient;
        _modelPath            = modelPath;
        _logger               = logger;
        AiConfiguration       = aiConfiguration;
        ModelType            = aiConfiguration.ModelType;
        TargetAgentType      = targetAgentType;

        _onGameStartedHandler     = OnGameStarted;
        _onDecisionRequestHandler = OnDecisionRequest;
    }

    /// <summary>
    /// Subscribes to game events. Called once per executor episode before the first turn.
    /// </summary>
    public void Initialize()
    {
        _messageBroker.Subscribe(_onGameStartedHandler);
        _messageBroker.Subscribe(_onDecisionRequestHandler);
    }

    /// <summary>
    /// Unsubscribes from game events so stale handlers don't leak on the shared broker.
    /// </summary>
    public void Dispose()
    {
        _messageBroker.Unsubscribe(_onGameStartedHandler);
        _messageBroker.Unsubscribe(_onDecisionRequestHandler);
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private void OnGameStarted(GameStartedEvent evt)
    {
        _playgroundId = evt.PlaygroundId;
        _messageBroker.Publish(new AiReadyToActionsResponse(Guid.NewGuid(), evt.PlaygroundId, evt.Id));
    }

    private void OnDecisionRequest(RequestAgentDecisionMakeCommand cmd)
    {
        if (cmd.PlaygroundId != _playgroundId) return;

        var agent = _agentStateRepository.LoadObject(cmd.AgentId);
        if (agent is null) return;
        if (agent.Type != TargetAgentType) return;

        var obs     = ObservationBuilder.Build(agent);
        var request = new ActRequest
        {
            RunId = _modelPath,
            AlgorithmType = ModelType.ToString().ToLowerInvariant()
        };
        request.Observation.AddRange(obs);

        // Fire-and-forget: await the gRPC call asynchronously (same pattern as Sb3Actions)
        // so the message-broker thread is never blocked, avoiding thread-pool starvation.
        var cmdId    = cmd.Id;
        var agentId  = cmd.AgentId;
        var agentPos = agent.Coordinates;

        _ = Task.Run(async () =>
        {
            int action = 0;
            try
            {
                var actResponse = await _policyTrainerClient.ActAsync(request).ConfigureAwait(false);
                if (actResponse.Success)
                {
                    action = actResponse.Action;
                }
                else
                {
                    // Log once per episode to avoid flooding during mass simulation.
                    if (!_actFailureLogged)
                    {
                        _actFailureLogged = true;
                        _logger?.LogWarning(
                            "[InferenceActions] Act RPC failed: {ErrorMessage}. " +
                            "ModelPath={ModelPath}, Algorithm={Algorithm}. Defaulting to action 0 (up). " +
                            "This usually means the Python service could not load the model file.",
                            actResponse.ErrorMessage, _modelPath, ModelType);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_actFailureLogged)
                {
                    _actFailureLogged = true;
                    _logger?.LogWarning(ex,
                        "[InferenceActions] gRPC ActAsync threw. ModelPath={ModelPath}, Algorithm={Algorithm}. " +
                        "Defaulting to action 0 (up).",
                        _modelPath, ModelType);
                }
            }

            var response = ObservationBuilder.BuildDecisionResponse(cmdId, agentId, agentPos, action);
            _messageBroker.Publish(response);
        });
    }
}
