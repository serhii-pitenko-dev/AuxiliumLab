using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.SharedContracts;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation;

[TestClass]
public class ApplySandboxOverridesTests
{
    private IOptions<SandBoxConfiguration> _baseConfig = null!;

    [TestInitialize]
    public void Setup()
    {
        _baseConfig = Options.Create(new SandBoxConfiguration
        {
            MaxTurns = new IncrementalRange { Min = 1, Current = 100, Max = 500, Step = 10 },
            MapSettings = new MapConfiguration
            {
                Size = new Size
                {
                    Width  = new IncrementalRange { Min = 5, Current = 30, Max = 50, Step = 1 },
                    Height = new IncrementalRange { Min = 5, Current = 30, Max = 50, Step = 1 }
                },
                ElementsPercentages = new ElementsPercentages
                {
                    BlocksPercent    = new IncrementalRange { Min = 0, Current = 5, Max = 40, Step = 1 },
                    PercentOfEnemies = new IncrementalRange { Min = 0, Current = 3, Max = 20, Step = 1 }
                }
            },
            Hero = new HeroConfiguration
            {
                Speed      = new IncrementalRange { Min = 1, Current = 3, Max = 10, Step = 1 },
                SightRange = new IncrementalRange { Min = 1, Current = 7, Max = 20, Step = 1 },
                Stamina    = new IncrementalRange { Min = 1, Current = 20, Max = 50, Step = 1 }
            },
            Enemy = new EnemyConfiguration
            {
                Speed = new IncrementalRange { Min = 1, Current = 2, Max = 10, Step = 1 }
            }
        });
    }

    [TestMethod]
    public void ReturnsNull_WhenOverridesIsNull()
    {
        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, null);

        result.Should().BeNull();
    }

    [TestMethod]
    public void AppliesMaxTurns()
    {
        var overrides = new SimulationSandboxOverrideDto { MaxTurns = 50 };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        result.MaxTurns.Current.Should().Be(50);
    }

    [TestMethod]
    public void AppliesMapWidth()
    {
        var overrides = new SimulationSandboxOverrideDto { MapWidth = 20 };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        result.MapSettings.Size.Width.Current.Should().Be(20);
    }

    [TestMethod]
    public void AppliesMapHeight()
    {
        var overrides = new SimulationSandboxOverrideDto { MapHeight = 20 };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        result.MapSettings.Size.Height.Current.Should().Be(20);
    }

    [TestMethod]
    public void AppliesBlocksPercent()
    {
        var overrides = new SimulationSandboxOverrideDto { BlocksPercent = 10.0 };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        result.MapSettings.ElementsPercentages.BlocksPercent.Current.Should().Be(10);
    }

    [TestMethod]
    public void AppliesEnemiesPercent()
    {
        var overrides = new SimulationSandboxOverrideDto { EnemiesPercent = 5.0 };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        result.MapSettings.ElementsPercentages.PercentOfEnemies.Current.Should().Be(5);
    }

    [TestMethod]
    public void AppliesHeroSpeed()
    {
        var overrides = new SimulationSandboxOverrideDto { HeroSpeed = 2 };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        result.Hero.Speed.Current.Should().Be(2);
    }

    [TestMethod]
    public void AppliesHeroSightRange()
    {
        var overrides = new SimulationSandboxOverrideDto { HeroSightRange = 5 };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        result.Hero.SightRange.Current.Should().Be(5);
    }

    [TestMethod]
    public void AppliesHeroStamina()
    {
        var overrides = new SimulationSandboxOverrideDto { HeroStamina = 15 };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        result.Hero.Stamina.Current.Should().Be(15);
    }

    [TestMethod]
    public void AppliesEnemySpeed()
    {
        var overrides = new SimulationSandboxOverrideDto { EnemySpeed = 1 };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        result.Enemy.Speed.Current.Should().Be(1);
    }

    [TestMethod]
    public void AppliesAllOverridesFromScreenshot()
    {
        // Matches the exact values from the UI screenshot
        var overrides = new SimulationSandboxOverrideDto
        {
            MaxTurns       = 50,
            MapWidth       = 20,
            MapHeight      = 20,
            BlocksPercent  = 10.00,
            EnemiesPercent = 0.00,
            HeroSpeed      = 2,
            HeroSightRange = 5,
            HeroStamina    = 15,
            EnemySpeed     = 1
        };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        result.MaxTurns.Current.Should().Be(50);
        result.MapSettings.Size.Width.Current.Should().Be(20);
        result.MapSettings.Size.Height.Current.Should().Be(20);
        result.MapSettings.ElementsPercentages.BlocksPercent.Current.Should().Be(10);
        result.MapSettings.ElementsPercentages.PercentOfEnemies.Current.Should().Be(0);
        result.Hero.Speed.Current.Should().Be(2);
        result.Hero.SightRange.Current.Should().Be(5);
        result.Hero.Stamina.Current.Should().Be(15);
        result.Enemy.Speed.Current.Should().Be(1);
    }

    [TestMethod]
    public void DoesNotModifyOriginalConfig()
    {
        var overrides = new SimulationSandboxOverrideDto
        {
            MaxTurns      = 50,
            MapWidth      = 20,
            BlocksPercent = 10.0,
            HeroSpeed     = 2
        };

        SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides);

        _baseConfig.Value.MaxTurns.Current.Should().Be(100);
        _baseConfig.Value.MapSettings.Size.Width.Current.Should().Be(30);
        _baseConfig.Value.MapSettings.ElementsPercentages.BlocksPercent.Current.Should().Be(5);
        _baseConfig.Value.Hero.Speed.Current.Should().Be(3);
    }

    [TestMethod]
    public void PreservesNonOverriddenValues()
    {
        // Only override one field — all others should keep their base values
        var overrides = new SimulationSandboxOverrideDto { MaxTurns = 50 };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        result.MapSettings.Size.Width.Current.Should().Be(30);
        result.MapSettings.Size.Height.Current.Should().Be(30);
        result.MapSettings.ElementsPercentages.BlocksPercent.Current.Should().Be(5);
        result.MapSettings.ElementsPercentages.PercentOfEnemies.Current.Should().Be(3);
        result.Hero.Speed.Current.Should().Be(3);
        result.Hero.SightRange.Current.Should().Be(7);
        result.Hero.Stamina.Current.Should().Be(20);
        result.Enemy.Speed.Current.Should().Be(2);
    }

    [TestMethod]
    public void PreservesIncrementalRangeBounds_WhenOverridingCurrent()
    {
        var overrides = new SimulationSandboxOverrideDto { BlocksPercent = 10.0 };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        var blocksRange = result.MapSettings.ElementsPercentages.BlocksPercent;
        blocksRange.Current.Should().Be(10);
        blocksRange.Min.Should().Be(0, "Min should be preserved from the base config");
        blocksRange.Max.Should().Be(40, "Max should be preserved from the base config");
        blocksRange.Step.Should().Be(1, "Step should be preserved from the base config");
    }

    [TestMethod]
    public void TruncatesBlocksPercentDecimal()
    {
        var overrides = new SimulationSandboxOverrideDto { BlocksPercent = 10.99 };

        var result = SimulationCommandService.ApplySandboxOverrides(_baseConfig, overrides)!;

        result.MapSettings.ElementsPercentages.BlocksPercent.Current.Should().Be(10);
    }
}
