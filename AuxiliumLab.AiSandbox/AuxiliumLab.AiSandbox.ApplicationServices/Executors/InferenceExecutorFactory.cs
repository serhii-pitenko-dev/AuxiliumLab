using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Executors;

/// <summary>
/// Wraps an <see cref="IExecutorFactory"/> so that <see cref="CreateStandardExecutor"/>
/// returns an <c>InferenceActions</c>-based executor rather than the default
/// <c>RandomActions</c>-based one.  Used by <c>AggregationRunner</c> when running the
/// trained-AI phase.
/// </summary>
public sealed class InferenceExecutorFactory : IExecutorFactory
{
    private readonly IExecutorFactory _inner;
    private readonly IPolicyTrainerClient _policyTrainerClient;
    private readonly string _modelPath;
    private readonly AiConfiguration _aiConfig;

    public InferenceExecutorFactory(
        IExecutorFactory inner,
        IPolicyTrainerClient policyTrainerClient,
        string modelPath,
        AiConfiguration aiConfig)
    {
        _inner               = inner;
        _policyTrainerClient = policyTrainerClient;
        _modelPath           = modelPath;
        _aiConfig            = aiConfig;
    }

    /// <summary>Delegates to the inner factory (presentation executors use the DI-registered IAiActions).</summary>
    public IExecutorForPresentation CreateExecutorForPresentation()
        => _inner.CreateExecutorForPresentation();

    /// <summary>Creates an inference-based executor instead of the default random-actions executor.</summary>
    public IStandardExecutor CreateStandardExecutor()
        => _inner.CreateInferenceExecutor(_policyTrainerClient, _modelPath, _aiConfig);

    public IStandardExecutor CreateInferenceExecutor(
        IPolicyTrainerClient policyTrainerClient,
        string modelPath,
        AiConfiguration aiConfig)
        => _inner.CreateInferenceExecutor(policyTrainerClient, modelPath, aiConfig);
}
