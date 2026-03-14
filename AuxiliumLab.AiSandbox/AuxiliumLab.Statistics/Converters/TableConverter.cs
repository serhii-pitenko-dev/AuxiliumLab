using AuxiliumLab.AiSandbox.Domain.Statistics.Result;
using AuxiliumLab.AiSandbox.Statistics.Preconditions;
using System.Text;

namespace AuxiliumLab.AiSandbox.Statistics.Converters;

/// <summary>
/// Converts simulation precondition and result objects to CSV-formatted strings
/// suitable for appending to a statistics file (e.g. for import into Google Sheets).
/// </summary>
public static class TableConverter
{
    // ── Public conversion methods ──────────────────────────────────────────────

    /// <summary>
    /// Converts <see cref="SimulationStartupSettings"/> to a CSV table where
    /// each row is a property name and the single column contains its value.
    /// </summary>
    public static string ToCsv(SimulationStartupSettings settings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Startup Settings");
        sb.AppendLine("Property,Value");
        sb.AppendLine($"PolicyType,{Escape(settings.PolicyType)}");
        sb.AppendLine($"ExecutionMode,{Escape(settings.ExecutionMode)}");
        sb.AppendLine($"StandardSimulationCount,{settings.StandardSimulationCount}");
        sb.AppendLine($"IncrementalSimulationCount,{settings.IncrementalProperties.SimulationCount}");
        sb.AppendLine($"IncrementalProperties,{Escape(string.Join(";", settings.IncrementalProperties.Properties))}");
        return sb.ToString();
    }

    /// <summary>
    /// Converts <see cref="SimulationSandBoxSettings"/> to a CSV table where
    /// rows are property names and columns are Min, Current, Max, Step.
    /// Boolean / scalar properties have only the Current column populated.
    /// </summary>
    public static string ToCsv(SimulationSandBoxSettings settings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# SandBox Settings");
        sb.AppendLine("Property,Min,Current,Max,Step");

        AppendRange(sb, "MaxTurns",          settings.MaxTurns);
        AppendRange(sb, "MapWidth",          settings.MapWidth);
        AppendRange(sb, "MapHeight",         settings.MapHeight);
        AppendRange(sb, "BlocksPercent",     settings.BlocksPercent);
        AppendRange(sb, "PercentOfEnemies",  settings.PercentOfEnemies);
        AppendRange(sb, "HeroSpeed",         settings.HeroSpeed);
        AppendRange(sb, "HeroSightRange",    settings.HeroSightRange);
        AppendRange(sb, "HeroStamina",       settings.HeroStamina);
        AppendRange(sb, "EnemySpeed",        settings.EnemySpeed);
        AppendRange(sb, "EnemySightRange",   settings.EnemySightRange);
        AppendRange(sb, "EnemyStamina",      settings.EnemyStamina);
        AppendScalar(sb, "IncrementalAreaEnabled", settings.IncrementalAreaEnabled.ToString());
        AppendScalar(sb, "IncrementalAreaStep",    settings.IncrementalAreaStep.ToString());

        return sb.ToString();
    }

    /// <summary>
    /// Converts a single <see cref="BatchSummary"/> (standard run) to a CSV property/value table.
    /// </summary>
    public static string ToCsv(BatchSummary batch)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Standard Run");
        sb.AppendLine("Property,Value");
        sb.AppendLine($"Id,{Escape(batch.Id.ToString())}");
        sb.AppendLine($"Number,{batch.Number}");
        sb.AppendLine($"TotalRuns,{batch.TotalRuns}");
        sb.AppendLine($"Wins,{batch.Wins}");
        sb.AppendLine($"Losses,{batch.Losses}");
        sb.AppendLine($"AverageTurns,{batch.AverageTurns:F2}");
        sb.AppendLine($"MaxTurns,{batch.MaxTurns}");
        sb.AppendLine($"ExecutionTime,{Escape(batch.ExecutionTime.ToString(@"hh\:mm\:ss\.fff"))}");
        return sb.ToString();
    }

    /// <summary>
    /// Converts an <see cref="IncrementalRunSummary"/> to its CSV section.
    /// First block: key/value metadata rows; second block: transposed batch table.
    /// </summary>
    public static string ToCsv(IncrementalRunSummary run)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Incremental Run {run.Number}");
        sb.AppendLine($"Number,{run.Number}");
        sb.AppendLine($"Id,{Escape(run.Id.ToString())}");
        sb.AppendLine($"Description,{Escape(run.Description)}");
        sb.AppendLine($"Property name,{Escape(run.Property)}");
        sb.AppendLine($"Execution time,{Escape(run.ExecutionTime.ToString(@"hh\:mm\:ss\.fff"))}");
        sb.AppendLine($"BatchRunCount,{run.BatchRunCount}");
        sb.AppendLine($"Min,{Escape(run.Min)}");
        sb.AppendLine($"Step,{Escape(run.Step)}");
        sb.AppendLine($"Max,{Escape(run.Max)}");
        sb.AppendLine();

        if (run.Batches.Count == 0)
        {
            sb.AppendLine("(no batch data)");
            return sb.ToString();
        }

        // Header: label column + one column per batch (numbered by batch.Number)
        var header = new List<string> { "Step" };
        header.AddRange(run.Batches.Select(b => b.Number.ToString()));
        sb.AppendLine(string.Join(",", header));

        // Transposed batch table rows
        AppendBatchRow(sb, "Id",              run.Batches, b => Escape(b.Id.ToString()));
        AppendBatchRow(sb, "Wins",            run.Batches, b => b.Wins.ToString());
        AppendBatchRow(sb, "Losses",          run.Batches, b => b.Losses.ToString());
        AppendBatchRow(sb, "AverageTurns",    run.Batches, b => b.AverageTurns.ToString("F2"));
        AppendBatchRow(sb, "MaxTurns",        run.Batches, b => b.MaxTurns.ToString());
        AppendBatchRow(sb, "ExecutionTime",   run.Batches, b => Escape(b.ExecutionTime.ToString(@"hh\:mm\:ss\.fff")));
        AppendBatchRow(sb, "AvgTurnExecTime", run.Batches, b =>
            b.AverageTurns > 0
                ? $"{b.ExecutionTime.TotalMilliseconds / b.AverageTurns:F3}ms"
                : "N/A");

        return sb.ToString();
    }

    /// <summary>
    /// Converts a <see cref="MassRunSummary"/> to a CSV property/value table.
    /// </summary>
    public static string ToCsv(MassRunSummary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Mass Run Summary");
        sb.AppendLine("Property,Value");
        sb.AppendLine($"BatchesCount,{summary.BatchesCount}");
        sb.AppendLine($"TimeExecution,{Escape(summary.TimeExecution.ToString(@"hh\:mm\:ss\.fff"))}");
        sb.AppendLine($"Description,{Escape(summary.Description)}");
        return sb.ToString();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static void AppendRange(StringBuilder sb, string name, RangeSettings range)
        => sb.AppendLine($"{Escape(name)},{range.Min},{range.Current},{range.Max},{range.Step}");

    private static void AppendScalar(StringBuilder sb, string name, string value)
        => sb.AppendLine($"{Escape(name)},,{Escape(value)},,");

    private static void AppendBatchRow(
        StringBuilder sb,
        string rowLabel,
        IReadOnlyList<BatchSummary> batches,
        Func<BatchSummary, string> selector)
    {
        var parts = new List<string> { Escape(rowLabel) };
        parts.AddRange(batches.Select(selector));
        sb.AppendLine(string.Join(",", parts));
    }

    /// <summary>
    /// Escapes a CSV field: wraps in double-quotes if the value contains
    /// a comma, double-quote, or newline; doubles any embedded double-quotes.
    /// </summary>
    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
