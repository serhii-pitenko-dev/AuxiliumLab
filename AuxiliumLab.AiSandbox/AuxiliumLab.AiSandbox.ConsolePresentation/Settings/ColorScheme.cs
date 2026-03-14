namespace AuxiliumLab.AiSandbox.ConsolePresentation.Settings;

public class ColorScheme
{
    public string BorderColor { get; set; } = string.Empty;
    public string GlobalBackGroundColor { get; set; } = string.Empty;
    public string MapBackGroundColor { get; set; } = string.Empty;
    public string HeroColor { get; set; } = string.Empty;
    public string EnemyColor { get; set; } = string.Empty;
    public string BlockColor { get; set; } = string.Empty;
    /// <summary>Colour used to render border blocks that line the map perimeter.</summary>
    public string BorderBlockColor { get; set; } = string.Empty;
    public string HeroPathColor { get; set; } = string.Empty;
    public string EnemyPathColor { get; set; } = string.Empty;
    public string HeroVisionColor { get; set; } = string.Empty;
    public string EnemyVisionColor { get; set; } = string.Empty;
}