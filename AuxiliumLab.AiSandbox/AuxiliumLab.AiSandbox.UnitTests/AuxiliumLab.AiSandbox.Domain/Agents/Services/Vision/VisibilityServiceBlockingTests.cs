using AuxiliumLab.AiSandbox.Domain.InanimateObjects;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using FluentAssertions;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;

[TestClass]
public class VisibilityServiceBlockingTests : VisibilityServiceTestBase
{
    [TestMethod]
    public void UpdateVisibleCells_SingleBlockDirectlyNorth_BlocksShadowBehind()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        playground.PlaceHero(hero, new Coordinates(HeroX, HeroY));

        var block = new Block(Guid.NewGuid());
        playground.AddBlock(block, new Coordinates(HeroX, HeroY + 1));

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // Hero can see the block itself
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 1,
            "Hero should see the blocking cell at (10, 11)");

        // Hero cannot see cells directly behind the block
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 2,
            "Hero should NOT see cell directly behind block at (10, 12)");
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 5,
            "Hero should NOT see cell behind block at (10, 15)");

        // Hero CAN see cells diagonally adjacent to their position (not blocked)
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX + 1 && c.Coordinates.Y == HeroY + 1,
            "Hero should see diagonal adjacent cell (11, 11)");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX - 1 && c.Coordinates.Y == HeroY + 1,
            "Hero should see diagonal adjacent cell (9, 11)");

        // Hero CANNOT see cells that Bresenham line passes through the block to reach
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX + 1 && c.Coordinates.Y == HeroY + 2,
            "Hero should NOT see (11, 12) - line passes through blocking cell");
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX - 1 && c.Coordinates.Y == HeroY + 2,
            "Hero should NOT see (9, 12) - line passes through blocking cell");

        // Hero CAN see cells at diagonal angles that don't pass through the block
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX + 2 && c.Coordinates.Y == HeroY + 1,
            "Hero should see (12, 11) - line doesn't pass through block");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX - 2 && c.Coordinates.Y == HeroY + 1,
            "Hero should see (8, 11) - line doesn't pass through block");
    }

    [TestMethod]
    public void UpdateVisibleCells_HorizontalWall_CreatesShadowBehind()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        playground.PlaceHero(hero, new Coordinates(HeroX, HeroY));

        int wallY = HeroY + 3;
        for (int x = HeroX - 3; x <= HeroX + 3; x++)
        {
            var block = new Block(Guid.NewGuid());
            playground.AddBlock(block, new Coordinates(x, wallY));
        }

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // Hero can see the wall
        for (int x = HeroX - 3; x <= HeroX + 3; x++)
        {
            hero.VisibleCells.Should().Contain(c => c.Coordinates.X == x && c.Coordinates.Y == wallY,
                $"Hero should see wall at ({x}, {wallY})");
        }

        // Hero cannot see cells directly behind the wall center
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == wallY + 1,
            $"Hero should NOT see behind wall at ({HeroX}, {wallY + 1})");
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == wallY + 3,
            $"Hero should NOT see behind wall at ({HeroX}, {wallY + 3})");

        // Hero can see cells outside wall shadow
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X < HeroX - 3 || c.Coordinates.X > HeroX + 3,
            "Hero should see cells outside wall shadow");
    }

    [TestMethod]
    public void UpdateVisibleCells_DiagonalBlock_BlocksDiagonalLine()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        playground.PlaceHero(hero, new Coordinates(HeroX, HeroY));

        var block = new Block(Guid.NewGuid());
        playground.AddBlock(block, new Coordinates(HeroX + 4, HeroY + 4));

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // Hero can see the block
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX + 4 && c.Coordinates.Y == HeroY + 4,
            "Hero should see the diagonal block");

        // Hero cannot see cells on the same diagonal beyond the block
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX + 5 && c.Coordinates.Y == HeroY + 5,
            "Hero should NOT see (15, 15) behind diagonal block");
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX + 6 && c.Coordinates.Y == HeroY + 6,
            "Hero should NOT see (16, 16) behind diagonal block");

        // Hero can see adjacent cells not on the blocked diagonal
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX + 4 && c.Coordinates.Y == HeroY + 5,
            "Hero should see adjacent cell (14, 15)");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX + 5 && c.Coordinates.Y == HeroY + 4,
            "Hero should see adjacent cell (15, 14)");
    }

    [TestMethod]
    public void UpdateVisibleCells_BlockBetweenHeroAndEdge_BlocksCellsBehind()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        playground.PlaceHero(hero, new Coordinates(HeroX, HeroY));

        var block = new Block(Guid.NewGuid());
        playground.AddBlock(block, new Coordinates(HeroX - 5, HeroY));

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // Hero can see the block
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX - 5 && c.Coordinates.Y == HeroY,
            "Hero should see the block at (5, 10)");

        // Hero cannot see cells behind the block toward the edge
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX - 6 && c.Coordinates.Y == HeroY,
            "Hero should NOT see (4, 10) behind block");
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX - 8 && c.Coordinates.Y == HeroY,
            "Hero should NOT see (2, 10) behind block");
    }

    [TestMethod]
    public void UpdateVisibleCells_EnemyDoesNotBlock_AgentSeesThrough()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        playground.PlaceHero(hero, new Coordinates(HeroX, HeroY));

        var enemy = CreateEnemy();
        playground.PlaceEnemy(enemy, new Coordinates(HeroX, HeroY + 5));

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // Hero can see the enemy
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 5,
            "Hero should see the enemy at (10, 15)");

        // Hero CAN see cells behind the enemy (agents are transparent)
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 6,
            "Hero SHOULD see (10, 16) behind enemy - agents don't block vision");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 8,
            "Hero SHOULD see (10, 18) behind enemy - agents don't block vision");
    }

    [TestMethod]
    public void UpdateVisibleCells_MultipleScatteredBlocks_CreatesMultipleShadows()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        playground.PlaceHero(hero, new Coordinates(HeroX, HeroY));

        var blockPositions = new[]
        {
            new Coordinates(HeroX - 2, HeroY + 2),
            new Coordinates(HeroX + 2, HeroY + 4),
            new Coordinates(HeroX + 5, HeroY),
            new Coordinates(HeroX, HeroY - 4)
        };

        foreach (var pos in blockPositions)
        {
            var block = new Block(Guid.NewGuid());
            playground.AddBlock(block, pos);
        }

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // Hero can see all blocks
        foreach (var pos in blockPositions)
        {
            hero.VisibleCells.Should().Contain(c => c.Coordinates.X == pos.X && c.Coordinates.Y == pos.Y,
                $"Hero should see block at ({pos.X}, {pos.Y})");
        }

        // Check shadows behind blocks
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX + 6 && c.Coordinates.Y == HeroY,
            "Hero should NOT see (16, 10) behind block at (15, 10)");
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY - 5,
            "Hero should NOT see (10, 5) behind block at (10, 6)");
    }

    [TestMethod]
    public void UpdateVisibleCells_RingOfBlocks_HeroSeesOnlyInside()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        playground.PlaceHero(hero, new Coordinates(HeroX, HeroY));

        int ringRadius = 2;
        for (int x = HeroX - ringRadius; x <= HeroX + ringRadius; x++)
        {
            playground.AddBlock(new Block(Guid.NewGuid()), new Coordinates(x, HeroY - ringRadius));
            playground.AddBlock(new Block(Guid.NewGuid()), new Coordinates(x, HeroY + ringRadius));
        }
        for (int y = HeroY - ringRadius + 1; y <= HeroY + ringRadius - 1; y++)
        {
            playground.AddBlock(new Block(Guid.NewGuid()), new Coordinates(HeroX - ringRadius, y));
            playground.AddBlock(new Block(Guid.NewGuid()), new Coordinates(HeroX + ringRadius, y));
        }

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // Hero should see inside the ring
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY,
            "Hero should see their own position");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX + 1 && c.Coordinates.Y == HeroY,
            "Hero should see inside ring");

        // Hero should see the ring itself
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + ringRadius,
            "Hero should see the ring");

        // Hero should NOT see cells outside the ring
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + ringRadius + 1,
            "Hero should NOT see outside ring at (10, 13)");
        hero.VisibleCells.Should().NotContain(c => c.Coordinates.X == HeroX + ringRadius + 1 && c.Coordinates.Y == HeroY,
            "Hero should NOT see outside ring at (13, 10)");

        // Verify total visible cells is limited to inner area + ring
        int expectedMaxCells = (ringRadius * 2 + 1) * (ringRadius * 2 + 1);
        hero.VisibleCells.Count.Should().BeLessThanOrEqualTo(expectedMaxCells,
            $"Hero should see at most {expectedMaxCells} cells (inside + ring)");
    }

    [TestMethod]
    public void UpdateVisibleCells_MultipleAgentsInLine_AllAreVisible()
    {
        // Arrange
        var playground = CreatePlayground();
        var hero = CreateHero();
        playground.PlaceHero(hero, new Coordinates(HeroX, HeroY));

        // Place multiple enemies in a line (north of hero)
        var enemy1 = CreateEnemy();
        var enemy2 = CreateEnemy();
        var enemy3 = CreateEnemy();
        playground.PlaceEnemy(enemy1, new Coordinates(HeroX, HeroY + 2));
        playground.PlaceEnemy(enemy2, new Coordinates(HeroX, HeroY + 4));
        playground.PlaceEnemy(enemy3, new Coordinates(HeroX, HeroY + 6));

        // Act
        playground.UpdateAgentVision(hero);

        // Assert
        hero.VisibleCells.Should().NotBeNull();

        // Hero can see all enemies (they don't block each other)
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 2,
            "Hero should see first enemy");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 4,
            "Hero should see second enemy (not blocked by first)");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 6,
            "Hero should see third enemy (not blocked by others)");

        // Hero can see cells behind all enemies
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 7,
            "Hero should see cell behind all enemies");
        hero.VisibleCells.Should().Contain(c => c.Coordinates.X == HeroX && c.Coordinates.Y == HeroY + 9,
            "Hero should see cell at max range behind enemies");
    }
}