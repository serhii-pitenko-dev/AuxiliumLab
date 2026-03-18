using AuxiliumLab.AiSandbox.Domain.Statistics.Result;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Executors;

public interface IExecutorForPresentation : IExecutor
{
    /// <summary>
    /// Delay in milliseconds applied between each agent action notification during presentation runs.
    /// </summary>
    int ActionDelayMs { get; set; }

    /// <summary>
    /// Optional semaphore used to pause/resume the simulation loop.
    /// When non-null, the loop awaits this gate before each turn.
    /// Pause: take the slot (count → 0). Resume: release the slot (count → 1).
    /// </summary>
    SemaphoreSlim? PauseGate { get; set; }
}
