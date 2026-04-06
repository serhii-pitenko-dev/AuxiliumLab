using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.Ai.GrpcClients;
using AuxiliumLab.AiSandbox.Ai.PolicyTrainer;

namespace AuxiliumLab.AiSandbox.Ai.Trainers;

public interface ITraining
{
    int PhysicalCores { get; }
    ModelType AlgorithmType { get; }
    Task<string> Run(IPolicyTrainerClient policyTrainerClient, IReadOnlyList<Guid> gymIds,
        string basePath, string trainedAlgorithmsFolder, string agentType);
    string BuildExperimentId();
    TrainingRequest BuildTrainingRequest(TrainingAlgorithmSettings settings, int nEnvs, IReadOnlyList<Guid> gymIds,
        string basePath, string trainedAlgorithmsFolder, string agentType);
}