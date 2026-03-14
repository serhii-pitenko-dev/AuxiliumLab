using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Configuration;
using AuxiliumLab.AiSandbox.Common.Helpers;

namespace AuxiliumLab.AiSandbox.AiTrainingOrchestrator;

public abstract class BaseTraining
{
    public int PhysicalCores { get; private set; }

    public abstract ModelType AlgorithmType { get; }

    protected BaseTraining(bool isSameMachine)
    {
        if (isSameMachine)
        {
            PhysicalCores = SystemInfo.GetPhysicalCoreCount();
        }
        else
        {
            throw new NotImplementedException("Remote training not implemented yet. Core count detection is only supported for local training.");
        }
    }

    public string BuildExperimentId(TrainingAlgorithmSettings settings)
    {
        string paramPart = string.Join("_", settings.Parameters.Select(p => p.Value));
        string datePart = DateTime.Now.ToString("yyyyMMdd");
        return $"{AlgorithmType.ToString().ToLower()}_{paramPart}_{datePart}";
    }

    /// <summary>
    /// Returns the folder path for the experiment's trained model and metadata.
    /// Structure: {basePath}/{trainedAlgorithmsFolder}/{Algorithm}/{experimentId}/
    /// The model file (e.g. model.zip) and preconditions.json are stored inside this folder.
    /// </summary>
    public string GetModelFolderPath(string experimentId, string basePath, string trainedAlgorithmsFolder)
        => Path.Combine(basePath, trainedAlgorithmsFolder, AlgorithmType.ToString(), experimentId);

    /// <summary>
    /// Returns the model output file path (without extension) within the experiment folder.
    /// Python SB3 appends .zip when saving.
    /// </summary>
    public string GetModelOutputPath(string experimentId, string basePath, string trainedAlgorithmsFolder)
        => Path.Combine(GetModelFolderPath(experimentId, basePath, trainedAlgorithmsFolder), "model");

    /// <summary>Legacy overload — uses the hardcoded default path. Prefer the overload with basePath.</summary>
    public string GetModelSavePath(string experimentId)
        => Path.Combine("D:/FILE_STORAGE/TRAINED_ALGORITHMS", AlgorithmType.ToString(), experimentId, "model");
}
