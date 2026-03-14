namespace AuxiliumLab.AiSandbox.Infrastructure.FileManager;

public class NullFileDataManager<T> : IFileDataManager<T>
{
    public Task SaveOrAppendAsync(Guid id, T obj)
    {
        return Task.CompletedTask;
    }

    public Task AppendObjectAsync(Guid id, object obj)
    {
        return Task.CompletedTask;
    }

    public void Clear()
    {

    }

    public bool DeleteObject(Guid id)
    {
        return true;
    }

    public IEnumerable<Guid> GetAvailableVersions()
    {
        return Enumerable.Empty<Guid>();
    }

    public Task<T> LoadObjectAsync(Guid id)
    {
        return Task.FromResult(default(T)!);
    }
}

