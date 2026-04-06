using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.Ai.GrpcClients;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Executors;

/// <summary>
/// Describes how to create an AI for a single agent type.
/// When <see cref="ModelType"/> is <c>Random</c>, a <see cref="Ai.RandomActions"/> is created.
/// Otherwise an <see cref="AiTrainingOrchestrator.InferenceActions"/> is created using
/// <see cref="ModelPath"/> and <see cref="AiConfig"/>.
/// </summary>
public class AgentAiSpec
{
    public ModelType ModelType { get; init; } = ModelType.Random;
    public string? ModelPath { get; init; }
    public AiConfiguration? AiConfig { get; init; }

    public static AgentAiSpec Random() => new();
}

public interface IExecutorFactory
{
    IExecutorForPresentation CreateExecutorForPresentation(
        SandBoxConfiguration configuration,
        AgentAiSpec heroAiSpec,
        AgentAiSpec enemyAiSpec,
        int actionDelayMs = 0,
        SemaphoreSlim? pauseGate = null);

    IStandardExecutor CreateStandardExecutor(
        SandBoxConfiguration configuration,
        AgentAiSpec heroAiSpec,
        AgentAiSpec enemyAiSpec);
}