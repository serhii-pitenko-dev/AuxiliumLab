namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;

/// <summary>
/// When <see cref="IsEnabled"/> is <see langword="true"/>, Width and Height are swept together
/// in a single joint sequence instead of being swept independently.
/// <para>
/// The number of iterations is determined by the larger of
/// <c>(Width.Max − Width.Min)</c> and <c>(Height.Max − Height.Min)</c> divided by <see cref="Step"/>.
/// Both dimensions are incremented by <see cref="Step"/> each iteration (clamped to their own Max).
/// Individual Width and Height sweeps are skipped while this is active.
/// </para>
/// </summary>
public class IncrementalArea
{
    public bool IsEnabled { get; set; }

    /// <summary>Shared step applied to both Width and Height per iteration.</summary>
    public int Step { get; set; }
}
