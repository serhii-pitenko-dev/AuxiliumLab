namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration;

/// <summary>
/// Configuration for the PolicyTrainer gRPC client.
/// </summary>
public class PolicyTrainerClientConfiguration
{
    public const string SectionName = "PolicyTrainerClient";

    /// <summary>
    /// Address of the Python RL Training Service (e.g., "http://localhost:50051")
    /// </summary>
    public string ServerAddress { get; set; } = "http://localhost:50051";
}
