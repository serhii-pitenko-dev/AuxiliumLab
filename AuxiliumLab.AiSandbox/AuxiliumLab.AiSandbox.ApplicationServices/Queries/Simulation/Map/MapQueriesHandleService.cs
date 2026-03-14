using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetAffectedCells;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetMapLayout;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Maps;

public class MapQueriesHandleService(
    IMapLayout mapLayoutQuery,
    IAffectedCells affectedCellsQuery) : IMapQueriesHandleService
{
    public required IMapLayout MapLayoutQuery { get; set; } = mapLayoutQuery;
    public required IAffectedCells AffectedCellsQuery { get; set; } = affectedCellsQuery;
}


