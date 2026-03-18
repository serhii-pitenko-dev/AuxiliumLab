using AuxiliumLab.Frontend.Configuration;
using AuxiliumLab.Frontend.Features.Simulation.Pages;
using AuxiliumLab.Frontend.Features.Simulation.Services;
using Microsoft.Extensions.Options;

namespace AuxiliumLab.AiSandbox.Frontend.UnitTests.Simulation;

[TestClass]
public class VisualizationPageTests
{
    // Must match the private const in VisualizationPage.
    private const int CellSize = 16;

    /// <summary>
    /// Minimal hub double that lets tests fire typed events on demand.
    /// </summary>
    private sealed class FakeHubClient : ISimulationHubClient
    {
        public event Action<SimulationStartedDto>? OnSimulationStarted;
        public event Action<AgentMovedDto>?        OnAgentMoved;
        public event Action<AgentToggledDto>?      OnAgentToggled;
        public event Action<TurnCompletedDto>?     OnTurnCompleted;
        public event Action<SimulationEndedDto>?   OnSimulationEnded;
        public event Action<string>?               OnDebugMessage;

        public Task ConnectAsync(string jobId, CancellationToken ct = default) => Task.CompletedTask;
        public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void FireSimulationStarted(SimulationStartedDto dto) => OnSimulationStarted?.Invoke(dto);
        public void FireAgentMoved(AgentMovedDto dto)               => OnAgentMoved?.Invoke(dto);
        public void FireTurnCompleted(TurnCompletedDto dto)         => OnTurnCompleted?.Invoke(dto);
    }

    private static (TestContext ctx, FakeHubClient hub) BuildCtx()
    {
        var ctx = new TestContext();
        ctx.SetupWithMudServices();

        var hub = new FakeHubClient();
        ctx.Services.AddSingleton<ISimulationHubClient>(hub);
        ctx.Services.AddSingleton(new Mock<ISimulationApiClient>().Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);
        ctx.Services.AddSingleton<IOptions<SandboxSettings>>(Options.Create(new SandboxSettings()));

        return (ctx, hub);
    }

    private static SimulationStartedDto MakeStartedWithAgent(string agentId, int x, int y) => new()
    {
        Width    = 10,
        Height   = 10,
        MaxTurns = 100,
        Cells    = [],
        Agents   =
        [
            new InitialAgentDto
            {
                AgentId   = agentId,
                AgentType = "Hero",
                X         = x,
                Y         = y
            }
        ]
    };

    /// <summary>Creates a cell DTO; defaults to Empty with no effects.</summary>
    private static SimulationCellDto MakeCell(
        int x, int y,
        string objectType = "Empty",
        string[]? effects = null) =>
        new() { X = x, Y = y, ObjectType = objectType, Effects = effects ?? [] };

    /// <summary>Builds a minimal SimulationStartedDto with the given cells and no agents.</summary>
    private static SimulationStartedDto MakeStartedWithCells(params SimulationCellDto[] cells) => new()
    {
        Width    = 10,
        Height   = 10,
        MaxTurns = 100,
        Cells    = cells,
        Agents   = []
    };

    // ── Tests ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void HandleAgentMoved_WhenIsSuccessTrue_MovesAgentCircleToNewCell()
    {
        var (ctx, hub) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();

            hub.FireSimulationStarted(MakeStartedWithAgent("agent-01", x: 2, y: 3));
            cut.WaitForAssertion(() => cut.Markup.Should().Contain("<svg"));

            hub.FireAgentMoved(new AgentMovedDto
            {
                AgentId   = "agent-01",
                AgentType = "Hero",
                FromX     = 2, FromY = 3,
                ToX       = 4, ToY   = 5,
                IsSuccess = true
            });

            // Circle should have moved to (4,5) — cx = 4*16+8 = 72, cy = 5*16+8 = 88
            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain($"cx=\"{4 * CellSize + CellSize / 2}\""));
            cut.Markup.Should().Contain($"cy=\"{5 * CellSize + CellSize / 2}\"");
        }
    }

    [TestMethod]
    public void HandleAgentMoved_WhenIsSuccessFalse_KeepsAgentCircleAtOriginalCell()
    {
        var (ctx, hub) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();

            hub.FireSimulationStarted(MakeStartedWithAgent("agent-01", x: 2, y: 3));
            cut.WaitForAssertion(() => cut.Markup.Should().Contain("<svg"));

            hub.FireAgentMoved(new AgentMovedDto
            {
                AgentId   = "agent-01",
                AgentType = "Hero",
                FromX     = 2, FromY = 3,
                ToX       = 4, ToY   = 5,
                IsSuccess = false
            });

            // Circle must stay at (2,3) — cx = 2*16+8 = 40, cy = 3*16+8 = 56
            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain($"cx=\"{2 * CellSize + CellSize / 2}\""));
            cut.Markup.Should().Contain($"cy=\"{3 * CellSize + CellSize / 2}\"");

            // Must NOT have moved to (4,5) — cx = 72
            cut.Markup.Should().NotContain($"cx=\"{4 * CellSize + CellSize / 2}\"");
        }
    }

    [TestMethod]
    public void HandleAgentMoved_WhenIsSuccessFalse_StillAppearsInEventLog()
    {
        var (ctx, hub) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();

            hub.FireSimulationStarted(MakeStartedWithAgent("agent-01", x: 2, y: 3));

            hub.FireAgentMoved(new AgentMovedDto
            {
                AgentId   = "agent-01",
                AgentType = "Hero",
                FromX     = 2, FromY = 3,
                ToX       = 4, ToY   = 5,
                IsSuccess = false
            });

            // The event log should record the move regardless of success
            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("AgentMoved"));
        }
    }

    [TestMethod]
    public void HandleAgentMoved_WhenIsSuccessFalse_StillUpdatesAgentSnapshot()
    {
        var (ctx, hub) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();

            hub.FireSimulationStarted(MakeStartedWithAgent("agent-01", x: 2, y: 3));
            cut.WaitForAssertion(() => cut.Markup.Should().Contain("<svg"));

            // _agents[agentId] is always updated; any exception from HandleAgentMoved would
            // be caught and logged instead — verify the component doesn't log an error.
            hub.FireAgentMoved(new AgentMovedDto
            {
                AgentId   = "agent-01",
                AgentType = "Hero",
                FromX     = 2, FromY = 3,
                ToX       = 4, ToY   = 5,
                IsSuccess = false,
                Agent     = new AgentSnapshotDto { Stamina = 80, MaxStamina = 100 }
            });

            cut.WaitForAssertion(() =>
                cut.Markup.Should().NotContain("HandleAgentMoved EXCEPTION"));
        }
    }

    // ── Effect overlay rendering tests ──────────────────────────────────────

    [TestMethod]
    public void HeroPathCell_IsRenderedWithHeroPathClass()
    {
        var (ctx, hub) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();

            hub.FireSimulationStarted(MakeStartedWithCells(
                MakeCell(3, 4, effects: ["Hero:Path"])));

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"hero-path\""));
        }
    }

    [TestMethod]
    public void EnemyPathCell_IsRenderedWithEnemyPathClass()
    {
        var (ctx, hub) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();

            hub.FireSimulationStarted(MakeStartedWithCells(
                MakeCell(5, 2, effects: ["Enemy:Path"])));

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"enemy-path\""));
        }
    }

    [TestMethod]
    public void HeroVisionCell_IsRenderedWithHeroVisionClass()
    {
        var (ctx, hub) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();

            hub.FireSimulationStarted(MakeStartedWithCells(
                MakeCell(1, 1, effects: ["Hero:Vision"])));

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"hero-vision\""));
        }
    }

    [TestMethod]
    public void EnemyVisionCell_IsRenderedWithEnemyVisionClass()
    {
        var (ctx, hub) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();

            hub.FireSimulationStarted(MakeStartedWithCells(
                MakeCell(7, 7, effects: ["Enemy:Vision"])));

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"enemy-vision\""));
        }
    }

    [TestMethod]
    public void HeroPath_TakesPriorityOverEnemyPath_WhenBothEffectsPresent()
    {
        var (ctx, hub) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();

            hub.FireSimulationStarted(MakeStartedWithCells(
                MakeCell(4, 4, effects: ["Hero:Path", "Enemy:Path"])));

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"hero-path\""));
            cut.Markup.Should().NotContain("class=\"enemy-path\"");
        }
    }

    [TestMethod]
    public void HeroPath_TakesPriorityOverHeroVision_WhenBothEffectsPresent()
    {
        var (ctx, hub) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();

            hub.FireSimulationStarted(MakeStartedWithCells(
                MakeCell(2, 6, effects: ["Hero:Vision", "Hero:Path"])));

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"hero-path\""));
            cut.Markup.Should().NotContain("class=\"hero-vision\"");
        }
    }

    [TestMethod]
    public void BlockCell_WithEffects_DoesNotRenderEffectOverlay()
    {
        var (ctx, hub) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();

            // Only cell is a Block — effect overlay must be suppressed.
            hub.FireSimulationStarted(MakeStartedWithCells(
                MakeCell(3, 3, objectType: "Block", effects: ["Hero:Path"])));

            cut.WaitForAssertion(() => cut.Markup.Should().Contain("<svg"));
            cut.Markup.Should().NotContain("class=\"hero-path\"");
        }
    }

    [TestMethod]
    public void TurnCompleted_WithEffectCells_RendersEffectOverlays()
    {
        var (ctx, hub) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();

            // Start with a plain empty cell.
            hub.FireSimulationStarted(MakeStartedWithCells(
                MakeCell(0, 0)));
            cut.WaitForAssertion(() => cut.Markup.Should().Contain("<svg"));

            // Turn update replaces the cell with an enemy-vision effect.
            hub.FireTurnCompleted(new TurnCompletedDto
            {
                TurnNumber   = 1,
                UpdatedCells = [MakeCell(0, 0, effects: ["Enemy:Vision"])]
            });

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"enemy-vision\""));
        }
    }
}
