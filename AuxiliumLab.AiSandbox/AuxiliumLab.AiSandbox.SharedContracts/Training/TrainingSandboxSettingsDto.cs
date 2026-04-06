namespace AuxiliumLab.AiSandbox.SharedContracts;

public class TrainingSandboxSettingsDto
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
}
