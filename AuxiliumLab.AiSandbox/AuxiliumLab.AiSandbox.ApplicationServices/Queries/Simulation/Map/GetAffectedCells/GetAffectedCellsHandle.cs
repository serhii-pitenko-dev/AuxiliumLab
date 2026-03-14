using AuxiliumLab.AiSandbox.ApplicationServices.Converters.Maps;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetAffectedCells;

public class GetAffectedCellsHandle(IMemoryDataManager<StandardPlayground> memoryDataManager) : IAffectedCells
{
    public AffectedCellsResponse GetFromMemory(Guid playgroundId, Guid objectId)
    {
        StandardPlayground playground = memoryDataManager.LoadObject(playgroundId);

        return playground.GetObjectAffectedCells(objectId);
    }
}
