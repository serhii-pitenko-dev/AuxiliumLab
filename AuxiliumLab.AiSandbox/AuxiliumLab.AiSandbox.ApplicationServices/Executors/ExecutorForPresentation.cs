using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto.Performance;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.TestPreconditionSet;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.Mappers;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using Microsoft.Extensions.Options;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Executors;

public class ExecutorForPresentation : Executor, IExecutorForPresentation
{
    /// <inheritdoc/>
    protected override bool NeedsStatePersistence => true;

    /// <inheritdoc/>
    protected override bool NeedsAgentNotifications => true;

    /// <inheritdoc/>
    public int ActionDelayMs { get; set; } = 0;

    /// <inheritdoc/>
    public SemaphoreSlim? PauseGate { get; set; }

    public ExecutorForPresentation(
        IPlaygroundCommandsHandleService mapCommands,
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
        ITestPreconditionData testPreconditionData) :
        base(mapCommands, sandboxRepository, aiActions,
             configuration,
             playgroundStateFileRepository, agentStateMemoryRepository, messageBroker,
             brokerRpcClient, standardPlaygroundMapper, rawDataLogFileRepository,
             turnExecutionPerformanceFileRepository, sandboxExecutionPerformanceFileRepository,
             testPreconditionData)
    {
    }

    protected override async Task WaitIfPausedAsync(CancellationToken ct)
    {
        if (PauseGate is null) return;
        await PauseGate.WaitAsync(ct);
        PauseGate.Release();
    }

    protected override async Task SendAgentMoveNotificationAsync(Guid id, Guid playgroundId, Guid agentId, Coordinates from, Coordinates to, bool isSuccess, Agent agent)
    {
        if (ActionDelayMs > 0)
            await Task.Delay(ActionDelayMs);

        var moveEvent = new OnAgentMoveActionEvent(
            id,
            playgroundId,
            agentId,
            from,
            to,
            isSuccess,
            GetAgentSnapshot(agent));

        _messageBroker.Publish(moveEvent);
    }

    protected override async Task SendAgentToggleActionNotificationAsync(AgentAction action, Guid playgroundId, Guid agentId, bool isActivated, Agent agent)
    {
        if (ActionDelayMs > 0)
            await Task.Delay(ActionDelayMs);

        var actionEvent = new OnAgentToggleActionEvent(
            Guid.NewGuid(),
            playgroundId,
            agentId,
            action,
            isActivated,
            GetAgentSnapshot(agent));

        _messageBroker.Publish(actionEvent);
    }

    private AgentSnapshot GetAgentSnapshot(Agent agent)
    {
        return new AgentSnapshot(
            agent.Id,
            agent.Type,
            agent.Speed,
            agent.SightRange,
            agent.IsRun,
            agent.Stamina,
            agent.MaxStamina,
            agent.OrderInTurnQueue);
    }
}

