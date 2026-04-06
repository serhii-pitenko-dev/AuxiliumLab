namespace AuxiliumLab.AiSandbox.SharedContracts;

public class PpoHyperparametersDto
{
    public int    TotalTimesteps { get; set; }
    public double LearningRate   { get; set; }
    public int    NSteps         { get; set; }
    public int    BatchSize      { get; set; }
    public int    NEpochs        { get; set; }
    public double Gamma          { get; set; }
    public double GaeLambda      { get; set; }
    public double ClipRange      { get; set; }
    public double EntCoef        { get; set; }
    public int    Seed           { get; set; }
    /// <summary>Number of parallel gym environments.</summary>
    public int    NEnvs          { get; set; }
}
