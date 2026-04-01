using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground.CreatePlayground;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto.Performance;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.TestPreconditionSet;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.Mappers;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Commands;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Responses;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Lost;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Win;
using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

#if CONSOLE_PRESENTATION_DEBUG
#warning CONSOLE_PRESENTATION_DEBUG is ON
#else
#warning CONSOLE_PRESENTATION_DEBUG is OFF
#endif

namespace AuxiliumLab.AiSandbox.ApplicationServices.Executors;

public abstract class Executor : IExecutor, IDisposable
{
    private readonly IPlaygroundCommandsHandleService _playgroundCommands;
    private readonly IMemoryDataManager<StandardPlayground> _playgroundRepository;
    private readonly SandBoxConfiguration _configuration;
    protected readonly IMessageBroker _messageBroker;
    protected StandardPlayground _playground;
    protected List<Agent> _agentsToAct = new();
    protected IMemoryDataManager<AgentStateForAIDecision> _agentStateMemoryRepository;
    protected IBrokerRpcClient _brokerRpcClient;
    protected IAiActions _aiActions;
    protected IStandardPlaygroundMapper _standardPlaygroundMapper;
    protected IFileDataManager<StandardPlaygroundState> _playgroundStateFileRepository;
    protected IFileDataManager<RawDataLog> _rawDataLogFileRepository;
    protected IFileDataManager<TurnExecutionPerformance> _turnExecutionPerformanceFileRepository;
    protected IFileDataManager<SandboxExecutionPerformance> _sandboxExecutionPerformanceFileRepository;
    protected ITestPreconditionData _testPreconditionData;

    private SandboxStatus sandboxStatus;
    protected SandboxStatus SandboxStatus => sandboxStatus;
    private SandboxExecutionPerformance sandboxExecutionPerformance;

    /// <summary>
    /// When <c>true</c>, the playground state is persisted to the file repository
    /// before <see cref="GameStartedEvent"/> is published so that a presentation layer
    /// (e.g. <see cref="ConsoleRunner"/>) can load it in its OnGameStarted handler.
    /// Non-interactive executors (mass runs, training) should leave this <c>false</c>
    /// to avoid unnecessary I/O on every simulation.
    /// </summary>
    protected virtual bool NeedsStatePersistence => false;

    /// <summary>
    /// When <c>true</c>, agent move and toggle-action notifications are published via the message broker.
    /// Set to <c>false</c> (default) for non-interactive executors (training, mass runs) to skip
    /// allocations and broker dispatch in the hot simulation loop.
    /// </summary>
    protected virtual bool NeedsAgentNotifications => false;

    /// <summary>
    /// The configuration active for the current run.
    /// Falls back to the injected <see cref="_configuration"/> when no override is supplied via <see cref="RunAsync"/>.
    /// </summary>
    private SandBoxConfiguration _activeConfiguration;

    public Executor(
        IPlaygroundCommandsHandleService playgroundCommandsHandleService,
        IMemoryDataManager<StandardPlayground> sandboxRepository,
        IAiActions aiActions,
        SandBoxConfiguration configuration,
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
        _playgroundCommands = playgroundCommandsHandleService;
        _playgroundRepository = sandboxRepository;
        _configuration = configuration;
        _activeConfiguration = _configuration;
        _agentStateMemoryRepository = agentStateMemoryRepository;
        _messageBroker = messageBroker;
        _brokerRpcClient = brokerRpcClient;
        _aiActions = aiActions;
        _standardPlaygroundMapper = standardPlaygroundMapper;
        _playgroundStateFileRepository = playgroundStateFileRepository;
        _rawDataLogFileRepository = rawDataLogFileRepository;
        _turnExecutionPerformanceFileRepository = turnExecutionPerformanceFileRepository;
        _sandboxExecutionPerformanceFileRepository = sandboxExecutionPerformanceFileRepository;
        _testPreconditionData = testPreconditionData;
    }

    public void Dispose()
    {
        (_aiActions as IDisposable)?.Dispose();
    }

    public async Task TestRunWithPreconditionsAsync()
    {
        var sandboxId = _testPreconditionData.CreatePlaygroundWithPreconditions();
        await RunAsync(sandboxId);
    }

    public virtual async Task<SandboxStatus> RunAsync(Guid sandboxId = default, SandBoxConfiguration sandBoxConfiguration = default, CancellationToken cancellationToken = default)
    {
        _activeConfiguration = sandBoxConfiguration ?? _configuration;

#if PERFORMANCE_ANALYSIS
        sandboxExecutionPerformance = new SandboxExecutionPerformance
            {
                Start = DateTime.UtcNow,
            };
#endif

        // Create standard map/sandbox and save it
        if (sandboxId == default)
        {
            sandboxId = _playgroundCommands.CreatePlaygroundCommand.Handle(new CreatePlaygroundCommandParameters(
                _activeConfiguration.MapSettings,
                _activeConfiguration.Hero,
                _activeConfiguration.Enemy
            ));
        }

        // Load the created sandbox
        _playground = _playgroundRepository.LoadObject(sandboxId);

        // Invoke game started events for playground
        await StartSimulationPreparationsAsync(cancellationToken);

        return sandboxStatus;
    }

    /// <summary>
    /// On start simulation actions
    /// </summary>
    protected virtual async Task StartSimulationPreparationsAsync(CancellationToken cancellationToken = default)
    {
        // Initialize AI module
        _aiActions.Initialize();

        if (NeedsStatePersistence)
            await SaveAsync();

        //Before starting the simulation, let all agents look around to have initial visible cells data in the repository for AI decision making
        _playground.LookAroundEveryone();

        // Notify everyone that the simulation has started
        var result =
            await _brokerRpcClient.RequestAsync<GameStartedEvent, AiReadyToActionsResponse>(new GameStartedEvent(Guid.NewGuid(), _playground.Id));

        try
        {
           await StartSimulationAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Simulation was stopped externally — clean state but don't re-throw
            if (NeedsStatePersistence)
                await SaveAsync();
        }
        catch (Exception ex)
        {
            await CreateRawLog($"Playground with id {_playground.Id} crashed. Exception: {ex.Message}");
            if (NeedsStatePersistence)
                await SaveAsync();
            throw;
        }
    }

    /// <summary>
    /// Called before each turn. Override to block the loop while paused.
    /// </summary>
    protected virtual Task WaitIfPausedAsync(CancellationToken ct) => Task.CompletedTask;

    protected async Task StartSimulationAsync(CancellationToken cancellationToken)
    {
        while (sandboxStatus == SandboxStatus.InProgress && !cancellationToken.IsCancellationRequested)
        {
            await WaitIfPausedAsync(cancellationToken);

            if (_playground.Turn >= _activeConfiguration.MaxTurns.Current)
            {
                _messageBroker.Publish(new HeroLostEvent(Guid.NewGuid(), _playground.Id, LostReason.MaxTurnsReached));
                sandboxStatus = SandboxStatus.TurnLimitReached;
                break;
            }
               
            _playground.OnStartTurnActions();

#if PERFORMANCE_ANALYSIS
            sandboxExecutionPerformance.TurnPerformances[_playground.Turn] = new TurnExecutionPerformance
                {
                    TurnNumber = _playground.Turn,
                    Start = DateTime.UtcNow,
                };
#endif
            await ExecuteTurnAsync(cancellationToken);

#if PERFORMANCE_ANALYSIS
            sandboxExecutionPerformance.TurnPerformances[_playground.Turn].Finish = DateTime.UtcNow;
             await _turnExecutionPerformanceFileRepository.SaveOrAppendAsync(_playground.Id, sandboxExecutionPerformance.TurnPerformances[_playground.Turn]);
#endif
            OnTurnEnded();
        }
    }

    private async Task ExecuteTurnAsync(CancellationToken cancellationToken)
    {
        // Let all agents look around initially
        _playground.LookAroundEveryone();

        _agentsToAct = _playground.GetOrderedAgentsForTurn();

        while (_agentsToAct.Count > 0 && sandboxStatus == SandboxStatus.InProgress)
        {
            var agent = _agentsToAct[0];
            _playground.PrepareAgentForTurnActions(agent);

#if PERFORMANCE_ANALYSIS
            sandboxExecutionPerformance.TurnPerformances[_playground.Turn].ActionPerformances[agent.Id] = new List<ActionExecutionPerformance>();
#endif

            while (agent.AvailableActions.Count > 0 && sandboxStatus == SandboxStatus.InProgress)
            {

#if PERFORMANCE_ANALYSIS
                var actionPerformance = new ActionExecutionPerformance
                {
                    Start = DateTime.UtcNow,
                    ObjectType = agent.Type

                };
#endif

                AgentDecisionBaseResponse agentDecision = await SendAgentActionRequestAsync(agent, _playground.Id, cancellationToken);
                await ApplyAgentActionAsync(agentDecision);
                await WaitIfPausedAsync(cancellationToken);

#if PERFORMANCE_ANALYSIS
                actionPerformance.Finish = DateTime.UtcNow;
                actionPerformance.Action = agentDecision.ActionType;
                sandboxExecutionPerformance.TurnPerformances[_playground.Turn]
                    .ActionPerformances[agent.Id].Add(actionPerformance);
#endif

            }
            _agentsToAct.RemoveAt(0);
        }
    }

    private async Task<AgentDecisionBaseResponse> SendAgentActionRequestAsync(Agent agent, Guid playgroundId, CancellationToken cancellationToken)
    {
        // Convert agent data to message format
        var visibleCells = agent.VisibleCells.Select(cell => new VisibleCellData(
            cell.Coordinates,
            cell.Object.Type,
            cell.Object.Id,
            cell.Object.Transparent
        )).ToList();

        var agentState = new AgentStateForAIDecision(
            playgroundId,
            agent.Id,
            agent.Type,
            agent.Coordinates,
            agent.Speed,
            agent.SightRange,
            agent.IsRun,
            agent.Stamina,
            agent.MaxStamina,
            visibleCells,
            agent.AvailableActions,
            agent.ExecutedActions
        );

        _agentStateMemoryRepository.AddOrUpdate(agent.Id, agentState);


        return await _brokerRpcClient.RequestAsync<RequestAgentDecisionMakeCommand, AgentDecisionBaseResponse>(
               new RequestAgentDecisionMakeCommand(Guid.NewGuid(), playgroundId, agent.Id), cancellationToken);
    }

    private async Task SaveAsync()
    {
        StandardPlaygroundState dataToSave = _standardPlaygroundMapper.ToState(_playground);
        await _playgroundStateFileRepository.SaveOrAppendAsync(_playground.Id, dataToSave);
    }


    protected virtual void OnTurnEnded()
    {
        _messageBroker.Publish(new TurnExecutedEvent(Guid.NewGuid(), _playground.Id, _playground.Turn));
    }

    private async Task ApplyAgentActionAsync(AgentDecisionBaseResponse action)
    {
        // Find the agent
        var agent = _playground.Hero.Id == action.AgentId
            ? (Agent)_playground.Hero
            : _playground.Enemies.FirstOrDefault(e => e.Id == action.AgentId);

        if (agent == null)
            return;

        switch (action)
        {
            case AgentDecisionMoveResponse moveEvent when moveEvent.IsSuccess && moveEvent.From != moveEvent.To:
                // Apply movement
                bool isSuccess;
                var result = CheckGameStatusAndMovingPossibility(agent, moveEvent.To);
                if (result.MovePossibility == true && result.GameStatus == SandboxStatus.InProgress)
                {
                    isSuccess = true;
                    _playground.MoveObjectOnMap(moveEvent.From, moveEvent.To);

                }
                else
                {
                    isSuccess = false;
                    agent.ActionFailed(AgentAction.Move);
                    sandboxStatus = result.GameStatus;
                }

            if (NeedsAgentNotifications)
                await SendAgentMoveNotificationAsync(moveEvent.Id, _playground.Id, agent.Id, moveEvent.From, moveEvent.To, isSuccess, agent);
                break;

            case AgentDecisionUseAbilityResponse abilityEvent:
                // Apply ability activation/deactivation
                if (abilityEvent.IsSuccess)
                    agent.DoAction(abilityEvent.ActionType, abilityEvent.IsActivated);
                else
                    agent.ActionFailed(abilityEvent.ActionType);

                if (NeedsAgentNotifications)
                    await SendAgentToggleActionNotificationAsync(abilityEvent.ActionType, _playground.Id, agent.Id, abilityEvent.IsActivated, agent);

                break;
        }
    }

    private (SandboxStatus GameStatus, bool MovePossibility) CheckGameStatusAndMovingPossibility(Agent agent, Coordinates moveTo)
    {
        if (agent.Stamina == 0)
            return (SandboxStatus.InProgress, false);

        // Out-of-bounds coordinates (e.g. Sb3Actions moving hero off map edge) are treated as a wall.
        if (!_playground.IsInBounds(moveTo))
            return (SandboxStatus.InProgress, false);

        var moveToCell = _playground.GetCell(moveTo);

        // Check for agent if target cell is empty
        if ((agent.Type is ObjectType.Enemy or ObjectType.Hero) && moveToCell.Object.Type == ObjectType.Empty)
        {
            return (SandboxStatus.InProgress, true);
        }

        // Check for agent if target cell with block (regular or border)
        if ((agent.Type is ObjectType.Enemy or ObjectType.Hero) &&
            (moveToCell.Object.Type == ObjectType.Block || moveToCell.Object.Type == ObjectType.BorderBlock))
        {
            return (SandboxStatus.InProgress, false);
        }

        // Check for enemy is target cell with Hero
        if (agent.Type is ObjectType.Enemy && moveToCell.Object.Type == ObjectType.Hero)
        {
            _messageBroker.Publish(new HeroLostEvent(Guid.NewGuid(), _playground.Id, LostReason.HeroCatched));
            sandboxStatus = SandboxStatus.HeroLost;

            return (SandboxStatus.HeroLost, false);
        }

        // Check for Hero is target cell with Exit
        if (agent.Type is ObjectType.Enemy && moveToCell.Object.Type == ObjectType.Exit)
        {
            return (SandboxStatus.InProgress, false);
        }

        // Check for Hero is target cell with Enemy
        if (agent.Type == ObjectType.Hero && moveToCell.Object.Type == ObjectType.Enemy)
        {
            _messageBroker.Publish(new HeroLostEvent(Guid.NewGuid(), _playground.Id, LostReason.HeroCatched));
            sandboxStatus = SandboxStatus.HeroLost;

            return (SandboxStatus.HeroLost, false);
        }

        // Check for Hero is target cell with Exit
        if (agent.Type == ObjectType.Hero && moveToCell.Object.Type == ObjectType.Exit)
        {
            _messageBroker.Publish(new HeroWonEvent(Guid.NewGuid(), _playground.Id, WinReason.ExitReached));
            sandboxStatus = SandboxStatus.HeroWon;

            return (SandboxStatus.HeroWon, false);
        }

        throw new InvalidOperationException("Unknown move scenario");
    }

    protected virtual Task SendAgentMoveNotificationAsync(
        Guid Id,
        Guid PlaygroundId,
        Guid AgentId,
        Coordinates From,
        Coordinates To,
        bool isSuccess,
        Agent agent) => Task.CompletedTask;

    protected virtual Task SendAgentToggleActionNotificationAsync(
        AgentAction action,
        Guid playgroundId,
        Guid agentId,
        bool isActivated,
        Agent agent) => Task.CompletedTask;

    private async Task CreateRawLog(string logMessage)
    {
        await _rawDataLogFileRepository.SaveOrAppendAsync(
               Guid.NewGuid(), new RawDataLog(Guid.NewGuid(), logMessage, DateTime.UtcNow));
    }
}

