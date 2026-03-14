using AuxiliumLab.Frontend.Features.Simulation.Dto;
using AuxiliumLab.Frontend.Http;

namespace AuxiliumLab.Frontend.Features.Simulation.Services;

public interface ISimulationApiClient
{
    Task<SimulationJobStartedDto?> StartSingleSimulationAsync(StartSingleSimulationCommand command, CancellationToken ct = default);
    Task<SimulationJobStartedDto?> StartMassSimulationAsync(StartMassSimulationCommand command, CancellationToken ct = default);
    Task<List<SimulationJobStatusDto>> GetSimulationStatusesAsync(CancellationToken ct = default);
    Task<bool> StopSimulationAsync(Guid jobId, CancellationToken ct = default);
    Task<bool> PauseSimulationAsync(Guid jobId, CancellationToken ct = default);
    Task<bool> ResumeSimulationAsync(Guid jobId, CancellationToken ct = default);
}

public sealed class SimulationApiClient : ApiClientBase, ISimulationApiClient
{
    public SimulationApiClient(HttpClient http) : base(http) { }

    public Task<SimulationJobStartedDto?> StartSingleSimulationAsync(StartSingleSimulationCommand command, CancellationToken ct = default)
        => PostAsync<SimulationJobStartedDto>("ai-sandbox/simulation/run/single", command, ct);

    public Task<SimulationJobStartedDto?> StartMassSimulationAsync(StartMassSimulationCommand command, CancellationToken ct = default)
        => PostAsync<SimulationJobStartedDto>("ai-sandbox/simulation/run/mass", command, ct);

    public async Task<List<SimulationJobStatusDto>> GetSimulationStatusesAsync(CancellationToken ct = default)
        => await GetAsync<List<SimulationJobStatusDto>>("ai-sandbox/simulation/status", ct) ?? [];

    public Task<bool> StopSimulationAsync(Guid jobId, CancellationToken ct = default)
        => PostVoidAsync($"ai-sandbox/simulation/{jobId}/stop", ct);

    public Task<bool> PauseSimulationAsync(Guid jobId, CancellationToken ct = default)
        => PostVoidAsync($"ai-sandbox/simulation/{jobId}/pause", ct);

    public Task<bool> ResumeSimulationAsync(Guid jobId, CancellationToken ct = default)
        => PostVoidAsync($"ai-sandbox/simulation/{jobId}/resume", ct);
}
