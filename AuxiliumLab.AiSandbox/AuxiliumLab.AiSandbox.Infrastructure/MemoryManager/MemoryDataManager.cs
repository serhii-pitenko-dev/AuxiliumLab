using System.Collections.Concurrent;

namespace AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;

public class MemoryDataManager<T>: IMemoryDataManager<T>
{
    private readonly ConcurrentDictionary<Guid, T> _mapStorage = new();

    public void AddOrUpdate(Guid id, T map)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        _mapStorage.AddOrUpdate(id, map, (key, oldValue) => map);
    }

    public T LoadObject(Guid id)
    {
        if (!_mapStorage.TryGetValue(id, out T? map) || map is null)
            throw new KeyNotFoundException($"Map with ID {id} not found.");

        return map;
    }

    public bool DeleteObject(Guid id)
    {
        return _mapStorage.TryRemove(id, out _);
    }

    public IEnumerable<Guid> GetAvailableVersions()
    {
        return [.. _mapStorage.Keys];
    }

    public void Clear()
    {
        _mapStorage.Clear();
    }
}

