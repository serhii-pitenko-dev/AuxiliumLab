using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.Entities;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetAffectedCells;

public record AffectedCellsResponse(int TurnNumber, List<MapCell> Cells);

