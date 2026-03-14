using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetAffectedCells;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetMapLayout;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Maps;

public interface IMapQueriesHandleService
{
    public IMapLayout MapLayoutQuery { get; set; }

    public IAffectedCells AffectedCellsQuery { get; set; }
}


