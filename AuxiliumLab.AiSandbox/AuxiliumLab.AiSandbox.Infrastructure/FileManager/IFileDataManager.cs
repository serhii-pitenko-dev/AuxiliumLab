using System.Threading.Tasks;

namespace AuxiliumLab.AiSandbox.Infrastructure.FileManager;

public interface IFileDataManager<T>
{
    Task SaveOrAppendAsync(Guid id, T obj);

    /// <summary>
    /// Appends any object to the same file identified by <paramref name="id"/>.
    /// Allows heterogeneous entries (e.g. different result types) to coexist in one log file.
    /// Each entry is written as a JSON object with a "$type" discriminator and a newline separator.
    /// </summary>
    Task AppendObjectAsync(Guid id, object obj);

    Task<T> LoadObjectAsync(Guid id);
    bool DeleteObject(Guid id);
    IEnumerable<Guid> GetAvailableVersions();
    void Clear();
}