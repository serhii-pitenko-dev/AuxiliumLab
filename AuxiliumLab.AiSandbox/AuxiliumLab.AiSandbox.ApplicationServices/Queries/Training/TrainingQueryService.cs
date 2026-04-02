using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Training;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Training;

/// <summary>
/// Read-only query service for training data. Reads job statuses from
/// <see cref="TrainingCommandService"/> and trained models from disk.
/// </summary>
public sealed class TrainingQueryService : ITrainingQueries
{
    private readonly TrainingCommandService _commandService;
    private readonly IOptions<FileSourceConfiguration> _fileSourceConfig;

    public TrainingQueryService(
        TrainingCommandService commandService,
        IOptions<FileSourceConfiguration> fileSourceConfig)
    {
        _commandService   = commandService;
        _fileSourceConfig = fileSourceConfig;
    }

    public Task<IReadOnlyList<TrainingJobStatusDto>> GetTrainingStatusesAsync(CancellationToken ct = default)
        => Task.FromResult(_commandService.GetJobStatuses());

    public Task<IReadOnlyList<TrainedModelInfoDto>> GetTrainedModelsAsync(CancellationToken ct = default)
    {
        var basePath = _fileSourceConfig.Value.FileStorage.BasePath;
        var trainedFolder = _fileSourceConfig.Value.FileStorage.TrainedAlgorithms;
        var root = Path.Combine(basePath, trainedFolder);
        var models = new List<TrainedModelInfoDto>();

        if (!Directory.Exists(root))
            return Task.FromResult<IReadOnlyList<TrainedModelInfoDto>>(models);

        foreach (var algoDir in Directory.EnumerateDirectories(root))
        {
            string algorithm = Path.GetFileName(algoDir);
            foreach (var expDir in Directory.EnumerateDirectories(algoDir))
            {
                string experimentId = Path.GetFileName(expDir);
                string modelFile    = Path.Combine(expDir, "model.zip");
                string errorFile    = Path.Combine(expDir, "error.json");
                bool hasModel       = File.Exists(modelFile);
                bool hasError       = File.Exists(errorFile);

                // Skip folders that have neither a model nor an error
                if (!hasModel && !hasError)
                    continue;

                TrainingPreconditionsDto? preconditions = null;
                string preconditionsFile = Path.Combine(expDir, "preconditions.json");
                if (File.Exists(preconditionsFile))
                {
                    try
                    {
                        string json = File.ReadAllText(preconditionsFile);
                        preconditions = JsonSerializer.Deserialize<TrainingPreconditionsDto>(json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch { /* corrupt file — skip */ }
                }

                string? errorMessage = null;
                if (hasError)
                {
                    try
                    {
                        string errorJson = File.ReadAllText(errorFile);
                        var errorData = JsonSerializer.Deserialize<Dictionary<string, string>>(errorJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        errorData?.TryGetValue("ErrorMessage", out errorMessage);
                    }
                    catch { /* corrupt file — skip */ }
                }

                models.Add(new TrainedModelInfoDto
                {
                    Algorithm     = algorithm,
                    ExperimentId  = experimentId,
                    ModelFilePath = hasModel ? modelFile : string.Empty,
                    TrainedAt     = hasModel ? File.GetLastWriteTime(modelFile)
                                  : File.GetLastWriteTime(hasError ? errorFile : preconditionsFile),
                    Preconditions = preconditions,
                    IsFailed      = hasError && !hasModel,
                    ErrorMessage  = errorMessage
                });
            }
        }

        return Task.FromResult<IReadOnlyList<TrainedModelInfoDto>>(
            models.OrderByDescending(m => m.TrainedAt).ToList());
    }
}
