using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.Ai.GrpcClients;
using AuxiliumLab.AiSandbox.Ai.PolicyTrainer;

namespace AuxiliumLab.AiSandbox.Ai.Trainers;

public class A2cTraining : BaseTraining, ITraining
{
    private readonly TrainingAlgorithmSettings _settings;

    public override ModelType AlgorithmType => ModelType.A2C;

    public A2cTraining(bool isSameMachine, TrainingAlgorithmSettings settings)
        : base(isSameMachine)
    {
        _settings = settings;
    }

    public string BuildExperimentId() => BuildExperimentId(_settings);

    public TrainingRequest BuildTrainingRequest(TrainingAlgorithmSettings settings, int nEnvs, IReadOnlyList<Guid> gymIds,
        string basePath, string trainedAlgorithmsFolder, string agentType)
    {
        string experimentId = BuildExperimentId(settings);
        var request = new TrainingRequest
        {
            ExperimentId = experimentId,
            ModelOutputPath = GetModelOutputPath(experimentId, basePath, trainedAlgorithmsFolder, agentType)
        };

        request.Hyperparameters.Add("n_envs", nEnvs.ToString());
        request.Hyperparameters.Add("gym_ids", string.Join(";", gymIds));
        foreach (var p in settings.Parameters)
        {
            if (p.Name == "total_timesteps")
                request.TotalTimesteps = int.TryParse(p.Value, out int ts) ? ts : throw new ArgumentException($"Invalid total_timesteps value: '{p.Value}'");
            else if (p.Name == "seed")
                request.Seed = int.TryParse(p.Value, out int s) ? s : throw new ArgumentException($"Invalid seed value: '{p.Value}'");
            else
                request.Hyperparameters.TryAdd(p.Name, p.Value);
        }

        return request;
    }

    public async Task<string> Run(IPolicyTrainerClient policyTrainerClient, IReadOnlyList<Guid> gymIds,
        string basePath, string trainedAlgorithmsFolder, string agentType)
    {
        int nEnvs = Math.Max(1, gymIds.Count);
        var request = BuildTrainingRequest(_settings, nEnvs, gymIds, basePath, trainedAlgorithmsFolder, agentType);
        CancellationToken cancellationToken = new CancellationTokenSource(TimeSpan.FromHours(2)).Token;
        var response = await policyTrainerClient.StartTrainingA2CAsync(request, cancellationToken);
        return response.RunId;
    }
}