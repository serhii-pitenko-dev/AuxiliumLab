using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;

public record ExitState
{
    public Guid Id { get; init; }
    public Coordinates Coordinates { get; init; }
}