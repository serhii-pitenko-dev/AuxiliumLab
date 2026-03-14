using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.InanimateObjects;

public class EmptyCell : SandboxMapBaseObject
{
    public EmptyCell(Cell cell) : base(ObjectType.Empty, cell, Guid.Empty) 
    { 
        Transparent = true;
    }
}