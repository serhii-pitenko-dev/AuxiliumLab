using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects.StartupSettings;
using Microsoft.Extensions.DependencyInjection;

namespace AuxiliumLab.AiSandbox.Ai.Configuration;

public static class AiSandboxCollectionExtensions
{
    public static IServiceCollection AddAiSandboxServices(
        this IServiceCollection services,
        ExecutionMode executionMode)
    {
        services.AddSingleton<Sb3AlgorithmTypeProvider>();

        return services;
    }
}

