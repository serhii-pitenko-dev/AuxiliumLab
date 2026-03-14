using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.InanimateObjects;

public class Exit : SandboxMapBaseObject
{
    /// <summary>
    /// Object is not on map yet, so cell is null. 
    /// It will be placed on the map later and cell will be assigned then.
    /// </summary>
    /// <param name="id">Exit identifier</param>
    public Exit(Guid id) : base(ObjectType.Exit, null, id)
    {
        Transparent = true;
    }

    /// <summary>
    /// Create exit with assigned cell. 
    /// This constructor can be used when we want to create exit and place it on the map at the same time. 
    /// Cell will be assigned to the exit and exit will be placed on the map in one step.
    /// </summary>
    /// <param name="cell">Cell on the map where the exit is placed</param>
    /// <param name="id">Exit identifier</param>
    public Exit(Cell cell, Guid id) : base(ObjectType.Exit, cell, id) 
    { 
        Transparent = true;
    }
}

