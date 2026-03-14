using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.InanimateObjects;

public class Block: SandboxMapBaseObject
{
    /// <summary>
    /// Object is not on map yet, so cell is null. 
    /// It will be placed on the map later and cell will be assigned then.
    /// </summary>
    /// <param name="id">Block identifier</param>
    public Block(Guid id) : base(ObjectType.Block, null, id)
    {
        Transparent = false;
    }

    /// <summary>
    /// Create block with assigned cell. 
    /// This constructor can be used when we want to create block and place it on the map at the same time. 
    /// Cell will be assigned to the block and block will be placed on the map in one step.
    /// </summary>
    /// <param name="cell">Cell on the map where the block is placed</param>
    /// <param name="id">Block identifier</param>
    public Block(Cell cell, Guid id) : base(ObjectType.Block, cell, id) 
    { 
        Transparent = false;
    }

    /// <summary>
    /// Protected constructor for derived block types (e.g. <see cref="BorderBlock"/>)
    /// that need to report a different <see cref="ObjectType"/> to the base class.
    /// </summary>
    protected Block(ObjectType objectType, Cell? cell, Guid id) : base(objectType, cell, id)
    {
        Transparent = false;
    }
}