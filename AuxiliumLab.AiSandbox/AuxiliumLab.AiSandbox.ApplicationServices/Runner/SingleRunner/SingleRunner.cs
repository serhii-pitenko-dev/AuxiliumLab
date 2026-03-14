using AuxiliumLab.AiSandbox.ApplicationServices.Executors;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Runner.SingleRunner;

/// <summary>
/// Handles single-run execution modes: one-shot simulation and precondition test runs.
/// </summary>
public class SingleRunner
{
    private readonly SandBoxConfiguration _configuration;

    public SingleRunner(SandBoxConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>Runs a single simulation with the presentation executor (publishes events to UI).</summary>
    public async Task RunSingleAsync(IExecutorForPresentation executor)
        => await executor.RunAsync();

    /// <summary>Runs a single trained-model simulation.</summary>
    public async Task RunSingleTrainedAsync(IStandardExecutor executor)
        => await executor.RunAsync();

    /// <summary>Runs a simulation seeded from precondition test data.</summary>
    public async Task RunTestPreconditionsAsync(IExecutorForPresentation executor)
        => await executor.TestRunWithPreconditionsAsync();
}
