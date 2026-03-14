using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.PolicyTrainer;
using Grpc.Net.Client;

namespace AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;

/// <summary>
/// gRPC client wrapper for the Python RL Training Service.
/// Forwards calls to the PolicyTrainerService without adding business logic.
/// </summary>
public class PolicyTrainerClient : IPolicyTrainerClient
{
    private readonly GrpcChannel _channel;
    private readonly PolicyTrainerService.PolicyTrainerServiceClient _client;
    private bool _disposed;

    public PolicyTrainerClient(string serverAddress)
    {
        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            throw new ArgumentException("Server address cannot be null or empty.", nameof(serverAddress));
        }

        _channel = GrpcChannel.ForAddress(serverAddress);
        _client = new PolicyTrainerService.PolicyTrainerServiceClient(_channel);
    }

    public async Task<NegotiateEnvironmentResponse> NegotiateEnvironmentAsync(
        NegotiateEnvironmentRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _client.NegotiateEnvironmentAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<TrainingResponse> StartTrainingPPOAsync(
        TrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _client.StartTrainingPPOAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<TrainingResponse> StartTrainingA2CAsync(
        TrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _client.StartTrainingA2CAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<TrainingResponse> StartTrainingDQNAsync(
        TrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _client.StartTrainingDQNAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<StatusResponse> GetTrainingStatusAsync(
        StatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _client.GetTrainingStatusAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<ActResponse> ActAsync(
        ActRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _client.ActAsync(request, cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _channel?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
