using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.InanimateObjects;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.Maps;

public class MapSquareCells
{
    public int Width { get; init; }
    public int Height { get; init; }
    public int Area { get; init; } = new();
    private readonly Cell[,] _cellGrid;
    internal Cell[,] CellGrid => _cellGrid;

    public MapSquareCells(int width, int height)
    {
        Width = width;
        Height = height;
        Area = Width * Height;
        _cellGrid = new Cell[Width, Height];

        // Initialize every cell in the grid
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Cell newCell = new Cell(new Coordinates(x, y));
                EmptyCell emptyCell = new EmptyCell(newCell);
                newCell.PlaceObjectToThisCell(emptyCell);
                _cellGrid[x, y] = newCell;
            }
        }
    }

    internal Cell MoveObject(Coordinates from, Coordinates to)
    {
        Cell initialCell = _cellGrid[from.X, from.Y];
        Cell targetCell = _cellGrid[to.X, to.Y];
        if (initialCell.Object.Type == ObjectType.Empty)
        {
            throw new InvalidOperationException("No object to move at the source coordinates.");
        }
        if (targetCell.Object.Type != ObjectType.Empty)
        {
            throw new InvalidOperationException("Target cell is already occupied.");
        }

        targetCell.PlaceObjectToThisCell(initialCell.Object);

        if (targetCell.Object is Agent agent)
        {
            agent.AgentWasMoved(to);
        }

        initialCell.PlaceObjectToThisCell(new EmptyCell(initialCell));

        return targetCell;
    }

    internal Cell[,] CutOutPartOfTheMap(Coordinates point, int radius)
    {
        // Calculate the bounds of the cutout area
        int startX = Math.Max(0, point.X - radius);
        int endX = Math.Min(Width - 1, point.X + radius);
        int startY = Math.Max(0, point.Y - radius);
        int endY = Math.Min(Height - 1, point.Y + radius);

        // Calculate dimensions of the result array
        int width = endX - startX + 1;
        int height = endY - startY + 1;

        // Create the result array
        Cell[,] result = new Cell[width, height];

        // Copy the cells from the map to the result array
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                result[x, y] = _cellGrid[startX + x, startY + y];
            }
        }

        return result;
    }

    internal void PlaceObject(SandboxMapBaseObject obj, Coordinates coordinates)
    {
        Cell targetCell = _cellGrid[coordinates.X, coordinates.Y];
        if (targetCell.Object.Type != ObjectType.Empty)
        {
            throw new InvalidOperationException("Target cell is already occupied.");
        }
        targetCell.PlaceObjectToThisCell(obj);
    }
}