using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.InanimateObjects;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Domain.Playgrounds.Builders;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.Mappers;

public class StandardPlaygroundMapper: IStandardPlaygroundMapper
{
    private readonly IPlaygroundBuilder _playgroundBuilder;

    public StandardPlaygroundMapper(IPlaygroundBuilder playgroundBuilder)
    {
        _playgroundBuilder = playgroundBuilder ?? throw new ArgumentNullException(nameof(playgroundBuilder));
    }

    /// <summary>
    /// Maps StandardPlayground domain model to StandardPlaygroundState for persistence
    /// </summary>
    public StandardPlaygroundState ToState(StandardPlayground playground)
    {
        ArgumentNullException.ThrowIfNull(playground);

        return new StandardPlaygroundState
        {
            Turn = playground.Turn,
            Id = playground.Id,
            Hero = playground.Hero != null ? MapHeroToState(playground.Hero) : null,
            Exit = playground.Exit != null ? MapExitToState(playground.Exit) : null,
            Blocks = playground.Blocks
                .Where(b => b is not BorderBlock) // Border blocks are re-created by SetMap on every load — never persist them.
                .Select(MapBlockToState).ToList(),
            Enemies = playground.Enemies.Select(MapEnemyToState).ToList(),
            Map = MapMapToState(playground)
        };
    }

    /// <summary>
    /// Reconstructs StandardPlayground domain model from StandardPlaygroundState
    /// </summary>
    public StandardPlayground FromState(StandardPlaygroundState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        // Create the map
        var map = new MapSquareCells(state.Map.Width, state.Map.Height);

        // Build the playground using domain builder
        var builder = _playgroundBuilder
            .SetMap(map)
            .SetPlaygroundId(state.Id)
            .SetTurn(state.Turn);

        // Place blocks
        foreach (var blockState in state.Blocks)
        {
            var block = new Block(null, blockState.Id);
            builder.PlaceBlock(block, blockState.Coordinates);
        }

        // Place exit
        if (state.Exit != null)
        {
            var exit = new Exit(state.Exit.Id);
            builder.PlaceExit(exit, state.Exit.Coordinates);
        }

        // Place hero with restored state
        if (state.Hero != null)
        {
            var hero = CreateHeroFromState(state.Hero);
            builder.PlaceHero(hero, state.Hero.Coordinates);
        }

        // Place enemies with restored state
        foreach (var enemyState in state.Enemies)
        {
            var enemy = CreateEnemyFromState(enemyState);
            builder.PlaceEnemy(enemy, enemyState.Coordinates);
        }

        return builder
            .FillCellGrid()
            .Build();
    }

    #region To State Mapping

    private HeroState MapHeroToState(Hero hero)
    {
        return new HeroState
        {
            Id = hero.Id,
            Coordinates = hero.Coordinates,
            Speed = hero.Speed,
            SightRange = hero.SightRange,
            IsRun = hero.IsRun,
            Stamina = hero.Stamina,
            MaxStamina = hero.MaxStamina,
            OrderInTurnQueue = hero.OrderInTurnQueue,
            PathToTarget = [.. hero.PathToTarget],
            VisibleCells = hero.VisibleCells.Select(c => c.Coordinates).ToList(),
            AvailableActions = [.. hero.AvailableActions],
            ExecutedActions = [.. hero.ExecutedActions]
        };
    }

    private EnemyState MapEnemyToState(Enemy enemy)
    {
        return new EnemyState
        {
            Id = enemy.Id,
            Coordinates = enemy.Coordinates,
            Speed = enemy.Speed,
            SightRange = enemy.SightRange,
            IsRun = enemy.IsRun,
            Stamina = enemy.Stamina,
            MaxStamina = enemy.MaxStamina,
            OrderInTurnQueue = enemy.OrderInTurnQueue,
            PathToTarget = [.. enemy.PathToTarget],
            VisibleCells = enemy.VisibleCells.Select(c => c.Coordinates).ToList(),
            AvailableActions = [.. enemy.AvailableActions],
            ExecutedActions = [.. enemy.ExecutedActions]
        };
    }

    private BlockState MapBlockToState(Block block)
    {
        return new BlockState
        {
            Id = block.Id,
            Coordinates = block.Coordinates
        };
    }

    private ExitState MapExitToState(Exit exit)
    {
        return new ExitState
        {
            Id = exit.Id,
            Coordinates = exit.Coordinates
        };
    }

    private MapSquareCellsState MapMapToState(StandardPlayground playground)
    {
        var cellGrid = new CellState[playground.MapWidth, playground.MapHeight];

        for (int x = 0; x < playground.MapWidth; x++)
        {
            for (int y = 0; y < playground.MapHeight; y++)
            {
                var cell = playground.GetCell(x, y);
                cellGrid[x, y] = new CellState
                {
                    Coordinates = cell.Coordinates,
                    ObjectType = cell.Object.Type,
                    ObjectId = cell.Object.Id
                };
            }
        }

        return new MapSquareCellsState
        {
            Width = playground.MapWidth,
            Height = playground.MapHeight,
            CellGrid = cellGrid
        };
    }

    #endregion

    #region From State Mapping

    private Hero CreateHeroFromState(HeroState heroState)
    {
        var characters = new InitialAgentCharacters(
            heroState.Speed,
            heroState.SightRange,
            heroState.MaxStamina,
            heroState.PathToTarget,
            heroState.AvailableActions,
            heroState.ExecutedActions,
            heroState.IsRun,
            heroState.OrderInTurnQueue
        );

        var hero = new Hero(characters, heroState.Id);

        return hero;
    }

    private Enemy CreateEnemyFromState(EnemyState enemyState)
    {
        var characters = new InitialAgentCharacters(
            enemyState.Speed,
            enemyState.SightRange,
            enemyState.MaxStamina,
            enemyState.PathToTarget,
            enemyState.AvailableActions,
            enemyState.ExecutedActions,
            enemyState.IsRun,
            enemyState.OrderInTurnQueue
        );

        var enemy = new Enemy(characters, enemyState.Id);

        return enemy;
    }

    #endregion
}