using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.InanimateObjects;

/// <summary>
/// An impassable border block that lines the outer perimeter of every map.
/// </summary>
/// <remarks>
/// <para>
/// Border blocks are placed automatically by <see cref="AuxiliumLab.AiSandbox.Domain.Playgrounds.Builders.PlaygroundBuilder"/>
/// during <c>SetMap()</c> so that agents can never walk off the edge of the map
/// without any explicit bounds-clamping logic in the executor.
/// </para>
/// <para>
/// Like a regular <see cref="Block"/>, a border block is <c>Transparent = false</c>
/// (it blocks line-of-sight) and has its own <see cref="AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects.ObjectType.BorderBlock"/>
/// type so the renderer can display it with a distinct colour.
/// </para>
/// <para>
/// Border blocks are <b>never persisted</b> in <c>StandardPlaygroundState.Blocks</c>;
/// they are re-created from scratch every time <c>SetMap()</c> is called during
/// playground reconstruction, which keeps saved state compact and avoids
/// duplication of the entire perimeter on every save.
/// </para>
/// </remarks>
public class BorderBlock : Block
{
    /// <summary>
    /// Creates a border block that is not yet placed on the map.
    /// </summary>
    /// <param name="id">Unique identifier for the block.</param>
    public BorderBlock(Guid id) : base(ObjectType.BorderBlock, null, id)
    {
    }
}
