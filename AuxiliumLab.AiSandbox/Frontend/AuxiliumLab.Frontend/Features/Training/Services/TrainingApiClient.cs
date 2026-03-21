using AuxiliumLab.Frontend.Http;

namespace AuxiliumLab.Frontend.Features.Training.Services;

public interface ITrainingApiClient
{
    Task<TrainingJobStartedDto?> StartPpoTrainingAsync(StartPpoTrainingCommand command, CancellationToken ct = default);
    Task<List<TrainingJobStatusDto>> GetTrainingStatusesAsync(CancellationToken ct = default);
    Task<List<TrainedModelInfoDto>> GetTrainedModelsAsync(CancellationToken ct = default);
    Task<bool> StopTrainingAsync(Guid jobId, CancellationToken ct = default);
}

public sealed class TrainingApiClient : ApiClientBase, ITrainingApiClient
{
    public TrainingApiClient(HttpClient http) : base(http) { }

    public Task<TrainingJobStartedDto?> StartPpoTrainingAsync(StartPpoTrainingCommand command, CancellationToken ct = default)
        => PostAsync<TrainingJobStartedDto>("ai-sandbox/training/ppo", command, ct);

    public async Task<List<TrainingJobStatusDto>> GetTrainingStatusesAsync(CancellationToken ct = default)
        => await GetAsync<List<TrainingJobStatusDto>>("ai-sandbox/training/status", ct) ?? [];

    public async Task<List<TrainedModelInfoDto>> GetTrainedModelsAsync(CancellationToken ct = default)
        => await GetAsync<List<TrainedModelInfoDto>>("ai-sandbox/training/models", ct) ?? [];

    public Task<bool> StopTrainingAsync(Guid jobId, CancellationToken ct = default)
        => PostVoidAsync($"ai-sandbox/training/{jobId}/stop", ct);
}
