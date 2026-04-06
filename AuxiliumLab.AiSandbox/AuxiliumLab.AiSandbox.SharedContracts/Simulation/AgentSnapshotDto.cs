using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.SharedContracts;

/// <summary>Snapshot of agent state accompanying action events.</summary>
public class AgentSnapshotDto
{
    public string     Id               { get; set; } = string.Empty;
    public ObjectType Type             { get; set; }
    public int        Speed            { get; set; }
    public int        SightRange       { get; set; }
    public bool       IsRun            { get; set; }
    public int        Stamina          { get; set; }
    public int        MaxStamina       { get; set; }
    public int        OrderInTurnQueue { get; set; }
}
