namespace AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

public enum ObjectType
{
    Empty,
    Block,
    Hero,
    Enemy,
    Exit,
    /// <summary>
    /// An impassable border wall that lines the outer perimeter of every map.
    /// Placed automatically at map creation time. Rendered distinctly from
    /// interior blocks but behaves identically (impassable, opaque).
    /// </summary>
    BorderBlock
}

