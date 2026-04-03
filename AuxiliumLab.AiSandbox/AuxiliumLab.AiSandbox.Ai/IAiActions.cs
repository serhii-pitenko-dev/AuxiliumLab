using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Ai;

public interface IAiActions
{
    ModelType ModelType { get; }
    AiConfiguration AiConfiguration { get; init; }

    /// <summary>
    /// The agent type this AI handles (Hero or Enemy).
    /// Decision requests for other agent types are ignored.
    /// </summary>
    ObjectType TargetAgentType { get; }

    void Initialize();
}

