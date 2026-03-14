namespace AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto.Performance;

public class SandboxExecutionPerformance
{
    public DateTime Start { get; init; }
    public DateTime Finish { get; set; }
    public Dictionary<int, TurnExecutionPerformance> TurnPerformances { get; init; } = new Dictionary<int, TurnExecutionPerformance>();
}
