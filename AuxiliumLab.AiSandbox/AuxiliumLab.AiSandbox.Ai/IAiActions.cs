using AuxiliumLab.AiSandbox.Ai.Configuration;

namespace AuxiliumLab.AiSandbox.Ai;

public interface IAiActions
{
    AiConfiguration AiConfiguration { get; init; }
    void Initialize();
}

