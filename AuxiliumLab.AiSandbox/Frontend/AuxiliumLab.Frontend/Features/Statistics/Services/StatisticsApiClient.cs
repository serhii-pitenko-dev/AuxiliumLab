using AuxiliumLab.Frontend.Http;

namespace AuxiliumLab.Frontend.Features.Statistics.Services;

public interface IStatisticsApiClient
{
    Task<List<CompletedSimulationRunDto>> GetCompletedSimulationsAsync(CancellationToken ct = default);
    Task<List<CompletedAggregationRunDto>> GetCompletedAggregationsAsync(CancellationToken ct = default);
}

public sealed class StatisticsApiClient : ApiClientBase, IStatisticsApiClient
{
    public StatisticsApiClient(HttpClient http) : base(http) { }

    public async Task<List<CompletedSimulationRunDto>> GetCompletedSimulationsAsync(CancellationToken ct = default)
        => await GetAsync<List<CompletedSimulationRunDto>>("ai-sandbox/statistic/simulations", ct) ?? [];

    public async Task<List<CompletedAggregationRunDto>> GetCompletedAggregationsAsync(CancellationToken ct = default)
        => await GetAsync<List<CompletedAggregationRunDto>>("ai-sandbox/statistic/aggregations", ct) ?? [];
}
