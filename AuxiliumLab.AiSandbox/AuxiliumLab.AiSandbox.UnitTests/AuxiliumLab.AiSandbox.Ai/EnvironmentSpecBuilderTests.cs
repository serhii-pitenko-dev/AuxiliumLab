using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.PolicyTrainer;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.Ai;

/// <summary>
/// Tests for <see cref="EnvironmentSpecBuilder"/>.
/// Validates the observation-dimension formula, feature-name generation,
/// and the round-trip echo assertion.
/// </summary>
[TestClass]
public class EnvironmentSpecBuilderTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static SandBoxConfiguration MakeSettings(int sightRange) =>
        new()
        {
            Hero = new HeroConfiguration
            {
                SightRange = new IncrementalRange { Current = sightRange, Min = 1, Max = 10, Step = 1 },
                Speed      = new IncrementalRange { Current = 2, Min = 1, Max = 5,  Step = 1 },
                Stamina    = new IncrementalRange { Current = 15, Min = 5, Max = 30, Step = 5 },
            },
            Enemy = new EnemyConfiguration
            {
                SightRange = new IncrementalRange { Current = 4, Min = 1, Max = 8, Step = 1 },
                Speed      = new IncrementalRange { Current = 1, Min = 1, Max = 4, Step = 1 },
                Stamina    = new IncrementalRange { Current = 2, Min = 1, Max = 10, Step = 2 },
            },
            MapSettings  = new MapConfiguration(),
            MaxTurns     = new IncrementalRange { Current = 50, Min = 10, Max = 3000, Step = 20 },
        };

    /// <summary>Convenience overload that builds a spec using the given sight range as both config hero value and trainee value.</summary>
    private static EnvironmentSpec BuildSpec(int sightRange, string experimentId) =>
        EnvironmentSpecBuilder.Build(MakeSettings(sightRange), experimentId, sightRange);

    /// <summary>Build spec with explicit settings and sight range.</summary>
    private static EnvironmentSpec BuildSpec(SandBoxConfiguration settings, string experimentId, int traineeSightRange) =>
        EnvironmentSpecBuilder.Build(settings, experimentId, traineeSightRange);

    // -----------------------------------------------------------------------
    // ObservationDim formula
    // -----------------------------------------------------------------------

    [TestMethod]
    [DataRow(2,   30)]   // 5 + 5^2  = 30
    [DataRow(5,  126)]   // 5 + 11^2 = 126
    [DataRow(10, 446)]   // 5 + 21^2 = 446
    [DataRow(1,   14)]   // 5 + 3^2  = 14
    public void Build_ObservationDim_MatchesFormula(int sightRange, int expectedObsDim)
    {
        var spec = BuildSpec(sightRange, "exp_formula");

        spec.ObservationDim.Should().Be(expectedObsDim,
            $"obs_dim mismatch for sight_range={sightRange}");
    }

    // -----------------------------------------------------------------------
    // ActionDim
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Build_ActionDim_IsAlwaysFive()
    {
        var spec = BuildSpec(5, "exp_action");
        spec.ActionDim.Should().Be(5);
    }

    // -----------------------------------------------------------------------
    // SightRange echoed
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Build_SightRange_EchoesSettingsCurrent()
    {
        var spec = BuildSpec(7, "exp_sr");
        spec.SightRange.Should().Be(7);
    }

    // -----------------------------------------------------------------------
    // Feature names
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Build_FeatureNames_CountMatchesObsDim()
    {
        var spec = BuildSpec(5, "exp_names");
        spec.ObservationFeatureNames.Should().HaveCount(spec.ObservationDim,
            "Length of ObservationFeatureNames must equal ObservationDim.");
    }

    [TestMethod]
    public void Build_FeatureNames_FirstFiveAreScalars()
    {
        var spec = BuildSpec(5, "exp_scalars");
        string[] expectedScalars = ["x", "y", "is_run", "stamina_frac", "speed"];

        for (int i = 0; i < expectedScalars.Length; i++)
            spec.ObservationFeatureNames[i].Should().Be(expectedScalars[i],
                $"Feature name at index {i} is wrong.");
    }

    [TestMethod]
    public void Build_FeatureNames_GridCellsFollowScalars()
    {
        int sightRange = 2;           // gridSize = 5
        var spec = BuildSpec(sightRange, "exp_grid");

        // First grid cell at index 5 must be "grid_0_0"
        spec.ObservationFeatureNames[5].Should().Be("grid_0_0");

        // Last grid cell at index obsDim-1 must be "grid_4_4" for 5×5 grid
        int gridSize = 2 * sightRange + 1;
        string expectedLast = $"grid_{gridSize - 1}_{gridSize - 1}";
        spec.ObservationFeatureNames[^1].Should().Be(expectedLast);
    }

    // -----------------------------------------------------------------------
    // MaxSteps
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Build_MaxSteps_MatchesMaxTurnsCurrent()
    {
        var spec = BuildSpec(5, "exp_maxsteps");
        spec.MaxSteps.Should().Be(50, "MaxSteps must equal MaxTurns.Current from settings");
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(3000)]
    [DataRow(1)]
    public void Build_MaxSteps_VariesWithSettings(int maxTurns)
    {
        var settings = MakeSettings(5);
        settings.MaxTurns.Current = maxTurns;

        var spec = BuildSpec(settings, "exp_maxsteps_vary", 5);
        spec.MaxSteps.Should().Be(maxTurns);
    }

    // -----------------------------------------------------------------------
    // AssertEchoMatches — round-trip checks
    // -----------------------------------------------------------------------

    [TestMethod]
    public void AssertEchoMatches_IdenticalSpecs_DoesNotThrow()
    {
        var sent = BuildSpec(5, "exp_echo_ok");
        // Build another identical spec as a fake "echo"
        var echoed = BuildSpec(5, "exp_echo_ok");

        // Must not throw
        EnvironmentSpecBuilder.AssertEchoMatches(sent, echoed, "exp_echo_ok");
    }

    [TestMethod]
    public void AssertEchoMatches_MismatchedObsDim_Throws()
    {
        var sent = BuildSpec(5, "exp_mismatch");
        var echoed = BuildSpec(4, "exp_mismatch"); // different sight_range → different obs_dim

        FluentActions.Invoking(() => EnvironmentSpecBuilder.AssertEchoMatches(sent, echoed, "exp_mismatch"))
            .Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("*echo mismatch*");
    }

    [TestMethod]
    public void AssertEchoMatches_MismatchedMaxSteps_Throws()
    {
        var sent = BuildSpec(5, "exp_ms_mismatch");
        var echoed = BuildSpec(5, "exp_ms_mismatch");
        echoed.MaxSteps = sent.MaxSteps + 1; // tamper with max_steps

        FluentActions.Invoking(() => EnvironmentSpecBuilder.AssertEchoMatches(sent, echoed, "exp_ms_mismatch"))
            .Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("*echo mismatch*");
    }

    [TestMethod]
    public void AssertEchoMatches_NullSentSpec_Throws()
    {
        var echoed = BuildSpec(5, "exp");
        FluentActions.Invoking(() => EnvironmentSpecBuilder.AssertEchoMatches(null!, echoed, "exp"))
            .Should().ThrowExactly<ArgumentNullException>();
    }

    // -----------------------------------------------------------------------
    // Guard clauses
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Build_NullSettings_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => EnvironmentSpecBuilder.Build(null!, "exp", 5))
            .Should().ThrowExactly<ArgumentNullException>();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Build_EmptyOrWhiteSpaceExperimentId_ThrowsArgumentException(string experimentId)
    {
        FluentActions.Invoking(() => EnvironmentSpecBuilder.Build(MakeSettings(5), experimentId, 5))
            .Should().ThrowExactly<ArgumentException>();
    }
}
