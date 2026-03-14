using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;

public record AgentState
{
    public Guid Id { get; init; }
    public Coordinates Coordinates { get; init; }
    public int Speed { get; init; }
    public int SightRange { get; init; }
    public bool IsRun { get; init; }
    public int Stamina { get; init; }
    public int MaxStamina { get; init; }
    public int OrderInTurnQueue { get; init; }
    public List<Coordinates> PathToTarget { get; init; } = [];
    public List<Coordinates> VisibleCells { get; init; } = [];
    public List<AgentAction> AvailableActions { get; init; } = [];
    public List<AgentAction> ExecutedActions { get; init; } = [];
}