namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;

public struct Size
{
    public IncrementalRange Width { get; set; }
    public IncrementalRange Height { get; set; }

    /// <summary>
    /// When enabled, Width and Height are swept jointly instead of independently.
    /// Individual <see cref="IncrementalPropertyNames.MapWidth"/> and
    /// <see cref="IncrementalPropertyNames.MapHeight"/> sweeps are skipped.
    /// </summary>
    public IncrementalArea IncrementalArea { get; set; }
}