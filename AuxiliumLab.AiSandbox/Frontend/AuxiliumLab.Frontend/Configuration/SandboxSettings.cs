namespace AuxiliumLab.Frontend.Configuration;

/// <summary>Mirrors the "SandBox" section in wwwroot/appsettings.json.</summary>
public class SandboxSettings
{
    public RangedValue    MaxTurns    { get; set; } = new();
    public MapSettingsConfig MapSettings { get; set; } = new();
    public AgentConfig    Hero        { get; set; } = new();
    public AgentConfig    Enemy       { get; set; } = new();
}

public class RangedValue
{
    public int Min     { get; set; }
    public int Current { get; set; }
    public int Max     { get; set; }
    public int Step    { get; set; }
}

public class MapSettingsConfig
{
    public MapSizeConfig             Size                { get; set; } = new();
    public ElementsPercentagesConfig ElementsPercentages { get; set; } = new();
}

public class MapSizeConfig
{
    public RangedValue Width  { get; set; } = new();
    public RangedValue Height { get; set; } = new();
}

public class ElementsPercentagesConfig
{
    public RangedValue BlocksPercent    { get; set; } = new();
    public RangedValue PercentOfEnemies { get; set; } = new();
}

public class AgentConfig
{
    public RangedValue Speed      { get; set; } = new();
    public RangedValue SightRange { get; set; } = new();
    public RangedValue Stamina    { get; set; } = new();
}
