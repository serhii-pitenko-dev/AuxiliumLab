using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using FluentAssertions;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation;

[TestClass]
public class CreateFromValuesTests
{
    [TestMethod]
    public void SetsMaxTurns()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 20, 10, 5, 2, 5, 15, 1, 4, 2);
        config.MaxTurns.Current.Should().Be(50);
    }

    [TestMethod]
    public void SetsMapWidth()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 25, 20, 10, 5, 2, 5, 15, 1, 4, 2);
        config.MapSettings.Size.Width.Current.Should().Be(25);
    }

    [TestMethod]
    public void SetsMapHeight()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 30, 10, 5, 2, 5, 15, 1, 4, 2);
        config.MapSettings.Size.Height.Current.Should().Be(30);
    }

    [TestMethod]
    public void SetsBlocksPercent()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 20, 15, 5, 2, 5, 15, 1, 4, 2);
        config.MapSettings.ElementsPercentages.BlocksPercent.Current.Should().Be(15);
    }

    [TestMethod]
    public void SetsEnemiesPercent()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 20, 10, 8, 2, 5, 15, 1, 4, 2);
        config.MapSettings.ElementsPercentages.PercentOfEnemies.Current.Should().Be(8);
    }

    [TestMethod]
    public void SetsHeroSpeed()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 20, 10, 5, 4, 5, 15, 1, 4, 2);
        config.Hero.Speed.Current.Should().Be(4);
    }

    [TestMethod]
    public void SetsHeroSightRange()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 20, 10, 5, 2, 9, 15, 1, 4, 2);
        config.Hero.SightRange.Current.Should().Be(9);
    }

    [TestMethod]
    public void SetsHeroStamina()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 20, 10, 5, 2, 5, 25, 1, 4, 2);
        config.Hero.Stamina.Current.Should().Be(25);
    }

    [TestMethod]
    public void SetsEnemySpeed()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 20, 10, 5, 2, 5, 15, 3, 4, 2);
        config.Enemy.Speed.Current.Should().Be(3);
    }

    [TestMethod]
    public void SetsEnemySightRange()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 20, 10, 5, 2, 5, 15, 1, 7, 2);
        config.Enemy.SightRange.Current.Should().Be(7);
    }

    [TestMethod]
    public void SetsEnemyStamina()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 20, 10, 5, 2, 5, 15, 1, 4, 8);
        config.Enemy.Stamina.Current.Should().Be(8);
    }

    [TestMethod]
    public void SetsAllValuesFromScreenshotDefaults()
    {
        var config = SandBoxConfiguration.CreateFromValues(
            maxTurns: 50, mapWidth: 20, mapHeight: 20,
            blocksPercent: 10.00, enemiesPercent: 0.00,
            heroSpeed: 2, heroSightRange: 5, heroStamina: 15,
            enemySpeed: 1, enemySightRange: 4, enemyStamina: 2);

        config.MaxTurns.Current.Should().Be(50);
        config.MapSettings.Size.Width.Current.Should().Be(20);
        config.MapSettings.Size.Height.Current.Should().Be(20);
        config.MapSettings.ElementsPercentages.BlocksPercent.Current.Should().Be(10);
        config.MapSettings.ElementsPercentages.PercentOfEnemies.Current.Should().Be(0);
        config.Hero.Speed.Current.Should().Be(2);
        config.Hero.SightRange.Current.Should().Be(5);
        config.Hero.Stamina.Current.Should().Be(15);
        config.Enemy.Speed.Current.Should().Be(1);
        config.Enemy.SightRange.Current.Should().Be(4);
        config.Enemy.Stamina.Current.Should().Be(2);
    }

    [TestMethod]
    public void SetsMinMaxEqualToCurrent()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 20, 10, 5, 2, 5, 15, 1, 4, 2);

        config.MaxTurns.Min.Should().Be(50);
        config.MaxTurns.Max.Should().Be(50);
        config.MapSettings.Size.Width.Min.Should().Be(20);
        config.MapSettings.Size.Width.Max.Should().Be(20);
    }

    [TestMethod]
    public void SetsStepToOne()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 20, 10, 5, 2, 5, 15, 1, 4, 2);

        config.MaxTurns.Step.Should().Be(1);
        config.Hero.Speed.Step.Should().Be(1);
    }

    [TestMethod]
    public void TruncatesBlocksPercentDecimal()
    {
        var config = SandBoxConfiguration.CreateFromValues(50, 20, 20, 10.99, 5, 2, 5, 15, 1, 4, 2);
        config.MapSettings.ElementsPercentages.BlocksPercent.Current.Should().Be(10);
    }
}
