using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.SharedContracts;

/// <summary>Sent when an agent toggle-action (Run/RunOff) fires.</summary>
public class AgentToggledDto
{
    public string           JobId       { get; set; } = string.Empty;
    public string           AgentId     { get; set; } = string.Empty;
    public ObjectType       AgentType   { get; set; }
    public string           Action      { get; set; } = string.Empty;
    public bool             IsActivated { get; set; }
    public AgentSnapshotDto Agent       { get; set; } = new();
}
