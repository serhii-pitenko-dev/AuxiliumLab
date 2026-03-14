namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetAffectedCells;

public interface IAffectedCells
{
    AffectedCellsResponse GetFromMemory(Guid playgroundId, Guid objectId);
}
