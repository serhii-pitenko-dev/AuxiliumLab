
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.Entities;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetMapLayout;

public record MapLayoutResponse(int turnNumber, MapCell[,] Cells);

