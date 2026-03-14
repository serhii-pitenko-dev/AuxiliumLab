using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.Mappers;

public interface IStandardPlaygroundMapper
{
    StandardPlaygroundState ToState(StandardPlayground playground);
    StandardPlayground FromState(StandardPlaygroundState state);
}
