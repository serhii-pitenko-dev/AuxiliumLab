using AuxiliumLab.AiSandbox.Ai.Configuration;

namespace AuxiliumLab.AiSandbox.Ai;

public interface IAiActions
{
    ModelType ModelType { get; }
    AiConfiguration AiConfiguration { get; init; }
    void Initialize();
}

