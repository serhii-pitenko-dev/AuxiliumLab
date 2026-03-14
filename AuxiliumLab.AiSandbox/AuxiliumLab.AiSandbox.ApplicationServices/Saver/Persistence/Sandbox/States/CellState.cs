using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;

public record CellState
{
    public Coordinates Coordinates { get; init; }
    public ObjectType ObjectType { get; init; }
    public Guid ObjectId { get; init; }
}