using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto.Performance;

public class ActionExecutionPerformance
{
    public ObjectType ObjectType { get; init; }
    public AgentAction Action { get; set; }
    public DateTime Start { get; init; }
    public DateTime Finish { get; set; }
}

