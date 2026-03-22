namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;

public class SandBoxConfiguration
{
    public MapConfiguration MapSettings { get; set; }
    public HeroConfiguration Hero { get; set; }
    public EnemyConfiguration Enemy { get; set; }
    public int TurnTimeout { get; set; }
    public IncrementalRange MaxTurns { get; set; } = new IncrementalRange();
    public int SaveToFileRegularity { get; set; }
    public bool IsDebugMode { get; set; }

    /// <summary>
    /// Creates a <see cref="SandBoxConfiguration"/> from flat parameter values.
    /// Each value becomes the <see cref="IncrementalRange.Current"/> (with Min=Max=Current, Step=1).
    /// </summary>
    public static SandBoxConfiguration CreateFromValues(
        int maxTurns, int mapWidth, int mapHeight,
        double blocksPercent, double enemiesPercent,
        int heroSpeed, int heroSightRange, int heroStamina, int enemySpeed)
    {
        return new SandBoxConfiguration
        {
            MaxTurns = new IncrementalRange { Min = maxTurns, Current = maxTurns, Max = maxTurns, Step = 1 },
            MapSettings = new MapConfiguration
            {
                Size = new Size
                {
                    Width = new IncrementalRange { Min = mapWidth, Current = mapWidth, Max = mapWidth, Step = 1 },
                    Height = new IncrementalRange { Min = mapHeight, Current = mapHeight, Max = mapHeight, Step = 1 },
                },
                ElementsPercentages = new ElementsPercentages
                {
                    BlocksPercent = new IncrementalRange { Min = (int)blocksPercent, Current = (int)blocksPercent, Max = (int)blocksPercent, Step = 1 },
                    PercentOfEnemies = new IncrementalRange { Min = (int)enemiesPercent, Current = (int)enemiesPercent, Max = (int)enemiesPercent, Step = 1 },
                },
            },
            Hero = new HeroConfiguration
            {
                Speed = new IncrementalRange { Min = heroSpeed, Current = heroSpeed, Max = heroSpeed, Step = 1 },
                SightRange = new IncrementalRange { Min = heroSightRange, Current = heroSightRange, Max = heroSightRange, Step = 1 },
                Stamina = new IncrementalRange { Min = heroStamina, Current = heroStamina, Max = heroStamina, Step = 1 },
            },
            Enemy = new EnemyConfiguration
            {
                Speed = new IncrementalRange { Min = enemySpeed, Current = enemySpeed, Max = enemySpeed, Step = 1 },
            },
        };
    }
}
