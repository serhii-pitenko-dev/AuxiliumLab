using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Executors;

public interface IExecutorFactory
{
    IExecutorForPresentation CreateExecutorForPresentation();
    IStandardExecutor CreateStandardExecutor();

    /// <summary>
    /// Creates a standard executor whose AI decisions are driven by a pre-trained model
    /// via an <c>InferenceActions</c> instance (calls the Python <c>Act</c> RPC).
    /// Each call creates isolated per-simulation broker, agent-store, and AI instances
    /// so that parallel simulations do not share mutable state.
    /// </summary>
    IStandardExecutor CreateInferenceExecutor(
        IPolicyTrainerClient policyTrainerClient,
        string modelPath,
        AiConfiguration aiConfig);
}