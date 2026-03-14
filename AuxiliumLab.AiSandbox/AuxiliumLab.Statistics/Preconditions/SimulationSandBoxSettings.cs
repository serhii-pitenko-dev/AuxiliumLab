namespace AuxiliumLab.AiSandbox.Statistics.Preconditions;

/// <summary>
/// Captures the sandbox configuration relevant to a mass run simulation.
/// Mirrors the <c>SandBox</c> configuration block without
/// <c>TotalRuns</c>, <c>SaveToFileRegularity</c>, <c>TurnTimeout</c>, <c>FileSource</c> and <c>Type</c>.
/// All incremental range properties are flattened to <see cref="RangeSettings"/>.
/// </summary>
public class SimulationSandBoxSettings
{
    // ── Top-level ──────────────────────────────────────────────────────────────
    public RangeSettings MaxTurns { get; set; } = new(0, 0, 0, 0);

    // ── Map size ───────────────────────────────────────────────────────────────
    public RangeSettings MapWidth { get; set; } = new(0, 0, 0, 0);
    public RangeSettings MapHeight { get; set; } = new(0, 0, 0, 0);

    /// <summary>Whether Width and Height are swept jointly instead of independently.</summary>
    public bool IncrementalAreaEnabled { get; set; }

    /// <summary>Shared step applied to both Width and Height per joint iteration.</summary>
    public int IncrementalAreaStep { get; set; }

    // ── Map contents ──────────────────────────────────────────────────────────
    public RangeSettings BlocksPercent { get; set; } = new(0, 0, 0, 0);
    public RangeSettings PercentOfEnemies { get; set; } = new(0, 0, 0, 0);

    // ── Hero ──────────────────────────────────────────────────────────────────
    public RangeSettings HeroSpeed { get; set; } = new(0, 0, 0, 0);
    public RangeSettings HeroSightRange { get; set; } = new(0, 0, 0, 0);
    public RangeSettings HeroStamina { get; set; } = new(0, 0, 0, 0);

    // ── Enemy ─────────────────────────────────────────────────────────────────
    public RangeSettings EnemySpeed { get; set; } = new(0, 0, 0, 0);
    public RangeSettings EnemySightRange { get; set; } = new(0, 0, 0, 0);
    public RangeSettings EnemyStamina { get; set; } = new(0, 0, 0, 0);
}
