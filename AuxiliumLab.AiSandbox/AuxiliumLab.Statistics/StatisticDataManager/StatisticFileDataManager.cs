using AuxiliumLab.AiSandbox.Domain.Statistics.Result;
using AuxiliumLab.AiSandbox.Statistics.Converters;

namespace AuxiliumLab.AiSandbox.Statistics.StatisticDataManager;

public class StatisticFileDataManager : IStatisticFileDataManager
{
    string statisticFolderName = "STATISTICS";
    string folder;

    public StatisticFileDataManager(string parentFolderPath)
    {
        folder = System.IO.Path.Combine(parentFolderPath, statisticFolderName);
        if (!System.IO.Directory.Exists(folder))
        {
            System.IO.Directory.CreateDirectory(folder);
        }
    }

    public Task AppendDataToFileAsync(string fileName, object data)
    {
        string filePath = System.IO.Path.Combine(folder, fileName);
        string jsonData = System.Text.Json.JsonSerializer.Serialize(data);

        return System.IO.File.AppendAllTextAsync(filePath, jsonData + Environment.NewLine);
    }

    public Task ConvertToCsvAndAppendAsync(string fileName, string csvContent)
    {
        string filePath = System.IO.Path.Combine(folder, fileName);
        return System.IO.File.AppendAllTextAsync(filePath, csvContent);
    }

    /// <inheritdoc/>
    public async Task<string> SaveAggregationReportAsync(IReadOnlyList<AggregationStepResult> steps, DateTime runDate)
    {
        string fileName = $"aggregation_{runDate:yyyy-MM-dd_HH-mm-ss}.csv";
        string filePath = System.IO.Path.Combine(folder, fileName);

        string csvContent = AggregationReportConverter.ToCsv(steps, runDate);
        await System.IO.File.WriteAllTextAsync(filePath, csvContent);

        return filePath;
    }
}

