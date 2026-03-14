namespace AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto.Performance;

public class TurnExecutionPerformance
{
    public int TurnNumber { get; init; }
    public DateTime Start { get; init; }
    public DateTime Finish { get; set; }
    public Dictionary<Guid, List<ActionExecutionPerformance>> ActionPerformances { get; init; } = new Dictionary<Guid, List<ActionExecutionPerformance>>();
}

