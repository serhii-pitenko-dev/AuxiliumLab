using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.AggregationRun;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.AggregationRun.Dto;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.AggregationRun;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.AggregationRun.Dto;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.AggregationRunner;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects.StartupSettings;
using AuxiliumLab.AiSandbox.Statistics.Preconditions;
using AuxiliumLab.AiSandbox.Statistics.StatisticDataManager;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Jobs.AggregationRun;

/// <summary>
/// Singleton service that implements both <see cref="IAggregationRunCommands"/> and
/// <see cref="IAggregationRunQueries"/>. Launches aggregation runs on background threads
/// and tracks job state in memory.
/// </summary>
public sealed class AggregationJobService : IAggregationRunCommands, IAggregationRunQueries
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TrainingSettings _trainingSettings;
    private readonly Sb3AlgorithmTypeProvider _algorithmTypeProvider;
    private readonly IPolicyTrainerClient _policyTrainerClient;
    private readonly GymBrokerRegistry _gymBrokerRegistry;
    private readonly IOptions<SandBoxConfiguration> _sandboxConfig;
    private readonly IOptions<FileSourceConfiguration> _fileSourceConfig;
    private readonly IOptions<AggregationSettings> _aggregationSettings;
    private readonly IStatisticFileDataManager _statisticFileManager;

    private readonly ConcurrentDictionary<Guid, AggregationJobStatusDto> _jobs = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobCts = new();

    public AggregationJobService(
        IServiceProvider serviceProvider,
        TrainingSettings trainingSettings,
        Sb3AlgorithmTypeProvider algorithmTypeProvider,
        IPolicyTrainerClient policyTrainerClient,
        GymBrokerRegistry gymBrokerRegistry,
        IOptions<SandBoxConfiguration> sandboxConfig,
        IOptions<FileSourceConfiguration> fileSourceConfig,
        IOptions<AggregationSettings> aggregationSettings,
        IStatisticFileDataManager statisticFileManager)
    {
        _serviceProvider      = serviceProvider;
        _trainingSettings     = trainingSettings;
        _algorithmTypeProvider = algorithmTypeProvider;
        _policyTrainerClient  = policyTrainerClient;
        _gymBrokerRegistry    = gymBrokerRegistry;
        _sandboxConfig        = sandboxConfig;
        _fileSourceConfig     = fileSourceConfig;
        _aggregationSettings  = aggregationSettings;
        _statisticFileManager = statisticFileManager;
    }

    // ── IAggregationRunCommands ──────────────────────────────────────────────

    public Task<AggregationJobStartedDto> StartAggregationAsync(
        StartAggregationCommand command, CancellationToken ct = default)
    {
        var steps = ResolveSteps(command);
        var jobId     = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;

        var status = new AggregationJobStatusDto
        {
            JobId          = jobId,
            State          = AggregationJobState.Running,
            StartedAt      = startedAt,
            StepNames      = steps.Select(s => s.Name).ToList(),
            CurrentStep    = steps.FirstOrDefault()?.Name,
            CompletedSteps = 0
        };
        _jobs[jobId] = status;

        var aggCts = new CancellationTokenSource();
        _jobCts[jobId] = aggCts;

        _ = Task.Run(async () =>
        {
            try
            {
                string algorithmsFolderPath = Path.Combine(
                    _fileSourceConfig.Value.FileStorage.BasePath,
                    _fileSourceConfig.Value.FileStorage.TrainedAlgorithms);

                var runner = new AggregationRunner(
                    _serviceProvider,
                    _trainingSettings,
                    _algorithmTypeProvider,
                    _policyTrainerClient,
                    _gymBrokerRegistry,
                    _statisticFileManager,
                    _sandboxConfig,
                    algorithmsFolderPath);

                var incrementalProperties = new SimulationIncrementalPropertiesSettings
                {
                    SimulationCount = command.IncrementalSweep?.SimulationCount ?? 1,
                    Properties      = command.IncrementalSweep?.Properties ?? []
                };

                // Track current step via callback by monitoring step execution
                int stepIndex = 0;
                foreach (var step in steps)
                {
                    status.CurrentStep = step.Name;
                    stepIndex++;
                }

                await runner.RunAggregationAsync(
                    steps,
                    command.StandardSimulationCount,
                    incrementalProperties,
                    command.Algorithm,
                    command.PolicyType,
                    command.TrainingOverrides);

                status.CompletedSteps = steps.Count;
                status.CurrentStep    = null;
                status.State          = AggregationJobState.Completed;
                status.CompletedAt    = DateTime.UtcNow;
            }
            catch (OperationCanceledException)
            {
                status.State        = AggregationJobState.Failed;
                status.CompletedAt  = DateTime.UtcNow;
                status.ErrorMessage = "Stopped by user.";
            }
            catch (Exception ex)
            {
                status.State        = AggregationJobState.Failed;
                status.CompletedAt  = DateTime.UtcNow;
                status.ErrorMessage = ex.Message;
            }
            finally
            {
                _jobCts.TryRemove(jobId, out _);
            }
        });

        return Task.FromResult(new AggregationJobStartedDto(
            jobId,
            steps.Select(s => s.Name).ToList(),
            startedAt));
    }

    // ── IAggregationRunQueries ───────────────────────────────────────────────

    public Task<IReadOnlyList<AggregationJobStatusDto>> GetAggregationStatusesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AggregationJobStatusDto>>(_jobs.Values.ToList());

    public Task<bool> StopAggregationAsync(Guid jobId, CancellationToken ct = default)
    {
        if (!_jobs.TryGetValue(jobId, out var status) || status.State != AggregationJobState.Running)
            return Task.FromResult(false);

        if (_jobCts.TryGetValue(jobId, out var cts))
            cts.Cancel();

        status.State        = AggregationJobState.Failed;
        status.CompletedAt  = DateTime.UtcNow;
        status.ErrorMessage = "Stopped by user.";
        return Task.FromResult(true);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private IReadOnlyList<AggregationStepConfiguration> ResolveSteps(StartAggregationCommand command)
    {
        IEnumerable<(string Name, string Mode)> source = command.Steps.Count > 0
            ? command.Steps.Select(s => (s.Name, s.Mode))
            : _aggregationSettings.Value.Steps.Select(s => (s.Name, s.Mode));

        return source
            .Select(s => new AggregationStepConfiguration(
                s.Name,
                Enum.TryParse<ExecutionMode>(s.Mode, out var mode)
                    ? mode
                    : throw new ArgumentException($"Unknown aggregation step mode '{s.Mode}'.")))
            .ToList();
    }
}
