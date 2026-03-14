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
}
