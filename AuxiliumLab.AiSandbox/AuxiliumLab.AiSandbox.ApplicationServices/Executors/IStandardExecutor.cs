using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Domain.Statistics.Result;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Executors;

public interface IStandardExecutor : IExecutor
{
    /// <summary>Runs a simulation and returns the captured outcome using the injected default configuration.</summary>
    Task<ParticularRun> RunAndCaptureAsync();

    /// <summary>Runs a simulation and returns the captured outcome using the supplied <paramref name="sandBoxConfiguration"/>.</summary>
    Task<ParticularRun> RunAndCaptureAsync(SandBoxConfiguration sandBoxConfiguration);
}
