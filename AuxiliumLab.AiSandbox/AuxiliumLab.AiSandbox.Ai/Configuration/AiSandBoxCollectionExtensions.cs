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

        // RandomActions is the default IAiActions for all non-training modes.
        // In Training mode, Sb3Actions instances are created manually by TrainingRunner
        // (one per gym), so IAiActions is never resolved from DI during training.
        // We still register RandomActions here to satisfy ExecutorFactory's constructor
        // dependency — ExecutorFactory is always registered but never invoked in training.
        services.AddScoped<IAiActions, RandomActions>();

        return services;
    }
}

