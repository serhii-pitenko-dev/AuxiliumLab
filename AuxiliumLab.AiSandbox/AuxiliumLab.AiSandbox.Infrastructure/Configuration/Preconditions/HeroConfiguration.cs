namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;

public struct HeroConfiguration
{
    public IncrementalRange Speed { get; set; }
    public IncrementalRange SightRange { get; set; }
    public IncrementalRange Stamina { get; set; }
}