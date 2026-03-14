using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Configuration;

public static class PolicyTrainerClientServiceCollectionExtensions
{
    /// <summary>
    /// Adds the PolicyTrainer gRPC client to the service collection, reading the server address from configuration.
    /// </summary>
    public static IServiceCollection AddPolicyTrainerClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var config = configuration
            .GetSection(PolicyTrainerClientConfiguration.SectionName)
            .Get<PolicyTrainerClientConfiguration>()
            ?? new PolicyTrainerClientConfiguration();

        services.AddSingleton<IPolicyTrainerClient>(_ => new PolicyTrainerClient(config.ServerAddress));

        return services;
    }

    /// <summary>
    /// Adds the PolicyTrainer gRPC client with an explicit server address.
    /// </summary>
    public static IServiceCollection AddPolicyTrainerClient(
        this IServiceCollection services,
        string serverAddress)
    {
        services.AddSingleton<IPolicyTrainerClient>(_ => new PolicyTrainerClient(serverAddress));

        return services;
    }
}
