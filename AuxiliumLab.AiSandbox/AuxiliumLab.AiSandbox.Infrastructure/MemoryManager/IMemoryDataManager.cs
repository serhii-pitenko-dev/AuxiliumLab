namespace AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;

public interface IMemoryDataManager<T>
{
    void AddOrUpdate(Guid id, T map);
    T LoadObject(Guid id);
    bool DeleteObject(Guid id);
    IEnumerable<Guid> GetAvailableVersions();
    void Clear();
}
