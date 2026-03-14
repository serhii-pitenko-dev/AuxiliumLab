using AuxiliumLab.AiSandbox.Domain.Statistics.Result;
using System.Text;

namespace AuxiliumLab.AiSandbox.Statistics.Converters;

/// <summary>
/// Converts a collection of <see cref="AggregationStepResult"/> into a single CSV report
/// that compares all simulation run types side-by-side in one spreadsheet.
/// </summary>
public static class AggregationReportConverter
{
    /// <summary>
    /// Builds the full aggregation CSV report.
    ///
    /// Structure:
    ///   1. Header    — date, run-type labels.
    ///   2. Training  — algorithm + parameters (when a Training step is present).
    ///   3. Sandbox   — wall of sandbox-setting rows (skipped when no mass run step present).
    ///   4. Standard  — one-row comparison of the baseline (standard) batch per run type.
    ///   5. Sweep(s)  — one section per swept property; rows = sweep steps, columns per run type.
    /// </summary>
    public static string ToCsv(
        IReadOnlyList<AggregationStepResult> steps,
        DateTime runDate)
    {
        var sb = new StringBuilder();

        // ── 1. Heading ────────────────────────────────────────────────────────
        sb.AppendLine("# Aggregation Run Report");
        sb.AppendLine($"Date,{Escape(runDate.ToString("yyyy-MM-dd HH:mm:ss"))}");

        var massSteps    = steps.Where(s => s.MassRunResult is not null).ToList();
        var trainingStep = steps.FirstOrDefault(s => s.IsTraining);

        var runTypeLabels = massSteps.Select(s => s.StepName).ToList();
        sb.AppendLine($"Run Types,{Escape(string.Join(", ", steps.Select(s => s.StepName)))}");
        sb.AppendLine();

        // ── 2. Training info ──────────────────────────────────────────────────
        if (trainingStep?.TrainingInfo is TrainingRunInfo ti)
        {
            sb.AppendLine("# Training Information");
            sb.AppendLine("Property,Value");
            sb.AppendLine($"Algorithm,{Escape(ti.AlgorithmName)}");
            sb.AppendLine($"ExperimentId,{Escape(ti.ExperimentId)}");
            foreach (var (paramName, paramValue) in ti.Parameters)
                sb.AppendLine($"{Escape(paramName)},{Escape(paramValue)}");
            sb.AppendLine();
        }

        if (massSteps.Count == 0)
            return sb.ToString();

        // ── 3. Sandbox settings (taken from any mass step — all use the same config) ──
        // We don't have the sandbox config here directly; the sandbox section is
        // written by MassRunner to its own CSV. We write a note instead.
        sb.AppendLine("# Note: Sandbox configuration is also recorded in each individual MassRunner CSV.");
        sb.AppendLine();

        // ── 4. Standard batch comparison table ────────────────────────────────
        //   Row 1: header row 1 — run type label spanning 3 columns
        //   Row 2: header row 2 — MaxTurns, AvgTurns, Wins per run type
        //   Row 3: data
        sb.AppendLine("# Standard Runs Comparison");
        AppendComparisonTable(sb, massSteps, includeIncrementalValueColumn: false);
        sb.AppendLine();

        // ── 5. Incremental sweep sections ─────────────────────────────────────
        // Collect every unique swept property name across all mass steps.
        var allSweepProperties = massSteps
            .SelectMany(s => s.MassRunResult!.SweepSummaries.Select(sw => sw.Property))
            .Distinct()
            .ToList();

        foreach (var property in allSweepProperties)
        {
            sb.AppendLine($"# Incremental Sweep: {Escape(property)}");
            AppendSweepTable(sb, massSteps, property);
            sb.AppendLine();
        }

        // ── 6. Area sweep section (when present) ─────────────────────────────
        var areaSteps = massSteps.Where(s => s.MassRunResult!.AreaSweepSummary is not null).ToList();
        if (areaSteps.Count > 0)
        {
            sb.AppendLine("# Area Sweep (Width × Height)");
            AppendAreaSweepTable(sb, areaSteps);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Writes a two-row header and a single data row for the standard (non-incremental) batch
    /// comparison across all <paramref name="massSteps"/>.
    /// </summary>
    private static void AppendComparisonTable(
        StringBuilder sb,
        IReadOnlyList<AggregationStepResult> massSteps,
        bool includeIncrementalValueColumn)
    {
        // Row 1: run-type group headers.
        // Format:  Batch | [IncrVal] | RunName1 | | | RunName2 | | | …
        var row1 = new List<string> { "Batch" };
        if (includeIncrementalValueColumn)
            row1.Add("IncrementalValue");
        foreach (var step in massSteps)
        {
            row1.Add(step.StepName);   // run type label
            row1.Add(string.Empty);    // padding for AvgTurns column
            row1.Add(string.Empty);    // padding for Wins column
            row1.Add(string.Empty);    // padding for WinPct column
        }
        sb.AppendLine(string.Join(",", row1));

        // Row 2: sub-headers.
        var row2 = new List<string> { string.Empty };
        if (includeIncrementalValueColumn)
            row2.Add(string.Empty);
        foreach (var _ in massSteps)
        {
            row2.Add("MaxTurns");
            row2.Add("AvgTurns");
            row2.Add("Wins");
            row2.Add("WinPct");
        }
        sb.AppendLine(string.Join(",", row2));

        // Data row: standard batch stats per run type.
        var dataRow = new List<string> { "Standard" };
        if (includeIncrementalValueColumn)
            dataRow.Add("n/a");
        foreach (var step in massSteps)
        {
            var b = step.MassRunResult!.StandardBatch;
            dataRow.Add(b.MaxTurns.ToString());
            dataRow.Add(b.AverageTurns.ToString("F2"));
            dataRow.Add(b.Wins.ToString());
            dataRow.Add(b.WinPercentage.ToString("F1"));
        }
        sb.AppendLine(string.Join(",", dataRow));
    }

    /// <summary>
    /// Writes a swept-property section: two-row header + one data row per sweep step.
    /// Sweep steps are aligned by their position index (all run types use the same config ranges).
    /// </summary>
    private static void AppendSweepTable(
        StringBuilder sb,
        IReadOnlyList<AggregationStepResult> massSteps,
        string property)
    {
        // Collect per-step sweep summaries for each run type (null when a run type lacks this sweep).
        var sweepPerStep = massSteps
            .Select(s => s.MassRunResult!.SweepSummaries.FirstOrDefault(sw => sw.Property == property))
            .ToList();

        // Determine max batch count (number of sweep steps).
        int stepCount = sweepPerStep
            .Where(sw => sw is not null)
            .Max(sw => sw!.Batches.Count);

        if (stepCount == 0)
            return;

        // Use the first available sweep summary to extract step values.
        var referenceSweep = sweepPerStep.First(sw => sw is not null)!;

        // Row 1: group headers.
        var row1 = new List<string> { $"Step ({property})", "IncrementalValue" };
        foreach (var step in massSteps)
        {
            row1.Add(step.StepName);
            row1.Add(string.Empty);
            row1.Add(string.Empty);
        }
        sb.AppendLine(string.Join(",", row1));

        // Row 2: sub-headers.
        var row2 = new List<string> { string.Empty, string.Empty };
        foreach (var _ in massSteps)
        {
            row2.Add("MaxTurns");
            row2.Add("AvgTurns");
            row2.Add("Wins");
        }
        sb.AppendLine(string.Join(",", row2));

        // Data rows.
        for (int i = 0; i < stepCount; i++)
        {
            // Incremental property value for this step derived from Min + i * Step.
            string incrementalValue = ComputeStepValue(referenceSweep, i);

            var dataRow = new List<string> { (i + 1).ToString(), incrementalValue };

            foreach (var sweep in sweepPerStep)
            {
                if (sweep is null || i >= sweep.Batches.Count)
                {
                    dataRow.Add("n/a");
                    dataRow.Add("n/a");
                    dataRow.Add("n/a");
                }
                else
                {
                    var b = sweep.Batches[i];
                    dataRow.Add(b.MaxTurns.ToString());
                    dataRow.Add(b.AverageTurns.ToString("F2"));
                    dataRow.Add(b.Wins.ToString());
                }
            }

            sb.AppendLine(string.Join(",", dataRow));
        }
    }

    /// <summary>Writes the area-sweep section (Width × Height sweeps).</summary>
    private static void AppendAreaSweepTable(
        StringBuilder sb,
        IReadOnlyList<AggregationStepResult> areaSteps)
    {
        int stepCount = areaSteps
            .Select(s => s.MassRunResult!.AreaSweepSummary!.Batches.Count)
            .Max();

        if (stepCount == 0)
            return;

        // Row 1 and Row 2 headers.
        var row1 = new List<string> { "Step (Area)", "IncrementalValue" };
        foreach (var step in areaSteps)
        {
            row1.Add(step.StepName);
            row1.Add(string.Empty);
            row1.Add(string.Empty);
        }
        sb.AppendLine(string.Join(",", row1));

        var row2 = new List<string> { string.Empty, string.Empty };
        foreach (var _ in areaSteps)
        {
            row2.Add("MaxTurns");
            row2.Add("AvgTurns");
            row2.Add("Wins");
        }
        sb.AppendLine(string.Join(",", row2));

        // Data rows.
        var referenceSweep = areaSteps.First().MassRunResult!.AreaSweepSummary!;
        for (int i = 0; i < stepCount; i++)
        {
            string incrementalValue = i < referenceSweep.Batches.Count
                ? ComputeStepValue(referenceSweep, i)
                : (i + 1).ToString();

            var dataRow = new List<string> { (i + 1).ToString(), incrementalValue };

            foreach (var step in areaSteps)
            {
                var sweepSummary = step.MassRunResult!.AreaSweepSummary;
                if (sweepSummary is null || i >= sweepSummary.Batches.Count)
                {
                    dataRow.Add("n/a");
                    dataRow.Add("n/a");
                    dataRow.Add("n/a");
                }
                else
                {
                    var b = sweepSummary.Batches[i];
                    dataRow.Add(b.MaxTurns.ToString());
                    dataRow.Add(b.AverageTurns.ToString("F2"));
                    dataRow.Add(b.Wins.ToString());
                }
            }

            sb.AppendLine(string.Join(",", dataRow));
        }
    }

    /// <summary>
    /// Reconstructs the swept property value for step index <paramref name="i"/>
    /// from the sweep summary's Min and Step strings.
    /// Falls back to the step number when parsing fails.
    /// </summary>
    private static string ComputeStepValue(IncrementalRunSummary sweep, int i)
    {
        if (int.TryParse(sweep.Min, out int min) && int.TryParse(sweep.Step, out int step))
            return (min + i * step).ToString();
        return (i + 1).ToString();
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
