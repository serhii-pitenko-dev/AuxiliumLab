using AuxiliumLab.AiSandbox.Domain.Statistics.Result;

namespace AuxiliumLab.AiSandbox.Statistics.StatisticDataManager;

public interface IStatisticFileDataManager
{
    /// <summary>Serialises <paramref name="data"/> as JSON and appends it to the file.</summary>
    public Task AppendDataToFileAsync(string fileName, object data);

    /// <summary>
    /// Appends the pre-formatted <paramref name="csvContent"/> string to the file.
    /// Creates the file (and any parent directories) if it does not yet exist.
    /// </summary>
    public Task ConvertToCsvAndAppendAsync(string fileName, string csvContent);

    /// <summary>
    /// Saves a full aggregation run report to a new CSV file named
    /// <c>aggregation_{timestamp}.csv</c> inside the statistics folder.
    /// The report compares results from multiple run types side-by-side.
    /// </summary>
    /// <param name="steps">Ordered list of completed aggregation step results.</param>
    /// <param name="runDate">The date/time the aggregation run started.</param>
    /// <returns>The absolute path to the saved CSV file.</returns>
    Task<string> SaveAggregationReportAsync(IReadOnlyList<AggregationStepResult> steps, DateTime runDate);
}

