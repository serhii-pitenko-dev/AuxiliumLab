using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto.Performance;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.TestPreconditionSet;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.Mappers;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Lost;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Win;
using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Domain.Statistics.Result;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Executors;

public class StandardExecutor : Executor, IStandardExecutor
{
    public StandardExecutor(
        IPlaygroundCommandsHandleService mapCommands,
        IMemoryDataManager<StandardPlayground> sandboxRepository,
        IAiActions heroAiActions,
        IAiActions enemyAiActions,
        SandBoxConfiguration configuration,
        IFileDataManager<StandardPlaygroundState> playgroundStateFileRepository,
        IMemoryDataManager<AgentStateForAIDecision> agentStateMemoryRepository,
        IMessageBroker messageBroker,
        IBrokerRpcClient brokerRpcClient,
        IStandardPlaygroundMapper standardPlaygroundMapper,
        IFileDataManager<RawDataLog> rawDataLogFileRepository,
        IFileDataManager<TurnExecutionPerformance> turnExecutionPerformanceFileRepository,
        IFileDataManager<SandboxExecutionPerformance> sandboxExecutionPerformanceFileRepository,
        ITestPreconditionData testPreconditionData) :
        base(mapCommands, sandboxRepository, heroAiActions, enemyAiActions,
             configuration, playgroundStateFileRepository, agentStateMemoryRepository, messageBroker,
             brokerRpcClient, standardPlaygroundMapper, rawDataLogFileRepository,
             turnExecutionPerformanceFileRepository, sandboxExecutionPerformanceFileRepository,
             testPreconditionData)
    {
    }

    /// <inheritdoc/>
    public Task<ParticularRun> RunAndCaptureAsync() => RunAndCaptureAsync(default);

    /// <inheritdoc/>
    public async Task<ParticularRun> RunAndCaptureAsync(SandBoxConfiguration sandBoxConfiguration)
    {
        WinReason? winReason = null;
        LostReason? lostReason = null;

        void OnWon(HeroWonEvent e) { winReason = e.WinReason; }
        void OnLost(HeroLostEvent e) { lostReason = e.LostReason; }

        _messageBroker.Subscribe<HeroWonEvent>(OnWon);
        _messageBroker.Subscribe<HeroLostEvent>(OnLost);

        try
        {
            await RunAsync(default, sandBoxConfiguration);
        }
        finally
        {
            _messageBroker.Unsubscribe<HeroWonEvent>(OnWon);
            _messageBroker.Unsubscribe<HeroLostEvent>(OnLost);
        }

        return new ParticularRun(
            _playground.Id,
            _playground.Turn,
            _playground.Enemies.Count,
            winReason,
            lostReason);
    }

}

