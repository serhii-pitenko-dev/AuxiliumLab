using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;
using AuxiliumLab.AiSandbox.Domain.InanimateObjects;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Runner.TestPreconditionSet;

public class TestPreconditionData(IMemoryDataManager<StandardPlayground> playgroundMemoryDataManager): ITestPreconditionData
{
    protected const int MapWidth = 31;
    protected const int MapHeight = 31;
    protected const int HeroX = 10;
    protected const int HeroY = 10;
    protected const int HeroSpeed = 5;
    protected const int HeroSightRange = 9;
    protected const int HeroStamina = 15;
    protected const int EnemySpeed = 3;
    protected const int EnemySightRange = 7;
    protected const int EnemyStamina = 10;


    public Guid CreatePlaygroundWithPreconditions(
        Coordinates? heroCoordinates = null, 
        List<Coordinates> enemies = null, 
        List<Coordinates> blocks = null)
    {
        if (heroCoordinates == null)
        {
            heroCoordinates = new Coordinates(HeroX, HeroY);
        }
        
        if (enemies == null)
        {
            enemies = new List<Coordinates>()
            {
                new Coordinates(HeroX, HeroY + 5),
                new Coordinates(HeroX + 6, HeroY + 15),
            };
        }

        if (blocks == null)
        {
            blocks = new List<Coordinates>
            {
                new Coordinates(HeroX - 2, HeroY + 2),
                new Coordinates(HeroX + 2, HeroY + 4),
                new Coordinates(HeroX + 5, HeroY),
                new Coordinates(HeroX, HeroY - 4),
                new Coordinates(HeroX + 6, HeroY + 18),
                new Coordinates(HeroX + 6, HeroY + 17),
                new Coordinates(HeroX + 7, HeroY + 17),
                new Coordinates(HeroX + 8, HeroY + 17)
            };
        }

        StandardPlayground standardPlayground = CreatePlayground();
        Hero hero = CreateHero();
        standardPlayground.PlaceHero(hero, heroCoordinates);

        foreach (var enemyCoordinates in enemies)
        {
            Enemy enemy = CreateEnemy();
            standardPlayground.PlaceEnemy(enemy, enemyCoordinates);
        }

        foreach (var blockCoordinates in blocks)
        {
            Block block = new Block(Guid.NewGuid());
            standardPlayground.AddBlock(block, blockCoordinates);
        }

        Guid playgroundId = standardPlayground.Id;
        playgroundMemoryDataManager.AddOrUpdate(playgroundId, standardPlayground);

        return playgroundId;
    }   

    protected StandardPlayground CreatePlayground()
    {
        var map = new MapSquareCells(MapWidth, MapHeight);
        var visibilityService = new VisibilityService();
        return new StandardPlayground(map, visibilityService);
    }

    protected Hero CreateHero(int? sightRange = null)
    {
        var characters = new InitialAgentCharacters(
            Speed: HeroSpeed,
            SightRange: sightRange ?? HeroSightRange,
            Stamina: HeroStamina,
            PathToTarget: [],
            AgentActions: [],
            ExecutedActions: [],
            isRun: false,
            orderInTurnQueue: 0
        );

        return new Hero(characters, Guid.NewGuid());
    }

    protected Enemy CreateEnemy(int? sightRange = null)
    {
        var characters = new InitialAgentCharacters(
            Speed: EnemySpeed,
            SightRange: sightRange ?? EnemySightRange,
            Stamina: EnemyStamina,
            PathToTarget: [],
            AgentActions: [],
            ExecutedActions: [],
            isRun: false,
            orderInTurnQueue: 0
        );
        return new Enemy(characters, Guid.NewGuid());
    }
}

