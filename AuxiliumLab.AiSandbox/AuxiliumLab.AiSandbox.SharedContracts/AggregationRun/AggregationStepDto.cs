namespace AuxiliumLab.AiSandbox.SharedContracts;

public class AggregationStepDto
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Execution mode string: "Training", "MassRandomAISimulation", "MassTrainedAISimulation".</summary>
    public string Mode { get; set; } = string.Empty;
}
