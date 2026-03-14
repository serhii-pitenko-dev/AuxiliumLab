using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;

public record VisibleCellData(
    Coordinates Coordinates,
    ObjectType ObjectType,
    Guid ObjectId,
    bool IsTransparent);