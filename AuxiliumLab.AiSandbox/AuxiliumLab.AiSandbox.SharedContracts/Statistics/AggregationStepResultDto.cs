namespace AuxiliumLab.AiSandbox.SharedContracts;

public sealed class AggregationStepResultDto
{
    public string  StepName              { get; set; } = string.Empty;
    public string  Mode                  { get; set; } = string.Empty;
    public int     TotalRuns             { get; set; }
    public int     Wins                  { get; set; }
    public double  WinPercentage         => TotalRuns > 0 ? (double)Wins / TotalRuns * 100.0 : 0.0;
    public double  AverageTurns          { get; set; }
    public string? TrainingExperimentId  { get; set; }
}
