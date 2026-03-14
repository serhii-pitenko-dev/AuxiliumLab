namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;

/// <summary>
/// Describes an integer property that can be swept incrementally between Min and Max by Step.
/// <para>The <see cref="Current"/> value is the standard (default) value used for non-incremental runs.</para>
/// </summary>
public class IncrementalRange
{
    /// <summary>Start of the incremental sweep.</summary>
    public int Min { get; set; }

    /// <summary>Standard value used for normal runs.</summary>
    public int Current { get; set; }

    /// <summary>End of the incremental sweep (inclusive boundary).</summary>
    public int Max { get; set; }

    /// <summary>Increment between steps.</summary>
    public int Step { get; set; }

    /// <summary>Number of incremental steps: (Max - Min) / Step.</summary>
    public int StepCount => Step > 0 ? (Max - Min) / Step : 0;

    /// <summary>Returns a new <see cref="IncrementalRange"/> with <see cref="Current"/> overridden to <paramref name="current"/>.</summary>
    public IncrementalRange WithCurrent(int current) =>
        new IncrementalRange { Min = Min, Current = current, Max = Max, Step = Step };

    /// <summary>Implicit conversion so existing code that reads this as <c>int</c> uses <see cref="Current"/>.</summary>
    public static implicit operator int(IncrementalRange range) => range.Current;
}
