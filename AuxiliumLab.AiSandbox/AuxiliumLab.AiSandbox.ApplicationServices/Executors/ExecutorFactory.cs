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
using Microsoft.Extensions.Options;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Executors;

public class ExecutorFactory : IExecutorFactory
{
    private readonly IPlaygroundCommandsHandleService _mapCommands;
    private readonly IMemoryDataManager<StandardPlayground> _sandboxRepository;
    private readonly IAiActions _aiActions;
    private readonly IOptions<SandBoxConfiguration> _configuration;
    private readonly IFileDataManager<StandardPlaygroundState> _playgroundStateFileRepository;
    private readonly IMemoryDataManager<AgentStateForAIDecision> _agentStateMemoryRepository;
    private readonly IMessageBroker _messageBroker;
    private readonly IBrokerRpcClient _brokerRpcClient;
    private readonly IStandardPlaygroundMapper _standardPlaygroundMapper;
    private readonly IFileDataManager<RawDataLog> _rawDataLogFileRepository;
    private readonly IFileDataManager<TurnExecutionPerformance> _turnExecutionPerformanceFileRepository;
    private readonly IFileDataManager<SandboxExecutionPerformance> _sandboxExecutionPerformanceFileRepository;
    private readonly ITestPreconditionData _testPreconditionData;

    public ExecutorFactory(IPlaygroundCommandsHandleService mapCommands,
        IMemoryDataManager<StandardPlayground> sandboxRepository,
        IAiActions aiActions,
        IOptions<SandBoxConfiguration> configuration,
        IFileDataManager<StandardPlaygroundState> playgroundStateFileRepository,
        IMemoryDataManager<AgentStateForAIDecision> agentStateMemoryRepository,
        IMessageBroker messageBroker,
        IBrokerRpcClient brokerRpcClient,
        IStandardPlaygroundMapper standardPlaygroundMapper,
        IFileDataManager<RawDataLog> rawDataLogFileRepository,
        IFileDataManager<TurnExecutionPerformance> turnExecutionPerformanceFileRepository,
        IFileDataManager<SandboxExecutionPerformance> sandboxExecutionPerformanceFileRepository,
        ITestPreconditionData testPreconditionData)
    {
        _mapCommands = mapCommands;
        _sandboxRepository = sandboxRepository;
        _aiActions = aiActions;
        _configuration = configuration;
        _playgroundStateFileRepository = playgroundStateFileRepository;
        _agentStateMemoryRepository = agentStateMemoryRepository;
        _messageBroker = messageBroker;
        _brokerRpcClient = brokerRpcClient;
        _standardPlaygroundMapper = standardPlaygroundMapper;
        _rawDataLogFileRepository = rawDataLogFileRepository;
        _turnExecutionPerformanceFileRepository = turnExecutionPerformanceFileRepository;
        _sandboxExecutionPerformanceFileRepository = sandboxExecutionPerformanceFileRepository;
        _testPreconditionData = testPreconditionData;
    }

    public IExecutorForPresentation CreateExecutorForPresentation(
        int actionDelayMs = 0,
        SemaphoreSlim? pauseGate = null)
    {
        return new ExecutorForPresentation(
            _mapCommands,
            _sandboxRepository,
            _aiActions,
            _configuration,
            _playgroundStateFileRepository,
            _agentStateMemoryRepository,
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

    public IStandardExecutor CreateStandardExecutor()
    {
        // Create fully isolated instances per simulation so that concurrent
        // simulations running on different thread-pool threads share NO mutable
        // state in the message/AI pipeline.  This eliminates:
        //   1. The global lock in MessageBroker.Publish (all N handlers under one lock)
        //   2. Subscriber proliferation: each Initialize() previously accumulated
        //      another handler on the shared broker, causing NÃ—wasted CPU work per
        //      decision (N handlers respond but only 1 result is consumed)
        //
        // Note: IMemoryDataManager<StandardPlayground> stays shared because
        //   CreatePlaygroundCommandHandler saves to that singleton, and each
        //   simulation uses a unique sandboxId GUID so there are no key collisions.
        var broker     = new AuxiliumLab.AiSandbox.Common.MessageBroker.MessageBroker();
        var rpcClient  = new BrokerRpcClient(broker);
        var agentStore = new MemoryDataManager<AgentStateForAIDecision>(); // per-sim: no GUID collisions and keeps broker/AI pair consistent
        var aiActions  = new RandomActions(broker, agentStore);

        return new StandardExecutor(
            _mapCommands,
            _sandboxRepository, // shared: CreatePlaygroundCommandHandler writes here; unique GUIDs prevent collisions
            aiActions,          // per-sim: subscribes to its own broker only
            _configuration,
            _playgroundStateFileRepository,
            agentStore,         // per-sim: matches the broker/aiActions pair
            broker,             // per-sim: no shared publish lock
            rpcClient,          // per-sim: subscribes to its own broker
            _standardPlaygroundMapper,
            _rawDataLogFileRepository,
            _turnExecutionPerformanceFileRepository,
            _sandboxExecutionPerformanceFileRepository,
            _testPreconditionData);
    }

    /// <inheritdoc/>
    public IStandardExecutor CreateInferenceExecutor(
        IPolicyTrainerClient policyTrainerClient,
        string modelPath,
        AiConfiguration aiConfig)
    {
        // Each parallel simulation gets its own isolated broker, agent-store, and
        // InferenceActions instance (InferenceActions stores per-game _playgroundId).
        // The policyTrainerClient and modelPath are shared and read-only after construction.
        var broker     = new AuxiliumLab.AiSandbox.Common.MessageBroker.MessageBroker();
        var rpcClient  = new BrokerRpcClient(broker);
        var agentStore = new MemoryDataManager<AgentStateForAIDecision>();
        var aiActions  = new InferenceActions(
            broker,
            agentStore,
            policyTrainerClient,
            modelPath,
            aiConfig);

        return new StandardExecutor(
            _mapCommands,
            _sandboxRepository,
            aiActions,
            _configuration,
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
}
