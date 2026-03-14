namespace AuxiliumLab.Frontend.Services;

/// <summary>Describes a single navigation item in the top menu.</summary>
public class MenuItemConfig
{
    public string Label  { get; set; } = string.Empty;
    public string? Href  { get; set; }
    public string? Icon  { get; set; }

    /// <summary>Supported context strings for conditional display, e.g. "ai-sandbox".</summary>
    public List<string> Contexts { get; set; } = [];

    public List<MenuItemConfig> Children { get; set; } = [];
}

/// <summary>Top-level menu section.</summary>
public class MenuSectionConfig
{
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public List<MenuItemConfig> Items { get; set; } = [];
}

/// <summary>Root menu configuration loaded from wwwroot/menu-config.json.</summary>
public class MenuConfig
{
    public List<MenuSectionConfig> Sections { get; set; } = [];
}
