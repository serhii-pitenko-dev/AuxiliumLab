using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Executors;

public interface IExecutor
{
    Task RunAsync(Guid sandboxId = default, SandBoxConfiguration sandBoxConfiguration = default, CancellationToken cancellationToken = default);

    Task TestRunWithPreconditionsAsync();
}
