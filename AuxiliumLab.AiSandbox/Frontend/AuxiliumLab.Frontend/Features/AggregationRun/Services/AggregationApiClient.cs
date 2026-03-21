using AuxiliumLab.Frontend.Http;

namespace AuxiliumLab.Frontend.Features.AggregationRun.Services;

public interface IAggregationApiClient
{
    Task<AggregationJobStartedDto?> StartAggregationAsync(StartAggregationCommand command, CancellationToken ct = default);
    Task<List<AggregationJobStatusDto>> GetAggregationStatusesAsync(CancellationToken ct = default);
    Task<bool> StopAggregationAsync(Guid jobId, CancellationToken ct = default);
}

public sealed class AggregationApiClient : ApiClientBase, IAggregationApiClient
{
    public AggregationApiClient(HttpClient http) : base(http) { }

    public Task<AggregationJobStartedDto?> StartAggregationAsync(StartAggregationCommand command, CancellationToken ct = default)
        => PostAsync<AggregationJobStartedDto>("ai-sandbox/aggregation/run", command, ct);

    public async Task<List<AggregationJobStatusDto>> GetAggregationStatusesAsync(CancellationToken ct = default)
        => await GetAsync<List<AggregationJobStatusDto>>("ai-sandbox/aggregation/status", ct) ?? [];

    public Task<bool> StopAggregationAsync(Guid jobId, CancellationToken ct = default)
        => PostVoidAsync($"ai-sandbox/aggregation/{jobId}/stop", ct);
}
