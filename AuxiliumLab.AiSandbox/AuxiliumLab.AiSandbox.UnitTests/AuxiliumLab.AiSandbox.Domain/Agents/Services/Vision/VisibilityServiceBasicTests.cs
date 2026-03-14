using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using FluentAssertions;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;

[TestClass]
public class VisibilityServiceBasicTests : VisibilityServiceTestBase
{
    [DataTestMethod]
    [DataRow(0, 0, DisplayName = "TopLeftCorner")]
    [DataRow(0, 20, DisplayName = "BottomLeftCorner")]
    [DataRow(20, 0, DisplayName = "TopRightCorner")]
    [DataRow(20, 20, DisplayName = "BottomRightCorner")]
    [DataRow(5, 2, DisplayName = "Position5_2")]
    [DataRow(7, 7, DisplayName = "Position7_7")]
    [DataRow(5, 10, DisplayName = "Position5_10")]
    [DataRow(18, 10, DisplayName = "Position18_10")]
    [DataRow(18, 2, DisplayName = "Position18_2")]
    [DataRow(10, 10, DisplayName = "CenterPosition")]
    public void LookAroundEveryone_HeroAtVariousPositions_ShouldSeeExpectedArea(int x, int y)
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        var heroPosition = new Coordinates(x, y);
        playground.PlaceHero(hero, heroPosition);

        // Act
        playground.LookAroundEveryone();

        // Assert
        AssertVisibleCellsAreValid(hero, heroPosition, HeroSightRange, "Hero");
    }

    [TestMethod]
    public void UpdateVisibleCells_EmptyMap_HeroSeesAllCellsWithinRange()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        var heroPosition = new Coordinates(HeroX, HeroY);
        playground.PlaceHero(hero, heroPosition);

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        AssertVisibleCellsAreValid(hero, heroPosition, HeroSightRange, "Hero");

        // Verify cardinal directions at max range
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + HeroSightRange,
            "Hero should see north at max range");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY - HeroSightRange,
            "Hero should see south at max range");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX + HeroSightRange && c.Coordinates.Y == HeroY,
            "Hero should see east at max range");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX - HeroSightRange && c.Coordinates.Y == HeroY,
            "Hero should see west at max range");
    }

    [TestMethod]
    public void UpdateVisibleCells_MaximumRangeVerification_SeesExactlyToRange()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        var heroPosition = new Coordinates(HeroX, HeroY);
        playground.PlaceHero(hero, heroPosition);

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // Hero should see cells exactly at max range in cardinal directions
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + HeroSightRange,
            $"Hero should see north at max range ({HeroX}, {HeroY + HeroSightRange})");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY - HeroSightRange,
            $"Hero should see south at max range ({HeroX}, {HeroY - HeroSightRange})");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX + HeroSightRange && c.Coordinates.Y == HeroY,
            $"Hero should see east at max range ({HeroX + HeroSightRange}, {HeroY})");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX - HeroSightRange && c.Coordinates.Y == HeroY,
            $"Hero should see west at max range ({HeroX - HeroSightRange}, {HeroY})");

        // Hero should NOT see cells beyond max range
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + HeroSightRange + 1,
            "Hero should NOT see beyond north max range");
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX + HeroSightRange + 1 && c.Coordinates.Y == HeroY,
            "Hero should NOT see beyond east max range");

        // All visible cells should be within the circular range
        foreach (var cell in hero.VisibleCells)
        {
            double distance = Math.Sqrt(
                Math.Pow(cell.Coordinates.X - HeroX, 2) +
                Math.Pow(cell.Coordinates.Y - HeroY, 2)
            );
            distance.Should().BeLessThanOrEqualTo(HeroSightRange + 0.01, // Small epsilon for floating point
                $"Cell ({cell.Coordinates.X}, {cell.Coordinates.Y}) at distance {distance:F2} exceeds sight range {HeroSightRange}");
        }
    }

    [TestMethod]
    public void UpdateVisibleCells_EmptyMapWithExpectedCellCount_MatchesCalculation()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        var heroPosition = new Coordinates(HeroX, HeroY);
        playground.PlaceHero(hero, heroPosition);

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().HaveCount(ExpectedEmptyMapCellsAtCenter,
            $"Hero at center should see exactly {ExpectedEmptyMapCellsAtCenter} cells on empty map");
    }
}