using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto.Performance;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.TestPreconditionSet;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.Mappers;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Executors;

public class ExecutorFactory : IExecutorFactory
{
    private readonly IPlaygroundCommandsHandleService _mapCommands;
    private readonly IMemoryDataManager<StandardPlayground> _sandboxRepository;
    private readonly IFileDataManager<StandardPlaygroundState> _playgroundStateFileRepository;
    private readonly IMessageBroker _messageBroker;
    private readonly IBrokerRpcClient _brokerRpcClient;
    private readonly IStandardPlaygroundMapper _standardPlaygroundMapper;
    private readonly IFileDataManager<RawDataLog> _rawDataLogFileRepository;
    private readonly IFileDataManager<TurnExecutionPerformance> _turnExecutionPerformanceFileRepository;
    private readonly IFileDataManager<SandboxExecutionPerformance> _sandboxExecutionPerformanceFileRepository;
    private readonly ITestPreconditionData _testPreconditionData;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPolicyTrainerClient _policyTrainerClient;

    public ExecutorFactory(IPlaygroundCommandsHandleService mapCommands,
        IMemoryDataManager<StandardPlayground> sandboxRepository,
        IFileDataManager<StandardPlaygroundState> playgroundStateFileRepository,
        IMessageBroker messageBroker,
        IBrokerRpcClient brokerRpcClient,
        IStandardPlaygroundMapper standardPlaygroundMapper,
        IFileDataManager<RawDataLog> rawDataLogFileRepository,
        IFileDataManager<TurnExecutionPerformance> turnExecutionPerformanceFileRepository,
        IFileDataManager<SandboxExecutionPerformance> sandboxExecutionPerformanceFileRepository,
        ITestPreconditionData testPreconditionData,
        ILoggerFactory loggerFactory,
        IPolicyTrainerClient policyTrainerClient)
    {
        _mapCommands = mapCommands;
        _sandboxRepository = sandboxRepository;
        _playgroundStateFileRepository = playgroundStateFileRepository;
        _messageBroker = messageBroker;
        _brokerRpcClient = brokerRpcClient;
        _standardPlaygroundMapper = standardPlaygroundMapper;
        _rawDataLogFileRepository = rawDataLogFileRepository;
        _turnExecutionPerformanceFileRepository = turnExecutionPerformanceFileRepository;
        _sandboxExecutionPerformanceFileRepository = sandboxExecutionPerformanceFileRepository;
        _testPreconditionData = testPreconditionData;
        _loggerFactory = loggerFactory;
        _policyTrainerClient = policyTrainerClient;
    }

    public IExecutorForPresentation CreateExecutorForPresentation(
        SandBoxConfiguration configuration,
        AgentAiSpec heroAiSpec,
        AgentAiSpec enemyAiSpec,
        int actionDelayMs = 0,
        SemaphoreSlim? pauseGate = null)
    {
        // Presentation executors must use the shared singleton broker so that
        // SimulationVisualizationBridge (which subscribes on the same singleton)
        // receives all events and can forward them to the SignalR hub.
        var agentStore = new MemoryDataManager<AgentStateForAIDecision>();
        var heroAi  = CreateAiActions(heroAiSpec, ObjectType.Hero, _messageBroker, agentStore);
        var enemyAi = CreateAiActions(enemyAiSpec, ObjectType.Enemy, _messageBroker, agentStore);

        return new ExecutorForPresentation(
            _mapCommands,
            _sandboxRepository,
            heroAi,
            enemyAi,
            configuration,
            _playgroundStateFileRepository,
            agentStore,
            _messageBroker,
            _brokerRpcClient,
            _standardPlaygroundMapper,
            _rawDataLogFileRepository,
            _turnExecutionPerformanceFileRepository,
            _sandboxExecutionPerformanceFileRepository,
            _testPreconditionData,
            actionDelayMs,
            pauseGate);
    }

    public IStandardExecutor CreateStandardExecutor(
        SandBoxConfiguration configuration,
        AgentAiSpec heroAiSpec,
        AgentAiSpec enemyAiSpec)
    {
        // Create fully isolated instances per simulation so that concurrent
        // simulations running on different thread-pool threads share NO mutable
        // state in the message/AI pipeline.
        var broker     = new AuxiliumLab.AiSandbox.Common.MessageBroker.MessageBroker();
        var rpcClient  = new BrokerRpcClient(broker);
        var agentStore = new MemoryDataManager<AgentStateForAIDecision>();
        var heroAi     = CreateAiActions(heroAiSpec, ObjectType.Hero, broker, agentStore);
        var enemyAi    = CreateAiActions(enemyAiSpec, ObjectType.Enemy, broker, agentStore);

        return new StandardExecutor(
            _mapCommands,
            _sandboxRepository,
            heroAi,
            enemyAi,
            configuration,
            _playgroundStateFileRepository,
            agentStore,
            broker,
            rpcClient,
            _standardPlaygroundMapper,
            _rawDataLogFileRepository,
            _turnExecutionPerformanceFileRepository,
            _sandboxExecutionPerformanceFileRepository,
            _testPreconditionData);
    }

    /// <summary>
    /// Creates an <see cref="IAiActions"/> from the given spec.
    /// Random → <see cref="RandomActions"/>; otherwise → <see cref="InferenceActions"/>.
    /// </summary>
    private IAiActions CreateAiActions(AgentAiSpec spec, ObjectType targetAgentType, IMessageBroker broker, IMemoryDataManager<AgentStateForAIDecision> agentStore)
    {
        if (spec.ModelType == ModelType.Random)
            return new RandomActions(broker, agentStore, targetAgentType);

        return new InferenceActions(
            broker,
            agentStore,
            _policyTrainerClient,
            spec.ModelPath ?? throw new InvalidOperationException("ModelPath required for trained AI"),
            spec.AiConfig ?? new AiConfiguration { ModelType = spec.ModelType, Version = "1.0", PolicyType = AiPolicy.MLP },
            targetAgentType,
            _loggerFactory.CreateLogger<InferenceActions>());
    }
}
