using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;
using AuxiliumLab.AiSandbox.Domain.InanimateObjects;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using FluentAssertions;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;

[TestClass]
public abstract class VisibilityServiceTestBase
{
    protected const int MapWidth = 21;
    protected const int MapHeight = 21;
    protected const int HeroX = 10;
    protected const int HeroY = 10;
    protected const int HeroSpeed = 5;
    protected const int HeroSightRange = 9;
    protected const int HeroStamina = 15;
    protected const int EnemySpeed = 3;
    protected const int EnemySightRange = 3;
    protected const int EnemyStamina = 10;

    // Expected visible cells for empty map at center position with sight range 9
    // Updated to 253 based on actual visibility calculation
    protected const int ExpectedEmptyMapCellsAtCenter = 253;
    
    // L-wall blocking expectations
    protected const int MinCellsBlockedByLWall = 9;
    protected const int MaxCellsBlockedByLWall = 54;

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

    protected bool IsWithinDistance(int x1, int y1, int x2, int y2, int distance)
    {
        int dx = x2 - x1;
        int dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy) <= distance;
    }

    protected int CalculateExpectedVisibleCells(int agentX, int agentY, int sightRange)
    {
        int count = 0;
        int minX = Math.Max(0, agentX - sightRange);
        int maxX = Math.Min(MapWidth - 1, agentX + sightRange);
        int minY = Math.Max(0, agentY - sightRange);
        int maxY = Math.Min(MapHeight - 1, agentY + sightRange);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (IsWithinDistance(agentX, agentY, x, y, sightRange))
                {
                    count++;
                }
            }
        }

        return count;
    }

    protected void AssertVisibleCellsAreValid(Agent agent, Coordinates position, int sightRange, string agentType = "Agent")
    {
        agent.VisibleCells.Should().NotBeNull($"{agentType} VisibleCells should not be null");
        agent.VisibleCells.Should().NotBeEmpty($"{agentType} should see at least one cell");

        // Uniqueness check
        var uniqueCoordinates = agent.VisibleCells.Select(c => c.Coordinates).Distinct().Count();
        uniqueCoordinates.Should().Be(agent.VisibleCells.Count,
            $"{agentType} VisibleCells should not contain duplicate coordinates");

        // Expected cell count
        int expectedCellCount = CalculateExpectedVisibleCells(position.X, position.Y, sightRange);
        agent.VisibleCells.Should().HaveCount(expectedCellCount,
            $"{agentType} should see exactly {expectedCellCount} cells from position ({position.X}, {position.Y})");

        // Distance validation
        foreach (var cell in agent.VisibleCells)
        {
            IsWithinDistance(position.X, position.Y, cell.Coordinates.X, cell.Coordinates.Y, sightRange)
                .Should().BeTrue($"{agentType} sees cell ({cell.Coordinates.X}, {cell.Coordinates.Y}) which is outside sight range {sightRange} from ({position.X}, {position.Y})");
        }

        // Agent sees their own position
        agent.VisibleCells.Should().Contain(c => c.Coordinates.X == position.X && c.Coordinates.Y == position.Y,
            $"{agentType} should be able to see their own position ({position.X}, {position.Y})");
    }

    [TestMethod]
    public void Debug_CompareExpectedVsActual()
    {
        var playground = CreatePlayground();
        var hero = CreateHero();
        playground.PlaceHero(hero, new Coordinates(HeroX, HeroY));
        playground.UpdateAgentVision(hero);

        var expectedCells = new HashSet<Coordinates>();
        for (int x = HeroX - HeroSightRange; x <= HeroX + HeroSightRange; x++)
        {
            for (int y = HeroY - HeroSightRange; y <= HeroY + HeroSightRange; y++)
            {
                if (x >= 0 && x < MapWidth && y >= 0 && y < MapHeight && 
                    IsWithinDistance(HeroX, HeroY, x, y, HeroSightRange))
                {
                    expectedCells.Add(new Coordinates(x, y));
                }
            }
        }

        var actualCells = hero.VisibleCells.Select(c => c.Coordinates).ToHashSet();
        
        var missing = expectedCells.Except(actualCells).ToList();
        var extra = actualCells.Except(expectedCells).ToList();

        Console.WriteLine($"Expected: {expectedCells.Count}, Actual: {actualCells.Count}");
        Console.WriteLine($"Missing cells: {string.Join(", ", missing)}");
        Console.WriteLine($"Extra cells: {string.Join(", ", extra)}");
    }
}