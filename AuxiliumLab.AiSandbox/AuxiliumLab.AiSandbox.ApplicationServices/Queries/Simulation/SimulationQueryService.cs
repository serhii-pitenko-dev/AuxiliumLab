using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using Microsoft.Extensions.Options;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation;

/// <summary>
/// Read-only query service for simulation data. Reads job statuses from
/// <see cref="SimulationCommandService"/> and sandbox defaults from configuration.
/// </summary>
public sealed class SimulationQueryService : ISimulationQueries
{
    private readonly SimulationCommandService _commandService;
    private readonly IOptions<SandBoxConfiguration> _sandboxConfig;

    public SimulationQueryService(
        SimulationCommandService commandService,
        IOptions<SandBoxConfiguration> sandboxConfig)
    {
        _commandService = commandService;
        _sandboxConfig  = sandboxConfig;
    }

    public Task<IReadOnlyList<SimulationJobStatusDto>> GetSimulationStatusesAsync(CancellationToken ct = default)
        => Task.FromResult(_commandService.GetJobStatuses());

    public Task<SandboxDefaultsDto> GetSandboxDefaultsAsync(CancellationToken ct = default)
    {
        var cfg = _sandboxConfig.Value;
        return Task.FromResult(new SandboxDefaultsDto
        {
            MaxTurns       = cfg.MaxTurns.Current,
            MapWidth       = cfg.MapSettings.Size.Width.Current,
            MapHeight      = cfg.MapSettings.Size.Height.Current,
            BlocksPercent  = cfg.MapSettings.ElementsPercentages.BlocksPercent.Current,
            EnemiesPercent = cfg.MapSettings.ElementsPercentages.PercentOfEnemies.Current,
            HeroSpeed      = cfg.Hero.Speed.Current,
            HeroSightRange = cfg.Hero.SightRange.Current,
            HeroStamina    = cfg.Hero.Stamina.Current,
            EnemySpeed     = cfg.Enemy.Speed.Current
        });
    }
}
