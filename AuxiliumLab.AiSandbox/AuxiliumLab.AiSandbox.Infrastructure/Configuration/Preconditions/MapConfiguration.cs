using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;

public struct MapConfiguration
{
    public IncrementalSize Size { get; set; }
    public ElementsPercentages ElementsPercentages { get; set; }
    public MapType Type { get; set; }
}