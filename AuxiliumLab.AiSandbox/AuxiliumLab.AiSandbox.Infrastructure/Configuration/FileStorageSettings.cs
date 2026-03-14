namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration;

/// <summary>
/// Paths for the hierarchical file storage on disk.
/// BasePath is the storage root; the sub-folder names are relative to it.
/// </summary>
public class FileStorageSettings
{
    /// <summary>Root directory for all file storage (e.g. "E:\\FILE_STORAGE").</summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>Sub-folder that stores trained algorithm model files (e.g. "TRAINED_ALGORITHMS").</summary>
    public string TrainedAlgorithms { get; set; } = "TRAINED_ALGORITHMS";

    /// <summary>Sub-folder that stores pre-created playground layouts (e.g. "SAVED_PLAYGROUNDS").</summary>
    public string PrecreatedPlaygrounds { get; set; } = "SAVED_PLAYGROUNDS";

    /// <summary>Sub-folder that stores saved simulation results (e.g. "SAVED_SIMULATIONS").</summary>
    public string SavedSimulations { get; set; } = "SAVED_SIMULATIONS";
}
