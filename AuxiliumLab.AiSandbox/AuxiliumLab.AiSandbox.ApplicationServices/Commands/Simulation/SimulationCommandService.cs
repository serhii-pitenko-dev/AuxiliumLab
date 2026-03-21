using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.ApplicationServices.Executors;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation;
using AuxiliumLab.AiSandbox.Common.SimulationVisualizationBridge;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.MassRunner;
using AuxiliumLab.AiSandbox.Domain.Statistics.Result;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects.StartupSettings;
using AuxiliumLab.AiSandbox.Statistics.Preconditions;
using AuxiliumLab.AiSandbox.Statistics.StatisticDataManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation;

/// <summary>
/// Singleton service that implements <see cref="ISimulationCommands"/>.
/// Launches simulation runs on background threads and tracks job state in memory.
/// </summary>
public sealed class SimulationCommandService : ISimulationCommands
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<SandBoxConfiguration> _sandboxConfig;
    private readonly IOptions<FileSourceConfiguration> _fileSourceConfig;
    private readonly IPolicyTrainerClient _policyTrainerClient;
    private readonly IStatisticFileDataManager _statisticFileManager;
    private readonly ISimulationVisualizationBridge? _visualizationBridge;

    private readonly ConcurrentDictionary<Guid, SimulationJobStatusDto> _jobs = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobCts = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _pauseHandles = new();

    public SimulationCommandService(
        IServiceProvider serviceProvider,
        IOptions<SandBoxConfiguration> sandboxConfig,
        IOptions<FileSourceConfiguration> fileSourceConfig,
        IPolicyTrainerClient policyTrainerClient,
        IStatisticFileDataManager statisticFileManager,
        ISimulationVisualizationBridge? visualizationBridge = null)
    {
        _serviceProvider      = serviceProvider;
        _sandboxConfig        = sandboxConfig;
        _fileSourceConfig     = fileSourceConfig;
        _policyTrainerClient  = policyTrainerClient;
        _statisticFileManager = statisticFileManager;
        _visualizationBridge  = visualizationBridge;
    }

    // ── ISimulationCommands ──────────────────────────────────────────────────

    public Task<SimulationJobStartedDto> StartSingleSimulationAsync(
        StartSingleSimulationCommand command, CancellationToken ct = default)
    {
        var jobId     = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;
        var status    = new SimulationJobStatusDto
        {
            JobId      = jobId,
            Kind       = command.Kind,
            State      = SandboxStatus.InProgress,
            StartedAt  = startedAt,
            TotalRuns  = 1
        };
        _jobs[jobId] = status;

        var cts = new CancellationTokenSource();
        _jobCts[jobId] = cts;

        var pauseGate = new SemaphoreSlim(1, 1);
        _pauseHandles[jobId] = pauseGate;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var executorFactory = scope.ServiceProvider.GetRequiredService<IExecutorFactory>();

                var executor = executorFactory.CreateExecutorForPresentation(
                    command.ActionDelayMs, pauseGate);
                _visualizationBridge?.Attach(jobId);
                try
                {
                    status.State = await executor.RunAsync(cancellationToken: cts.Token);
                }
                finally
                {
                    _visualizationBridge?.Detach(jobId);
                }

                status.CompletedRuns = 1;
                status.CompletedAt   = DateTime.UtcNow;
            }
            catch (OperationCanceledException)
            {
                status.State        = SandboxStatus.Failed;
                status.CompletedAt  = DateTime.UtcNow;
                status.ErrorMessage = "Stopped by user.";
            }
            catch (Exception ex)
            {
                status.State        = SandboxStatus.Failed;
                status.CompletedAt  = DateTime.UtcNow;
                status.ErrorMessage = ex.Message;
            }
            finally
            {
                _jobCts.TryRemove(jobId, out _);
                _pauseHandles.TryRemove(jobId, out _);
            }
        });

        return Task.FromResult(new SimulationJobStartedDto { JobId = jobId, Kind = command.Kind, StartedAt = startedAt });
    }

    public Task<SimulationJobStartedDto> StartMassSimulationAsync(
        StartMassSimulationCommand command, CancellationToken ct = default)
    {
        var jobId     = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;
        var status    = new SimulationJobStatusDto
        {
            JobId      = jobId,
            Kind       = command.Kind,
            State      = SandboxStatus.InProgress,
            StartedAt  = startedAt,
            TotalRuns  = command.SimulationCount
        };
        _jobs[jobId] = status;

        var massCts = new CancellationTokenSource();
        _jobCts[jobId] = massCts;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var executorFactory  = scope.ServiceProvider.GetRequiredService<IExecutorFactory>();
                var batchFileManager = scope.ServiceProvider.GetRequiredService<IFileDataManager<GeneralBatchRunInformation>>();

                Func<IStandardExecutor> createExecutor = BuildExecutorCreator(command.Kind, command.Algorithm, executorFactory);

                var startupSettings = BuildSimulationStartupSettings(command.IncrementalSweep);
                var massRunner = new MassRunner(batchFileManager, _statisticFileManager, _sandboxConfig);
                var result = await massRunner.RunManyAsync(createExecutor, command.SimulationCount, startupSettings: startupSettings);

                status.CompletedRuns = result.StandardBatch.TotalRuns;
                status.State         = SandboxStatus.TurnLimitReached;
                status.CompletedAt   = DateTime.UtcNow;
            }
            catch (OperationCanceledException)
            {
                status.State        = SandboxStatus.Failed;
                status.CompletedAt  = DateTime.UtcNow;
                status.ErrorMessage = "Stopped by user.";
            }
            catch (Exception ex)
            {
                status.State        = SandboxStatus.Failed;
                status.CompletedAt  = DateTime.UtcNow;
                status.ErrorMessage = ex.Message;
            }
            finally
            {
                _jobCts.TryRemove(jobId, out _);
                _pauseHandles.TryRemove(jobId, out _);
            }
        });

        return Task.FromResult(new SimulationJobStartedDto { JobId = jobId, Kind = command.Kind, StartedAt = startedAt });
    }

    public Task<bool> StopSimulationAsync(Guid jobId, CancellationToken ct = default)
    {
        if (!_jobs.TryGetValue(jobId, out var status) || status.State != SandboxStatus.InProgress)
            return Task.FromResult(false);

        if (_jobCts.TryGetValue(jobId, out var cts))
            cts.Cancel();

        // Unblock any paused simulation
        if (_pauseHandles.TryGetValue(jobId, out var sem) && sem.CurrentCount == 0)
            sem.Release();

        status.State       = SandboxStatus.Failed;
        status.CompletedAt = DateTime.UtcNow;
        status.ErrorMessage = "Stopped by user.";
        return Task.FromResult(true);
    }

    public Task<bool> PauseSimulationAsync(Guid jobId, CancellationToken ct = default)
    {
        if (!_jobs.TryGetValue(jobId, out var status) || status.State != SandboxStatus.InProgress)
            return Task.FromResult(false);

        if (_pauseHandles.TryGetValue(jobId, out var sem) && sem.CurrentCount > 0)
            sem.Wait(0); // take slot → pauses the simulation loop

        return Task.FromResult(true);
    }

    public Task<bool> ResumeSimulationAsync(Guid jobId, CancellationToken ct = default)
    {
        if (!_jobs.TryGetValue(jobId, out var status) || status.State != SandboxStatus.InProgress)
            return Task.FromResult(false);

        if (_pauseHandles.TryGetValue(jobId, out var sem) && sem.CurrentCount == 0)
            sem.Release();

        return Task.FromResult(true);
    }

    /// <summary>Returns a snapshot of all job statuses (used by query services).</summary>
    internal IReadOnlyList<SimulationJobStatusDto> GetJobStatuses()
        => _jobs.Values.ToList();

    // ── Private helpers ──────────────────────────────────────────────────────

    private Func<IStandardExecutor> BuildExecutorCreator(
        SimulationKind kind, ModelType algorithm, IExecutorFactory baseFactory)
    {
        if (kind != SimulationKind.TrainedAI)
            return baseFactory.CreateStandardExecutor;

        if (algorithm != ModelType.PPO)
            throw new NotImplementedException($"Trained AI simulation for '{algorithm}' is not yet implemented.");

        string algoFolder = Path.Combine(
            _fileSourceConfig.Value.FileStorage.BasePath,
            _fileSourceConfig.Value.FileStorage.TrainedAlgorithms,
            algorithm.ToString());

        string modelPath = Directory.Exists(algoFolder)
            ? Directory.EnumerateDirectories(algoFolder)
                .SelectMany(expDir => new[] { Path.Combine(expDir, "model.zip") })
                .Where(File.Exists)
                .OrderByDescending(File.GetLastWriteTime)
                .FirstOrDefault() ?? string.Empty
            : string.Empty;

        if (string.IsNullOrEmpty(modelPath))
            throw new InvalidOperationException(
                $"No trained PPO model found under '{algoFolder}'. Train a model first.");

        var aiConfig = new AiConfiguration
        {
            ModelType  = algorithm,
            Version    = "1.0",
            PolicyType = AiPolicy.MLP
        };

        return () => baseFactory.CreateInferenceExecutor(_policyTrainerClient, modelPath, aiConfig);
    }

    private static SimulationStartupSettings BuildSimulationStartupSettings(IncrementalSweeperDto? sweep)
    {
        return new SimulationStartupSettings
        {
            PolicyType              = string.Empty,
            ExecutionMode           = string.Empty,
            IncrementalProperties   = new SimulationIncrementalPropertiesSettings
            {
                SimulationCount = sweep?.SimulationCount ?? 1,
                Properties      = sweep?.Properties ?? [],
            },
        };
    }
}
