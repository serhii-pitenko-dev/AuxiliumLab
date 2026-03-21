using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Training;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Training;
using AuxiliumLab.AiSandbox.ApplicationServices.Trainer;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Jobs.Training;

/// <summary>
/// Singleton service that implements both <see cref="ITrainingCommands"/> and
/// <see cref="ITrainingQueries"/>. Launches training on background threads and
/// tracks job state in memory.
/// </summary>
public sealed class TrainingJobService : ITrainingCommands, ITrainingQueries
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TrainingSettings _trainingSettings;
    private readonly Sb3AlgorithmTypeProvider _algorithmTypeProvider;
    private readonly IPolicyTrainerClient _policyTrainerClient;
    private readonly GymBrokerRegistry _gymBrokerRegistry;
    private readonly IOptions<FileSourceConfiguration> _fileSourceConfig;

    private readonly ConcurrentDictionary<Guid, TrainingJobStatusDto> _jobs = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobCts = new();

    public TrainingJobService(
        IServiceProvider serviceProvider,
        TrainingSettings trainingSettings,
        Sb3AlgorithmTypeProvider algorithmTypeProvider,
        IPolicyTrainerClient policyTrainerClient,
        GymBrokerRegistry gymBrokerRegistry,
        IOptions<FileSourceConfiguration> fileSourceConfig)
    {
        _serviceProvider       = serviceProvider;
        _trainingSettings      = trainingSettings;
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

    // ── ITrainingQueries ─────────────────────────────────────────────────────

    public Task<IReadOnlyList<TrainingJobStatusDto>> GetTrainingStatusesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TrainingJobStatusDto>>(_jobs.Values.ToList());

    public Task<IReadOnlyList<TrainedModelInfoDto>> GetTrainedModelsAsync(CancellationToken ct = default)
    {
        var basePath = _fileSourceConfig.Value.FileStorage.BasePath;
        var trainedFolder = _fileSourceConfig.Value.FileStorage.TrainedAlgorithms;
        var root = Path.Combine(basePath, trainedFolder);
        var models = new List<TrainedModelInfoDto>();

        if (!Directory.Exists(root))
            return Task.FromResult<IReadOnlyList<TrainedModelInfoDto>>(models);

        foreach (var algoDir in Directory.EnumerateDirectories(root))
        {
            string algorithm = Path.GetFileName(algoDir);
            foreach (var expDir in Directory.EnumerateDirectories(algoDir))
            {
                string experimentId = Path.GetFileName(expDir);
                string modelFile    = Path.Combine(expDir, "model.zip");
                if (!File.Exists(modelFile))
                    continue;

                TrainingPreconditionsDto? preconditions = null;
                string preconditionsFile = Path.Combine(expDir, "preconditions.json");
                if (File.Exists(preconditionsFile))
                {
                    try
                    {
                        string json = File.ReadAllText(preconditionsFile);
                        preconditions = JsonSerializer.Deserialize<TrainingPreconditionsDto>(json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch { /* corrupt file — skip */ }
                }

                models.Add(new TrainedModelInfoDto
                {
                    Algorithm    = algorithm,
                    ExperimentId = experimentId,
                    ModelFilePath = modelFile,
                    TrainedAt    = File.GetLastWriteTime(modelFile),
                    Preconditions = preconditions
                });
            }
        }

        return Task.FromResult<IReadOnlyList<TrainedModelInfoDto>>(
            models.OrderByDescending(m => m.TrainedAt).ToList());
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private Task<TrainingJobStartedDto> StartTrainingBackgroundAsync(
        ModelType algorithmType,
        StartPpoTrainingCommand? overrides)
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
                    _trainingSettings,
                    _algorithmTypeProvider,
                    _policyTrainerClient,
                    _gymBrokerRegistry);

                var info = await runner.RunTrainingAsync(algorithmType, overrides);

                status.State        = TrainingJobState.Completed;
                status.CompletedAt  = DateTime.UtcNow;
                status.ExperimentId = info.ExperimentId;
            }
            catch (OperationCanceledException)
            {
                status.State        = TrainingJobState.Failed;
                status.CompletedAt  = DateTime.UtcNow;
                status.ErrorMessage = "Stopped by user.";
            }
            catch (Exception ex)
            {
                status.State        = TrainingJobState.Failed;
                status.CompletedAt  = DateTime.UtcNow;
                status.ErrorMessage = ex.Message;
            }
            finally
            {
                _jobCts.TryRemove(jobId, out _);
            }
        });

        return Task.FromResult(new TrainingJobStartedDto { JobId = jobId, Algorithm = algoName, ExperimentId = experimentId, StartedAt = startedAt });
    }

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
}
