namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Training;

/// <summary>
/// Read-only training queries for the Training feature slice.
/// </summary>
public interface ITrainingQueries
{
    /// <summary>Returns a list of all trained models found on disk, with their precondition parameters.</summary>
    Task<IReadOnlyList<TrainedModelInfoDto>> GetTrainedModelsAsync(CancellationToken ct = default);

    /// <summary>Returns the current status of all training jobs (running and recently completed).</summary>
    Task<IReadOnlyList<TrainingJobStatusDto>> GetTrainingStatusesAsync(CancellationToken ct = default);
}
