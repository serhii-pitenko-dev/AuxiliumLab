using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Training;
using AuxiliumLab.AiSandbox.ApplicationServices.Trainer;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Commands.Training;

/// <summary>
/// Singleton service that implements <see cref="ITrainingCommands"/>.
/// Launches training on background threads and tracks job state in memory.
/// </summary>
public sealed class TrainingCommandService : ITrainingCommands
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Sb3AlgorithmTypeProvider _algorithmTypeProvider;
    private readonly IPolicyTrainerClient _policyTrainerClient;
    private readonly GymBrokerRegistry _gymBrokerRegistry;
    private readonly IOptions<FileSourceConfiguration> _fileSourceConfig;

    private readonly ConcurrentDictionary<Guid, TrainingJobStatusDto> _jobs = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobCts = new();

    public TrainingCommandService(
        IServiceProvider serviceProvider,
        Sb3AlgorithmTypeProvider algorithmTypeProvider,
        IPolicyTrainerClient policyTrainerClient,
        GymBrokerRegistry gymBrokerRegistry,
        IOptions<FileSourceConfiguration> fileSourceConfig)
    {
        _serviceProvider       = serviceProvider;
        _algorithmTypeProvider = algorithmTypeProvider;
        _policyTrainerClient   = policyTrainerClient;
        _gymBrokerRegistry     = gymBrokerRegistry;
        _fileSourceConfig      = fileSourceConfig;
    }

    // ── ITrainingCommands ────────────────────────────────────────────────────

    public Task<TrainingJobStartedDto> StartPpoTrainingAsync(
        StartPpoTrainingCommand command, CancellationToken ct = default)
        => StartTrainingBackgroundAsync(ModelType.PPO, command);

    public Task<TrainingJobStartedDto> StartA2cTrainingAsync(
        StartGenericTrainingCommand command, CancellationToken ct = default)
        => throw new NotImplementedException("A2C training is not yet implemented.");

    public Task<TrainingJobStartedDto> StartDqnTrainingAsync(
        StartGenericTrainingCommand command, CancellationToken ct = default)
        => throw new NotImplementedException("DQN training is not yet implemented.");

    public Task<TrainingJobStartedDto> StartSacTrainingAsync(
        StartGenericTrainingCommand command, CancellationToken ct = default)
        => throw new NotImplementedException("SAC training is not yet implemented.");

    public Task<TrainingJobStartedDto> StartTd3TrainingAsync(
        StartGenericTrainingCommand command, CancellationToken ct = default)
        => throw new NotImplementedException("TD3 training is not yet implemented.");

    public Task<TrainingJobStartedDto> StartDdpgTrainingAsync(
        StartGenericTrainingCommand command, CancellationToken ct = default)
        => throw new NotImplementedException("DDPG training is not yet implemented.");

    public Task<bool> StopTrainingAsync(Guid jobId, CancellationToken ct = default)
    {
        if (!_jobs.TryGetValue(jobId, out var status) || status.State != TrainingJobState.Running)
            return Task.FromResult(false);

        if (_jobCts.TryGetValue(jobId, out var cts))
            cts.Cancel();

        status.State        = TrainingJobState.Failed;
        status.CompletedAt  = DateTime.UtcNow;
        status.ErrorMessage = "Stopped by user.";
        return Task.FromResult(true);
    }

    /// <summary>Returns a snapshot of all job statuses (used by query services).</summary>
    internal IReadOnlyList<TrainingJobStatusDto> GetJobStatuses()
        => _jobs.Values.ToList();

    // ── Private helpers ──────────────────────────────────────────────────────

    private Task<TrainingJobStartedDto> StartTrainingBackgroundAsync(
        ModelType algorithmType,
        StartPpoTrainingCommand overrides)
    {
        var jobId     = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;

        // Build a placeholder experiment id for the job descriptor.
        // The real experiment id is determined inside TrainingRunner.
        string algoName    = algorithmType.ToString().ToUpper();
        string experimentId = $"{algoName}-{startedAt:yyyyMMdd-HHmmss}";

        var status = new TrainingJobStatusDto
        {
            JobId        = jobId,
            Algorithm    = algoName,
            ExperimentId = experimentId,
            State        = TrainingJobState.Running,
            StartedAt    = startedAt
        };
        _jobs[jobId] = status;

        var cts = new CancellationTokenSource();
        _jobCts[jobId] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                var runner = new TrainingRunner(
                    _serviceProvider,
                    _algorithmTypeProvider,
                    _policyTrainerClient,
                    _gymBrokerRegistry);

                var info = await runner.RunTrainingAsync(algorithmType, overrides, status);

                status.State        = TrainingJobState.Completed;
                status.CompletedAt  = DateTime.UtcNow;
                status.ExperimentId = info.ExperimentId;
            }
            catch (OperationCanceledException)
            {
                status.State        = TrainingJobState.Failed;
                status.CompletedAt  = DateTime.UtcNow;
                status.ErrorMessage = "Stopped by user.";
                await SaveTrainingErrorAsync(status);
            }
            catch (Exception ex)
            {
                status.State        = TrainingJobState.Failed;
                status.CompletedAt  = DateTime.UtcNow;
                status.ErrorMessage = ex.Message;
                await SaveTrainingErrorAsync(status);
            }
            finally
            {
                _jobCts.TryRemove(jobId, out _);
            }
        });

        return Task.FromResult(new TrainingJobStartedDto { JobId = jobId, Algorithm = algoName, ExperimentId = experimentId, StartedAt = startedAt });
    }

    private async Task SaveTrainingErrorAsync(TrainingJobStatusDto status)
    {
        try
        {
            await TrainingRunner.SaveErrorAsync(
                status.Algorithm,
                status.ExperimentId,
                status.ErrorMessage ?? "Unknown error",
                _fileSourceConfig.Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Training] WARNING: Could not save error.json: {ex.Message}");
        }
    }
}
