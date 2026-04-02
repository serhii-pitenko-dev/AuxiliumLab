using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.PolicyTrainer;

namespace AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Trainers;

public interface ITraining
{
    int PhysicalCores { get; }
    ModelType AlgorithmType { get; }
    Task<string> Run(IPolicyTrainerClient policyTrainerClient, IReadOnlyList<Guid> gymIds,
        string basePath, string trainedAlgorithmsFolder);
    string BuildExperimentId();
    TrainingRequest BuildTrainingRequest(TrainingAlgorithmSettings settings, int nEnvs, IReadOnlyList<Guid> gymIds,
        string basePath, string trainedAlgorithmsFolder);
}