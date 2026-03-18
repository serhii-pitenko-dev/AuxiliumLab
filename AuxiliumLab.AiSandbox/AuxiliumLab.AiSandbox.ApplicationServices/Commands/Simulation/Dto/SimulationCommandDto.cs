using AuxiliumLab.AiSandbox.Ai.Configuration;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Dto;

/// <summary>Simulation type to run.</summary>
public enum SimulationKind
{
    RandomAI,
    TrainedAI
}

/// <summary>
/// Sandbox configuration overrides for a simulation run.
/// Null fields use appsettings.json SandBox defaults.
/// </summary>
public class SimulationSandboxOverrideDto
{
    public int? MaxTurns { get; set; }
    public int? MapWidth { get; set; }
    public int? MapHeight { get; set; }
    public double? BlocksPercent { get; set; }
    public double? EnemiesPercent { get; set; }
    public int? HeroSpeed { get; set; }
    public int? HeroSightRange { get; set; }
    public int? HeroStamina { get; set; }
    public int? EnemySpeed { get; set; }
}

/// <summary>Command to start a single simulation run.</summary>
public class StartSingleSimulationCommand
{
    public SimulationKind Kind { get; set; } = SimulationKind.RandomAI;

    /// <summary>
    /// Algorithm for trained AI simulation (PPO supported; others throw NotImplementedException).
    /// Ignored for RandomAI.
    /// </summary>
    public ModelType Algorithm { get; set; } = ModelType.PPO;

    /// <inheritdoc cref="SimulationSandboxOverrideDto"/>
    public SimulationSandboxOverrideDto? SandboxSettings { get; set; }

    /// <summary>
    /// Delay in milliseconds applied between each agent action during presentation runs.
    /// 0 means no delay.
    /// </summary>
    public int ActionDelayMs { get; set; } = 0;
}

/// <summary>Command to start a mass (batch) simulation run.</summary>
public class StartMassSimulationCommand
{
    public SimulationKind Kind { get; set; } = SimulationKind.RandomAI;

    /// <summary>Number of standard (baseline) parallel runs.</summary>
    public int SimulationCount { get; set; } = 100;

    /// <summary>
    /// Algorithm for trained AI simulation (PPO supported; others throw NotImplementedException).
    /// Ignored for RandomAI.
    /// </summary>
    public ModelType Algorithm { get; set; } = ModelType.PPO;

    /// <inheritdoc cref="SimulationSandboxOverrideDto"/>
    public SimulationSandboxOverrideDto? SandboxSettings { get; set; }

    /// <summary>Optional incremental sweep settings.</summary>
    public IncrementalSweeperDto? IncrementalSweep { get; set; }
}

/// <summary>Incremental property sweep settings for mass runs.</summary>
public class IncrementalSweeperDto
{
    public int SimulationCount { get; set; } = 1;
    public List<string> Properties { get; set; } = [];
}

/// <summary>Returned immediately when a simulation job is accepted.</summary>
public record SimulationJobStartedDto(
    Guid JobId,
    SimulationKind Kind,
    DateTime StartedAt);
