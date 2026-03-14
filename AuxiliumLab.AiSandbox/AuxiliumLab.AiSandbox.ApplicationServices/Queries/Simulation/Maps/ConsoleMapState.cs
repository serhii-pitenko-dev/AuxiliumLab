using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.Entities;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Maps;

/// <summary>
/// Pure helper that applies an agent-move result to the in-memory
/// <see cref="MapCell"/> grid maintained by the console presenter.
/// Extracted from <c>ConsoleRunner.HandleAgentMoveEvent</c> so that the
/// map-state mutation logic can be unit-tested independently of the
/// Spectre.Console rendering layer.
/// </summary>
public static class ConsoleMapState
{
    /// <summary>
    /// Updates <paramref name="cells"/> and <paramref name="cellsToRerender"/>
    /// in response to an agent-move outcome.
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>Successful move</b>: the "from" cell is cleared (set to
    ///     <see cref="ObjectType.Empty"/>) and the "to" cell is stamped with
    ///     the agent's id/type. Both coordinates are queued for re-render.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Failed move</b>: the "from" and "to" cells are <b>not</b>
    ///     mutated. Only the "from" coordinate is queued for re-render so that
    ///     any sight-effect changes applied before this call are still flushed.
    ///     This prevents the agent icon from disappearing when it attempts to
    ///     walk into a <see cref="ObjectType.BorderBlock"/> or other obstacle.
    ///   </description></item>
    /// </list>
    /// </summary>
    public static void ApplyAgentMove(
        MapCell[,] cells,
        Coordinates from,
        Coordinates to,
        Guid agentId,
        bool isSuccess,
        HashSet<Coordinates> cellsToRerender)
    {
        if (isSuccess)
        {
            cellsToRerender.Add(from);
            cellsToRerender.Add(to);

            MapCell fromCell = cells[from.X, from.Y];
            cells[from.X, from.Y] = fromCell with
            {
                ObjectId = Guid.Empty,
                ObjectType = ObjectType.Empty
            };

            MapCell toCell = cells[to.X, to.Y];
            cells[to.X, to.Y] = toCell with
            {
                ObjectId = agentId,
                ObjectType = fromCell.ObjectType
            };
        }
        else
        {
            // Move was rejected; agent stays at "from".
            // Re-render "from" so sight-effect changes are flushed without
            // corrupting the cell's object type.
            cellsToRerender.Add(from);
        }
    }
}


