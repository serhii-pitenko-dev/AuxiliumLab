namespace AuxiliumLab.AiSandbox.SharedContracts;

public class SimulationSandboxOverrideDto
{
    public int    MaxTurns       { get; set; }
    public int    MapWidth       { get; set; }
    public int    MapHeight      { get; set; }
    public double BlocksPercent  { get; set; }
    public double EnemiesPercent { get; set; }
    public int    HeroSpeed      { get; set; }
    public int    HeroSightRange { get; set; }
    public int    HeroStamina      { get; set; }
    public int    EnemySpeed       { get; set; }
    public int    EnemySightRange  { get; set; }
    public int    EnemyStamina     { get; set; }
    /// <summary>Delay in milliseconds between each agent action during presentation visualization.</summary>
    public int    ActionDelayMs    { get; set; } = 500;
}
