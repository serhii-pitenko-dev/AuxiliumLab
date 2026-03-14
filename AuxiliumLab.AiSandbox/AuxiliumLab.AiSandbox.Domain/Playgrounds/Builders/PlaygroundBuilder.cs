using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Agents.Factories;
using AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;
using AuxiliumLab.AiSandbox.Domain.InanimateObjects;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.Playgrounds.Builders;

public class PlaygroundBuilder(
    IEnemyFactory EnemyFactory,
    IHeroFactory HeroFactory,
    IVisibilityService visibilityService) : IPlaygroundBuilder
{
    private StandardPlayground? _playground;
    private int _enemyOrderCounter = 1; // Start enemy orders from 1
    private Guid? _playgroundId;
    private int? _turn;

    public int Test { get; set; } = 0;

    public StandardPlayground Playground
    {
        get
        {
            if (_playground == null)
            {
                throw new InvalidOperationException("Sandbox has not been initialized. Call SetMap first.");
            }

            return _playground;
        }

        private set => _playground = value;
    }

    public IPlaygroundBuilder SetMap(MapSquareCells tileMap)
    {
        Playground = new StandardPlayground(tileMap, visibilityService, _playgroundId, _turn);
        _playgroundId = Playground.Id;
        _enemyOrderCounter = 1; // Reset counter when creating new playground

        // Surround the playable area with border blocks so agents can never step
        // off the edge of the map. Border blocks are opaque (block LOS) and
        // impassable, just like regular blocks, but carry their own ObjectType
        // so the renderer can colour them distinctly.
        PlaceBorderBlocks();

        return this;
    }

    public IPlaygroundBuilder SetPlaygroundId(Guid id)
    {
        _playgroundId = id;
        
        // If playground already exists, apply immediately
        if (_playground != null)
        {
            var idProperty = typeof(StandardPlayground).GetProperty(nameof(StandardPlayground.Id));
            idProperty?.SetValue(Playground, id);
        }

        return this;
    }

    public IPlaygroundBuilder SetTurn(int turn)
    {
        _turn = turn;
        
        // If playground already exists, apply immediately
        if (_playground != null)
        {
            var turnProperty = typeof(StandardPlayground).GetProperty(nameof(StandardPlayground.Turn));
            turnProperty?.SetValue(Playground, turn);
        }

        return this;
    }

    public IPlaygroundBuilder PlaceBlocks(int blocksCount)
    {
        Test++;
        var random = new Random();

        // Pre-seed with all coordinates that are already occupied (includes border blocks
        // placed by SetMap) so that WouldCreateClosedArea sees the full true state of the
        // map and does not erroneously route through border cells.
        var occupiedCells = new HashSet<(int x, int y)>(
            Playground.Blocks.Select(b => (b.Coordinates.X, b.Coordinates.Y)));

        int placedBlocks = 0;
        while (placedBlocks < blocksCount)
        {
            // Only place interior blocks — skip the entire border perimeter.
            int x = random.Next(1, Playground.MapWidth - 1);
            int y = random.Next(1, Playground.MapHeight - 1);

            // Skip if cell is already occupied
            if (occupiedCells.Contains((x, y)))
                continue;

            // Check if placing a block here would create a closed area
            if (!WouldCreateClosedArea(x, y, occupiedCells))
            {
                var block = new Block(Guid.NewGuid());
                Playground.AddBlock(block, new Coordinates(x, y));
                occupiedCells.Add((x, y));
                placedBlocks++;
            }
        }

        return this;
    }

    public IPlaygroundBuilder PlaceBlock(Block block, Coordinates coordinates)
    {
        Playground.AddBlock(block, coordinates);
        return this;
    }

    public IPlaygroundBuilder PlaceHero(InitialAgentCharacters heroCharacters)
    {
        var random = new Random();
        // x=1 is the first interior column — x=0 is the border wall.
        int x = 1;
        int y;

        do
        {
            // Interior rows only (1 .. Height-2); y=0 and y=Height-1 are border walls.
            y = random.Next(1, Playground.MapHeight - 1);
        } while (IsCellOccupied(x, y));

        Playground.PlaceHero(HeroFactory.CreateHero(heroCharacters), new Coordinates(x, y));

        return this;
    }

    public IPlaygroundBuilder PlaceHero(Hero hero, Coordinates coordinates)
    {
        Playground.PlaceHero(hero, coordinates);
        return this;
    }

    public IPlaygroundBuilder PlaceExit()
    {
        var random = new Random();
        // x=Width-2 is the last interior column — x=Width-1 is the border wall.
        int x = Playground.MapWidth - 2;
        int y;

        do
        {
            // Interior rows only (1 .. Height-2); y=0 and y=Height-1 are border walls.
            y = random.Next(1, Playground.MapHeight - 1);
        } while (IsCellOccupied(x, y));

        Playground.PlaceExit(new Exit(Guid.NewGuid()), new Coordinates(x, y));

        return this;
    }

    public IPlaygroundBuilder PlaceExit(Exit exit, Coordinates coordinates)
    {
        Playground.PlaceExit(exit, coordinates);
        return this;
    }

    public IPlaygroundBuilder PlaceEnemies(int enemiesCount, InitialAgentCharacters enemyCharacters)
    {
        var random = new Random();

        for (int i = 0; i < enemiesCount; i++)
        {
            int x, y;
            bool validPosition;

            do
            {
                // Interior cells only — skip border perimeter.
                x = random.Next(1, Playground.MapWidth - 1);
                y = random.Next(1, Playground.MapHeight - 1);
                validPosition = !IsCellOccupied(x, y) && IsDistanceFromHeroValid(x, y);
            } while (!validPosition);

            var cell = Playground.GetCell(x, y);
            var enemy = EnemyFactory.CreateEnemy(enemyCharacters);
            enemy.SetOrderInTurnQueue(_enemyOrderCounter++); // Assign and increment order

            Playground.PlaceEnemy(enemy, new Coordinates(x, y));
        }

        return this;
    }

    public IPlaygroundBuilder PlaceEnemy(Enemy enemy, Coordinates coordinates)
    {
        Playground.PlaceEnemy(enemy, coordinates);
        return this;
    }

    public IPlaygroundBuilder FillCellGrid()
    {
        for (int x = 0; x < Playground.MapWidth; x++)
        {
            for (int y = 0; y < Playground.MapHeight; y++)
            {
                var cell = Playground.GetCell(x, y);

                // Check for blocks
                var block = Playground.Blocks.FirstOrDefault(b => b.Coordinates.X == x && b.Coordinates.Y == y);
                if (block != null)
                {
                    cell.PlaceObjectToThisCell(block);
                    block.UpdateCell(cell);
                    continue;
                }

                // Check for hero
                if (Playground.Hero != null && Playground.Hero.Coordinates.X == x && Playground.Hero.Coordinates.Y == y)
                {
                    cell.PlaceObjectToThisCell(Playground.Hero);
                    Playground.Hero.UpdateCell(cell);
                    continue;
                }

                // Check for exit
                if (Playground.Exit != null && Playground.Exit.Coordinates.X == x && Playground.Exit.Coordinates.Y == y)
                {
                    cell.PlaceObjectToThisCell(Playground.Exit);
                    Playground.Exit.UpdateCell(cell);
                    continue;
                }

                // Check for enemies
                var enemy = Playground.Enemies.FirstOrDefault(e => e.Coordinates.X == x && e.Coordinates.Y == y);
                if (enemy != null)
                {
                    cell.PlaceObjectToThisCell(enemy);
                    enemy.UpdateCell(cell);
                    continue;
                }

                // If no object found, create empty cell
                var emptyCell = new EmptyCell(cell);
                cell.PlaceObjectToThisCell(emptyCell);
            }
        }

        return this;
    }

    public StandardPlayground Build()
    {
        Playground.LookAroundEveryone();

        return Playground;
    }

    private bool IsCellOccupied(int x, int y)
    {
        // Check blocks
        if (Playground.Blocks.Any(b => b.Coordinates.X == x && b.Coordinates.Y == y))
            return true;

        // Check hero
        if (Playground.Hero != null && Playground.Hero.Coordinates.X == x && Playground.Hero.Coordinates.Y == y)
            return true;

        // Check exit
        if (Playground.Exit != null && Playground.Exit.Coordinates.X == x && Playground.Exit.Coordinates.Y == y)
            return true;

        // Check enemies
        if (Playground.Enemies.Any(e => e.Coordinates.X == x && e.Coordinates.Y == y))
            return true;

        return false;
    }

    private bool IsDistanceFromHeroValid(int x, int y)
    {
        // Calculate Manhattan distance from hero
        int distance = Math.Abs(x - Playground.Hero.Coordinates.X) + Math.Abs(y - Playground.Hero.Coordinates.Y);
        return distance >= 4;
    }

    private bool WouldCreateClosedArea(int x, int y, HashSet<(int x, int y)> occupiedCells)
    {
        // Temporarily add the new block to the occupied set.
        // occupiedCells already includes the border perimeter so the BFS cannot
        // accidentally route through border cells.
        var tempOccupied = new HashSet<(int x, int y)>(occupiedCells)
        {
            (x, y)
        };

        // Check if there is still a path from the interior top-left corner to the
        // interior bottom-right corner (Cartesian coordinates, both stay within
        // the playable 1..Width-2 × 1..Height-2 region).
        var visited = new HashSet<(int x, int y)>();
        var queue = new Queue<(int x, int y)>();
        queue.Enqueue((1, Playground.MapHeight - 2)); // Interior top-left

        while (queue.Count > 0)
        {
            var (curX, curY) = queue.Dequeue();
            if (curX == Playground.MapWidth - 2 && curY == 1) // Interior bottom-right
                return false; // Path exists — no closed area

            var neighbors = new[]
            {
                (curX + 1, curY),
                (curX - 1, curY),
                (curX, curY + 1),
                (curX, curY - 1)
            };

            foreach (var (nextX, nextY) in neighbors)
            {
                if (nextX >= 1 && nextX < Playground.MapWidth - 1 &&
                    nextY >= 1 && nextY < Playground.MapHeight - 1 &&
                    !tempOccupied.Contains((nextX, nextY)) &&
                    !visited.Contains((nextX, nextY)))
                {
                    visited.Add((nextX, nextY));
                    queue.Enqueue((nextX, nextY));
                }
            }
        }

        return true; // No path found — would create a closed area
    }

    /// <summary>
    /// Surrounds the playable area with <see cref="BorderBlock"/> instances.
    /// Called once by <see cref="SetMap"/> immediately after the playground is
    /// created. The border covers the full bottom row (y=0), full top row
    /// (y=Height-1), full left column (x=0) and full right column (x=Width-1).
    /// </summary>
    private void PlaceBorderBlocks()
    {
        int w = Playground.MapWidth;
        int h = Playground.MapHeight;

        // Bottom row (y=0) and top row (y=h-1) — full width including corners.
        for (int bx = 0; bx < w; bx++)
        {
            Playground.AddBlock(new BorderBlock(Guid.NewGuid()), new Coordinates(bx, 0));
            Playground.AddBlock(new BorderBlock(Guid.NewGuid()), new Coordinates(bx, h - 1));
        }

        // Left column (x=0) and right column (x=w-1) — interior rows only
        // (corners already covered above).
        for (int by = 1; by < h - 1; by++)
        {
            Playground.AddBlock(new BorderBlock(Guid.NewGuid()), new Coordinates(0, by));
            Playground.AddBlock(new BorderBlock(Guid.NewGuid()), new Coordinates(w - 1, by));
        }
    }
}