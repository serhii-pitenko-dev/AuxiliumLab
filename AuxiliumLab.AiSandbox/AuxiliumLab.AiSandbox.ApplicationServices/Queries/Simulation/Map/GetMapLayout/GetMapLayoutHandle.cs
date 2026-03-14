using AuxiliumLab.AiSandbox.ApplicationServices.Converters.Maps;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetMapLayout;

public class GetMapLayoutHandle(
    IMemoryDataManager<StandardPlayground> MemoryDataManager,
    IFileDataManager<StandardPlayground> FileDataManager) : IMapLayout
{
    public MapLayoutResponse GetFromMemory(Guid guid)
    {
        StandardPlayground playground = MemoryDataManager.LoadObject(guid);

        return playground.ToMapLayout();
    }

    public async Task<MapLayoutResponse> GetFromFile(Guid guid)
    {
        StandardPlayground playground = await FileDataManager.LoadObjectAsync(guid);
        MemoryDataManager.AddOrUpdate(guid, playground);

        return playground.ToMapLayout();
    }
}
