using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground.CreatePlayground;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground;

public interface IPlaygroundCommandsHandleService
{
    public ICreatePlaygroundCommandHandler CreatePlaygroundCommand { get; }
}
