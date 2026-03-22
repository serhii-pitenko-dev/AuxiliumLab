using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.PolicyTrainer;

namespace AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Trainers;

public class PpoTraining : BaseTraining, ITraining
{
    private readonly TrainingAlgorithmSettings _settings;

    public override ModelType AlgorithmType => ModelType.PPO;

    public PpoTraining(bool isSameMachine, TrainingAlgorithmSettings settings)
        : base(isSameMachine)
    {
        _settings = settings;
    }

    public string BuildExperimentId() => BuildExperimentId(_settings);

    public TrainingRequest BuildTrainingRequest(TrainingAlgorithmSettings settings, int nEnvs, IReadOnlyList<Guid> gymIds,
        string? basePath = null, string? trainedAlgorithmsFolder = null)
    {
        string experimentId = BuildExperimentId(settings);
        string modelOutputPath = (basePath is not null && trainedAlgorithmsFolder is not null)
            ? GetModelOutputPath(experimentId, basePath, trainedAlgorithmsFolder)
            : GetModelSavePath(experimentId);

        // Ensure the experiment folder exists so Python can save directly into it
        var folderPath = Path.GetDirectoryName(modelOutputPath);
        if (!string.IsNullOrEmpty(folderPath))
            Directory.CreateDirectory(folderPath);

        var request = new TrainingRequest
        {
            ExperimentId = experimentId,
            ModelOutputPath = modelOutputPath
        };
        request.Hyperparameters.Add("n_envs", nEnvs.ToString());
        request.Hyperparameters.Add("gym_ids", string.Join(";", gymIds));
        foreach (var p in settings.Parameters)
        {
            if (p.Name == "total_timesteps")
                request.TotalTimesteps = int.TryParse(p.Value, out int ts) ? ts : 5000;
            else if (p.Name == "seed")
                request.Seed = int.TryParse(p.Value, out int s) ? s : 42;
            else
                request.Hyperparameters.TryAdd(p.Name, p.Value);
        }
        return request;
    }

    public async Task<string> Run(IPolicyTrainerClient policyTrainerClient, IReadOnlyList<Guid> gymIds,
        string? basePath = null, string? trainedAlgorithmsFolder = null)
    {
        int nEnvs = Math.Max(1, gymIds.Count);
        var request = BuildTrainingRequest(_settings, nEnvs, gymIds, basePath, trainedAlgorithmsFolder);
        CancellationToken cancellationToken = new CancellationTokenSource(TimeSpan.FromHours(2)).Token;
        var response = await policyTrainerClient.StartTrainingPPOAsync(request, cancellationToken);
        return response.RunId;
    }
}
