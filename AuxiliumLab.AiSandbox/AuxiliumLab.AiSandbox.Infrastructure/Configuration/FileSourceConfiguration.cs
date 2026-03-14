namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration;

/// <summary>
/// Top-level file-system configuration section ("FileSource" in appsettings.json).
/// Replaces the old nested SandBox → MapSettings → FileSource block.
/// </summary>
public class FileSourceConfiguration
{
    public const string SectionName = "FileSource";

    /// <summary>Settings for optional loading of a pre-created map from disk.</summary>
    public PrecreatedMapSettings PrecreatedMap { get; set; } = new();

    /// <summary>Root paths for the hierarchical file storage.</summary>
    public FileStorageSettings FileStorage { get; set; } = new();
}
