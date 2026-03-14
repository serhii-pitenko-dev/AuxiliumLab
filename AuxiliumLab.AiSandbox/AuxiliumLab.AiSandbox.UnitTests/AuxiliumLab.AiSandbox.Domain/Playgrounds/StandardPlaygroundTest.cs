using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using FluentAssertions;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.Domain.Playgrounds;

[TestClass]
public class StandardPlaygroundTest
{
    private const int MapWidth = 21;
    private const int MapHeight = 14;
    private const int HeroSpeed = 5;
    private const int HeroSightRange = 6;
    private const int HeroStamina = 15;

    private StandardPlayground CreatePlayground()
    {
        var map = new MapSquareCells(MapWidth, MapHeight);
        var visibilityService = new VisibilityService();
        return new StandardPlayground(map, visibilityService);
    }

    private Hero CreateHero()
    {
        var characters = new InitialAgentCharacters(
            Speed: HeroSpeed,
            SightRange: HeroSightRange,
            Stamina: HeroStamina,
            PathToTarget: [],
            AgentActions: [],
            ExecutedActions: [],
            isRun: false,
            orderInTurnQueue: 0
        );
        return new Hero(characters, Guid.NewGuid());
    }

    [TestMethod]
    public void LookAroundEveryone_HeroPlaced_UpdatesVisibleCells()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        var heroPosition = new Coordinates(10, 7);
        playground.PlaceHero(hero, heroPosition);

        // Act
        playground.LookAroundEveryone();

        // Assert
        hero.VisibleCells.Should().NotBeNull();
        hero.VisibleCells.Should().NotBeEmpty();

        // Verify hero can see their own position
        hero.VisibleCells.Should().Contain(c =>
            c.Coordinates.X == heroPosition.X &&
            c.Coordinates.Y == heroPosition.Y,
            "Hero should be able to see their own position");

        // Verify all visible cells are within sight range
        foreach (var cell in hero.VisibleCells)
        {
            var distance = Math.Sqrt(
                Math.Pow(cell.Coordinates.X - heroPosition.X, 2) +
                Math.Pow(cell.Coordinates.Y - heroPosition.Y, 2));
            distance.Should().BeLessThanOrEqualTo(HeroSightRange + 0.01,
                $"Cell ({cell.Coordinates.X}, {cell.Coordinates.Y}) at distance {distance:F2} exceeds sight range {HeroSightRange}");
        }
    }

    [TestMethod]
    public void LookAroundEveryone_MultipleAgents_UpdatesAllVisibleCells()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        var enemy = new Enemy(new InitialAgentCharacters(
            Speed: 3,
            SightRange: 4,
            Stamina: 10,
            PathToTarget: [],
            AgentActions: [],
            ExecutedActions: [],
            isRun: false,
            orderInTurnQueue: 0
        ), Guid.NewGuid());

        playground.PlaceHero(hero, new Coordinates(5, 5));
        playground.PlaceEnemy(enemy, new Coordinates(15, 8));

        // Act
        playground.LookAroundEveryone();

        // Assert
        hero.VisibleCells.Should().NotBeNull();
        hero.VisibleCells.Should().NotBeEmpty();
        enemy.VisibleCells.Should().NotBeNull();
        enemy.VisibleCells.Should().NotBeEmpty();

        // Verify hero sees their position
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == 5 && c.Coordinates.Y == 5,
            "Hero should see their own position");

        // Verify enemy sees their position
        enemy.VisibleCells.Should().Contain(c => c.Coordinates.X == 15 && c.Coordinates.Y == 8,
            "Enemy should see their own position");
    }
}