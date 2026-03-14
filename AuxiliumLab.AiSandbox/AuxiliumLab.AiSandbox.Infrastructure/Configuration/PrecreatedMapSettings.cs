namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration;

/// <summary>
/// Controls whether a pre-created playground layout is loaded from disk
/// instead of being generated procedurally.
/// </summary>
public class PrecreatedMapSettings
{
    /// <summary>When true, loads the playground identified by <see cref="PlaygroundId"/> from storage.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Identifier of the playground file to load (e.g. a GUID string).</summary>
    public string PlaygroundId { get; set; } = string.Empty;
}
