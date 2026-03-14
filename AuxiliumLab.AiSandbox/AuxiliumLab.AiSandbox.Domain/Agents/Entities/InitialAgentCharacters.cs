using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.Agents.Entities;

public record struct InitialAgentCharacters(
    int Speed,
    int SightRange,
    int Stamina,
    List<Coordinates> PathToTarget,
    List<AgentAction> AgentActions,
    List<AgentAction> ExecutedActions,
    bool isRun = false,
    int orderInTurnQueue = 0);



