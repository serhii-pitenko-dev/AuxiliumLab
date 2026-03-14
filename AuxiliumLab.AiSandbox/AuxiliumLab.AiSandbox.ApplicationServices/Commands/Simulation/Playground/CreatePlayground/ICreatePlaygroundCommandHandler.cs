namespace AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground.CreatePlayground;

public interface ICreatePlaygroundCommandHandler
{
    public Guid Handle(CreatePlaygroundCommandParameters createMapCommandParameters);
}


