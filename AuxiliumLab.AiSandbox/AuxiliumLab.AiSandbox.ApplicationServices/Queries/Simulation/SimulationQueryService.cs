using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation;

/// <summary>
/// Read-only query service for simulation data. Reads job statuses from
/// <see cref="SimulationCommandService"/>.
/// </summary>
public sealed class SimulationQueryService : ISimulationQueries
{
    private readonly SimulationCommandService _commandService;

    public SimulationQueryService(SimulationCommandService commandService)
    {
        _commandService = commandService;
    }

    public Task<IReadOnlyList<SimulationJobStatusDto>> GetSimulationStatusesAsync(CancellationToken ct = default)
        => Task.FromResult(_commandService.GetJobStatuses());
}
