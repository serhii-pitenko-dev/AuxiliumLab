using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;

public record AgentStateForAIDecision(
    Guid PlaygroundId,
    Guid Id,
    ObjectType Type,
    Coordinates Coordinates,
    int Speed,
    int SightRange,
    bool IsRun,
    int Stamina,
    int MaxStamina,
    List<VisibleCellData> VisibleCells,
    List<AgentAction> AvailableLimitedActions,
    List<AgentAction> ExecutedActions);


