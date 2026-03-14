using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Training.Dto;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Commands.Training;

/// <summary>
/// Application-level training commands for the Training feature slice.
/// All methods that mutate training state.
/// </summary>
public interface ITrainingCommands
{
    /// <summary>Starts a PPO training run in the background. Returns a job descriptor immediately (202 pattern).</summary>
    Task<TrainingJobStartedDto> StartPpoTrainingAsync(StartPpoTrainingCommand command, CancellationToken ct = default);

    /// <summary>Not implemented — throws <see cref="NotImplementedException"/>.</summary>
    Task<TrainingJobStartedDto> StartA2cTrainingAsync(StartGenericTrainingCommand command, CancellationToken ct = default);

    /// <summary>Not implemented — throws <see cref="NotImplementedException"/>.</summary>
    Task<TrainingJobStartedDto> StartDqnTrainingAsync(StartGenericTrainingCommand command, CancellationToken ct = default);

    /// <summary>Not implemented — throws <see cref="NotImplementedException"/>.</summary>
    Task<TrainingJobStartedDto> StartSacTrainingAsync(StartGenericTrainingCommand command, CancellationToken ct = default);

    /// <summary>Not implemented — throws <see cref="NotImplementedException"/>.</summary>
    Task<TrainingJobStartedDto> StartTd3TrainingAsync(StartGenericTrainingCommand command, CancellationToken ct = default);

    /// <summary>Not implemented — throws <see cref="NotImplementedException"/>.</summary>
    Task<TrainingJobStartedDto> StartDdpgTrainingAsync(StartGenericTrainingCommand command, CancellationToken ct = default);

    /// <summary>Requests cancellation of a running training job. Returns false if job is not found.</summary>
    Task<bool> StopTrainingAsync(Guid jobId, CancellationToken ct = default);
}
