namespace AuxiliumLab.AiSandbox.SharedContracts;

public sealed class CompletedSimulationRunDto
{
    public Guid     JobId         { get; set; }
    public string   Kind          { get; set; } = string.Empty;
    public DateTime StartedAt     { get; set; }
    public DateTime CompletedAt   { get; set; }
    public int      TotalRuns     { get; set; }
    public int      Wins          { get; set; }
    public int      Losses        { get; set; }
    public double   WinPercentage => TotalRuns > 0 ? (double)Wins / TotalRuns * 100.0 : 0.0;
    public double   AverageTurns  { get; set; }
}
