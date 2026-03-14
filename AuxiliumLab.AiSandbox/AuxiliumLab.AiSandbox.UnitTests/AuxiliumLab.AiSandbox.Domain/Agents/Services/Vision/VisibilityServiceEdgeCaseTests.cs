using AuxiliumLab.AiSandbox.Domain.InanimateObjects;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using FluentAssertions;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;

[TestClass]
public class VisibilityServiceEdgeCaseTests : VisibilityServiceTestBase
{
    [TestMethod]
    public void UpdateVisibleCells_ZeroSightRange_SeesOnlySelf()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero(sightRange: 0);
        var heroPosition = new Coordinates(HeroX, HeroY);
        playground.PlaceHero(hero, heroPosition);

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();
        hero.VisibleCells.Should().HaveCount(1, "Hero with zero sight range should see only their own cell");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY,
            "Hero should see their own position");
    }

    [TestMethod]
    public void UpdateVisibleCells_MinimalSightRange_SeesImmediateNeighbors()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero(sightRange: 1);
        var heroPosition = new Coordinates(HeroX, HeroY);
        playground.PlaceHero(hero, heroPosition);

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // Calculate expected cells using the same formula as the visibility service
        int expectedCells = CalculateExpectedVisibleCells(HeroX, HeroY, 1);
        hero.VisibleCells.Should().HaveCount(expectedCells,
            "Hero with sight range 1 should see calculated number of cells");

        // Verify hero sees themselves
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY,
            "Hero should see their own position");

        // With Euclidean distance and sight range 1:
        // - Cardinal neighbors (distance = 1.0): visible ✓
        // - Diagonal neighbors (distance = √2 ≈ 1.414): NOT visible ✗

        // Verify cardinal directions are visible (distance = 1.0)
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY - 1,
            "Hero should see north neighbor (distance 1.0)");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 1,
            "Hero should see south neighbor (distance 1.0)");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX - 1 && c.Coordinates.Y == HeroY,
            "Hero should see west neighbor (distance 1.0)");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX + 1 && c.Coordinates.Y == HeroY,
            "Hero should see east neighbor (distance 1.0)");

        // Verify diagonal neighbors are NOT visible (distance = √2 ≈ 1.414 > 1.0)
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX - 1 && c.Coordinates.Y == HeroY - 1,
            "Hero should NOT see northwest diagonal (distance √2 ≈ 1.414)");
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX + 1 && c.Coordinates.Y == HeroY - 1,
            "Hero should NOT see northeast diagonal (distance √2 ≈ 1.414)");
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX - 1 && c.Coordinates.Y == HeroY + 1,
            "Hero should NOT see southwest diagonal (distance √2 ≈ 1.414)");
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX + 1 && c.Coordinates.Y == HeroY + 1,
            "Hero should NOT see southeast diagonal (distance √2 ≈ 1.414)");

        // Expected cells: self (1) + 4 cardinal neighbors (4) = 5 total
        hero.VisibleCells.Should().HaveCount(5,
            "Hero with sight range 1 should see exactly 5 cells (self + 4 cardinal directions)");
    }

    [DataTestMethod]
    [DataRow(0, 10, 9, DisplayName = "LeftEdge")]
    [DataRow(20, 10, 9, DisplayName = "RightEdge")]
    [DataRow(10, 0, 9, DisplayName = "TopEdge")]
    [DataRow(10, 20, 9, DisplayName = "BottomEdge")]
    public void UpdateVisibleCells_AgentAtMapEdge_HandlesMapBoundaryCorrectly(int x, int y, int sightRange)
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero(sightRange: sightRange);
        var heroPosition = new Coordinates(x, y);
        playground.PlaceHero(hero, heroPosition);

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        AssertVisibleCellsAreValid(hero, heroPosition, sightRange, "Hero");

        // All cells should be within map boundaries
        foreach (var cell in hero.VisibleCells)
        {
            cell.Coordinates.X.Should().BeInRange(0, MapWidth - 1,
                $"Cell X={cell.Coordinates.X} is outside map width [0..{MapWidth - 1}]");
            cell.Coordinates.Y.Should().BeInRange(0, MapHeight - 1,
                $"Cell Y={cell.Coordinates.Y} is outside map height [0..{MapHeight - 1}]");
        }
    }

    [TestMethod]
    public void UpdateVisibleCells_AgentCompletelyEnclosed_SeesOnlyEnclosedArea()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        playground.PlaceHero(hero, new Coordinates(HeroX, HeroY));

        // Create complete enclosure - 3x3 box around hero
        int radius = 1;
        for (int x = HeroX - radius - 1; x <= HeroX + radius + 1; x++)
        {
            playground.AddBlock(new Block(Guid.NewGuid()), new Coordinates(x, HeroY - radius - 1));
            playground.AddBlock(new Block(Guid.NewGuid()), new Coordinates(x, HeroY + radius + 1));
        }
        for (int y = HeroY - radius; y <= HeroY + radius; y++)
        {
            playground.AddBlock(new Block(Guid.NewGuid()), new Coordinates(HeroX - radius - 1, y));
            playground.AddBlock(new Block(Guid.NewGuid()), new Coordinates(HeroX + radius + 1, y));
        }

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // Hero should only see inside the 3x3 area (9 cells) plus the walls
        int innerCells = (2 * radius + 1) * (2 * radius + 1); // 3x3 = 9

        // Hero should see at least the inner area
        hero.VisibleCells.Count.Should().BeGreaterThanOrEqualTo(innerCells,
            $"Hero should see at least {innerCells} cells in enclosed area");

        // Hero should NOT see beyond the walls
        hero.VisibleCells.Should().NotContain(c =>
            c.Coordinates.X < HeroX - radius - 1 ||
            c.Coordinates.X > HeroX + radius + 1 ||
            c.Coordinates.Y < HeroY - radius - 1 ||
            c.Coordinates.Y > HeroY + radius + 1,
            "Hero should NOT see beyond enclosing walls");
    }

    [TestMethod]
    public void UpdateVisibleCells_LargeSightRange_DoesNotExceedMapBoundaries()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero(sightRange: 50); // Larger than map
        var heroPosition = new Coordinates(HeroX, HeroY);
        playground.PlaceHero(hero, heroPosition);

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // All cells should be within map
        foreach (var cell in hero.VisibleCells)
        {
            cell.Coordinates.X.Should().BeInRange(0, MapWidth - 1);
            cell.Coordinates.Y.Should().BeInRange(0, MapHeight - 1);
        }

        // Hero should see entire map (21x21 = 441 cells)
        int totalMapCells = MapWidth * MapHeight;
        hero.VisibleCells.Should().HaveCount(totalMapCells,
            $"Hero with very large sight range should see entire map ({totalMapCells} cells)");
    }

    [TestMethod]
    public void UpdateVisibleCells_SightRangeTwoIncludesDiagonals_SeesAllNeighborsWithinCircle()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero(sightRange: 2);
        var heroPosition = new Coordinates(HeroX, HeroY);
        playground.PlaceHero(hero, heroPosition);

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // With sight range 2, diagonal neighbors (√2 ≈ 1.414) are now visible
        // Verify all immediate diagonal neighbors are visible
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX - 1 && c.Coordinates.Y == HeroY - 1,
            "Hero should see northwest diagonal with sight range 2");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX + 1 && c.Coordinates.Y == HeroY - 1,
            "Hero should see northeast diagonal with sight range 2");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX - 1 && c.Coordinates.Y == HeroY + 1,
            "Hero should see southwest diagonal with sight range 2");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX + 1 && c.Coordinates.Y == HeroY + 1,
            "Hero should see southeast diagonal with sight range 2");

        // Verify cardinal neighbors at distance 2
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY - 2,
            "Hero should see 2 cells north");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 2,
            "Hero should see 2 cells south");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX - 2 && c.Coordinates.Y == HeroY,
            "Hero should see 2 cells west");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX + 2 && c.Coordinates.Y == HeroY,
            "Hero should see 2 cells east");
    }
}