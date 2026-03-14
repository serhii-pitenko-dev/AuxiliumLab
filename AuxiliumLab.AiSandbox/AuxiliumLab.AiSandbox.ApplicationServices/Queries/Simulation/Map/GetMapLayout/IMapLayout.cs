namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetMapLayout;

public interface IMapLayout
{
    MapLayoutResponse GetFromMemory(Guid guid);

    Task<MapLayoutResponse> GetFromFile(Guid guid);
}

