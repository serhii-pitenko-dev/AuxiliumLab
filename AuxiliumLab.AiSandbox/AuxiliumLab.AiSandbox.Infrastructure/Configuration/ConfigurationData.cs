using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;

namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration;

public class ConfigurationData
{
    public SandBoxConfiguration MapSettings { get; init; } = new();
}