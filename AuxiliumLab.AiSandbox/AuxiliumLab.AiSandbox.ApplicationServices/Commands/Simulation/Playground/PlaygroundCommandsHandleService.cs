using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground.CreatePlayground;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground;

public class PlaygroundCommandsHandleService(
    ICreatePlaygroundCommandHandler createMapCommandHandler
    ) : IPlaygroundCommandsHandleService
{
    public ICreatePlaygroundCommandHandler CreatePlaygroundCommand { get; } = createMapCommandHandler;
}


