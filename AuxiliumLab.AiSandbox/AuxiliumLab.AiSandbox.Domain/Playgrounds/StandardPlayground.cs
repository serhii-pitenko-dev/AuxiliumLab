using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;
using AuxiliumLab.AiSandbox.Domain.InanimateObjects;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.Playgrounds;
    
public class StandardPlayground
{
    public int Turn { get; private set; } = 0;
    public Guid Id { get; init; } = Guid.NewGuid();
    public Hero? Hero { get; private set; }
    public Exit? Exit { get; private set; }
    public IReadOnlyCollection<Block> Blocks => _blocks.AsReadOnly();
    public IReadOnlyCollection<Enemy> Enemies => _enemies.AsReadOnly();
    public int MapWidth => _map.Width;
    public int MapHeight => _map.Height;
    public int MapArea => _map.Area;
    private readonly IVisibilityService _visibilityService;
    private readonly List<Block> _blocks = [];
    private readonly List<Enemy> _enemies = [];
    private readonly MapSquareCells _map;

    public StandardPlayground(MapSquareCells map, IVisibilityService visibilityService, Guid? id = null, int? turn = null)
    {
        _map = map;
        _visibilityService = visibilityService ?? throw new ArgumentNullException(nameof(visibilityService));

        if (id.HasValue)
        {
            Id = id.Value;
        }

        if (turn.HasValue)
        {
            Turn = turn.Value;
        }
    }
    public void OnStartTurnActions()
    {
        Turn++;
    }

    public void LookAroundEveryone()
    {
        _visibilityService.UpdateVisibleCells(Hero, this);

        foreach (var enemy in Enemies)
        {
            _visibilityService.UpdateVisibleCells(enemy, this);
        }
    }

    public void UpdateAgentVision(Agent agent)
    {
        _visibilityService.UpdateVisibleCells(agent, this);
    }

    public void PrepareAgentForTurnActions(Agent agent)
    {
        agent.GetReadyForNewTurn();
        agent.ReCalculateAvailableActions();
    }

    public void PlaceHero(Hero hero, Coordinates coordinates)
    {
        Hero = hero;
        _map.PlaceObject(hero, coordinates);
    }

    public void PlaceEnemy(Enemy enemy, Coordinates coordinates)
    {
        _enemies.Add(enemy);
        _map.PlaceObject(enemy, coordinates);
    }

    public void PlaceExit(Exit exit, Coordinates coordinates)
    {
        Exit = exit;
        _map.PlaceObject(Exit, coordinates);
    }

    public void AddBlock(Block block, Coordinates coordinates)
    {
        _blocks.Add(block);
        _map.PlaceObject(block, coordinates);
    }

    /// <summary>
    /// Move agent and update vision
    /// </summary>
    public void MoveObjectOnMap(Coordinates from, Coordinates to)
    {
        var cell = _map.CellGrid[from.X, from.Y];
        var targetCell = _map.MoveObject(from, to);
        if (targetCell.Object is Agent agent)
        {
            UpdateAgentVision(agent);
        }
    }

    public Cell[,] CutMapPart(Coordinates center, int radius)
    {
        return _map.CutOutPartOfTheMap(center, radius);
    }

    /// <summary>
    /// Returns <c>true</c> if the given coordinates fall within the map boundaries.
    /// Use this before calling <see cref="GetCell(Coordinates)"/> to avoid an
    /// <see cref="ArgumentOutOfRangeException"/> when the coordinates may be out of range —
    /// for example, when checking the cell one step ahead of an agent standing at a map edge.
    /// </summary>
    public bool IsInBounds(Coordinates coordinates)
    {
        return coordinates.X >= 0 && coordinates.X < _map.Width
            && coordinates.Y >= 0 && coordinates.Y < _map.Height;
    }

    public Cell GetCell(Coordinates coordinates)
    {
        if (coordinates.X < 0 || coordinates.X >= _map.Width
            || coordinates.Y < 0 || coordinates.Y >= _map.Height)
            throw new ArgumentOutOfRangeException(
                $"Coordinates ({coordinates.X}, {coordinates.Y}) are out of bounds (map {_map.Width}x{_map.Height}).");
        return _map.CellGrid[coordinates.X, coordinates.Y];
    }

    public Cell GetCell(int x, int y)
    {
        if (x < 0 || x >= _map.Width || y < 0 || y >= _map.Height)
            throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are out of bounds.");
        
        return _map.CellGrid[x, y];
    }

    /// <summary>
    /// Get all agents in the order they should act this turn. Hero first, then enemies by their order in turn queue.
    /// </summary>
    /// <returns>List of agents for the current turn.</returns>
    public List<Agent> GetOrderedAgentsForTurn()
    {
        var agents = new List<Agent>();
        if (Hero != null)
            agents.Add(Hero);
        agents.AddRange(Enemies.OrderBy(e => e.OrderInTurnQueue));

        return agents;
    }
}