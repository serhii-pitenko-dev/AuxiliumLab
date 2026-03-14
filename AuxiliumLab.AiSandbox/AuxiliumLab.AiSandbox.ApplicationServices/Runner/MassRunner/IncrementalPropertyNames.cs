namespace AuxiliumLab.AiSandbox.ApplicationServices.Runner.MassRunner;

/// <summary>
/// Well-known property name constants used to select which <see cref="AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions.IncrementalRange"/>
/// should be swept during an incremental batch run.
/// </summary>
public static class IncrementalPropertyNames
{
    // ── Top-level sandbox ──────────────────────────────────────────────────────
    public const string MaxTurns = "MaxTurns";

    // ── Map size ───────────────────────────────────────────────────────────────
    public const string MapWidth = "MapWidth";
    public const string MapHeight = "MapHeight";

    // ── Map contents ──────────────────────────────────────────────────────────
    public const string BlocksPercent = "BlocksPercent";
    public const string PercentOfEnemies = "PercentOfEnemies";

    // ── Hero ──────────────────────────────────────────────────────────────────
    public const string HeroSpeed = "HeroSpeed";
    public const string HeroSightRange = "HeroSightRange";
    public const string HeroStamina = "HeroStamina";

    // ── Enemy ─────────────────────────────────────────────────────────────────
    public const string EnemySpeed = "EnemySpeed";
    public const string EnemySightRange = "EnemySightRange";
    public const string EnemyStamina = "EnemyStamina";
}
