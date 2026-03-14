using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Lost;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Win;

namespace AuxiliumLab.AiSandbox.Domain.Statistics.Result;

/// <summary>
/// Contains information about the result of a single sandbox run within a batch.
/// Saved after each run completes (on HeroWonEvent or HeroLostEvent).
/// </summary>
public record ParticularRun(
    Guid PlaygroundId,
    int TurnsCount,
    int EnemiesCount,
    WinReason? WinReason,
    LostReason? LostReason);
