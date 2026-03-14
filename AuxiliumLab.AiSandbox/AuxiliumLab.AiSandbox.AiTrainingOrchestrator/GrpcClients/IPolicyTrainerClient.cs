using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.PolicyTrainer;

namespace AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;

/// <summary>
/// Interface for communicating with the Python RL Training Service.
/// This is a thin wrapper around the gRPC client - no business logic.
/// </summary>
public interface IPolicyTrainerClient : IDisposable
{
    /// <summary>
    /// Negotiate the environment contract with the Python RL service before training.
    /// Must be called once per experiment with the environment spec built by
    /// <see cref="EnvironmentSpecBuilder.Build"/>.
    /// The Python side validates the spec and returns a hard error on mismatch.
    /// </summary>
    Task<NegotiateEnvironmentResponse> NegotiateEnvironmentAsync(
        NegotiateEnvironmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start training a PPO model.
    /// </summary>
    Task<TrainingResponse> StartTrainingPPOAsync(TrainingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start training an A2C model.
    /// </summary>
    Task<TrainingResponse> StartTrainingA2CAsync(TrainingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start training a DQN model.
    /// </summary>
    Task<TrainingResponse> StartTrainingDQNAsync(TrainingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get training status and progress.
    /// </summary>
    Task<StatusResponse> GetTrainingStatusAsync(StatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform inference with a trained model.
    /// </summary>
    Task<ActResponse> ActAsync(ActRequest request, CancellationToken cancellationToken = default);
}
