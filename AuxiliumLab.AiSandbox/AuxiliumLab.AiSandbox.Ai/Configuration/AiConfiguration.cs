namespace AuxiliumLab.AiSandbox.Ai.Configuration;

public class AiConfiguration
{
    public string Version { get; init; } = string.Empty;
    public ModelType ModelType { get; init; }
    public AiPolicy PolicyType { get; init; }
}

