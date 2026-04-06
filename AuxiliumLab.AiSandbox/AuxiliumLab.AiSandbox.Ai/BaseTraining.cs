using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.Common.Helpers;

namespace AuxiliumLab.AiSandbox.Ai;

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
    /// Structure: {basePath}/{trainedAlgorithmsFolder}/{Algorithm}/{AgentType}/{experimentId}/
    /// The model file (e.g. model.zip) and preconditions.json are stored inside this folder.
    /// </summary>
    public string GetModelFolderPath(string experimentId, string basePath, string trainedAlgorithmsFolder, string agentType)
        => Path.Combine(basePath, trainedAlgorithmsFolder, AlgorithmType.ToString(), agentType, experimentId);

    /// <summary>
    /// Returns the model output file path (without extension) within the experiment folder.
    /// Python SB3 appends .zip when saving.
    /// </summary>
    public string GetModelOutputPath(string experimentId, string basePath, string trainedAlgorithmsFolder, string agentType)
        => Path.Combine(GetModelFolderPath(experimentId, basePath, trainedAlgorithmsFolder, agentType), "model");
}