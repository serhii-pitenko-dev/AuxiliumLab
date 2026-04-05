using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using AuxiliumLab.AiSandbox.SharedContracts;
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

    private static SandboxSettings MakeSandboxSettings() => new()
    {
        MaxTurns = new RangedValue { Min = 10, Current = 100, Max = 3000, Step = 20 },
        MapSettings = new MapSettingsConfig
        {
            Size = new MapSizeConfig
            {
                Width  = new RangedValue { Min = 5, Current = 20, Max = 30, Step = 5 },
                Height = new RangedValue { Min = 5, Current = 20, Max = 30, Step = 5 }
            },
            ElementsPercentages = new ElementsPercentagesConfig
            {
                BlocksPercent    = new RangedValue { Min = 0, Current = 5, Max = 40, Step = 5 },
                PercentOfEnemies = new RangedValue { Min = 0, Current = 1, Max = 30, Step = 5 }
            }
        },
        Hero = new AgentConfig
        {
            Speed      = new RangedValue { Min = 1, Current = 2, Max = 5, Step = 1 },
            SightRange = new RangedValue { Min = 2, Current = 5, Max = 10, Step = 1 },
            Stamina    = new RangedValue { Min = 5, Current = 15, Max = 30, Step = 5 }
        },
        Enemy = new AgentConfig
        {
            Speed      = new RangedValue { Min = 1, Current = 1, Max = 4, Step = 1 },
            SightRange = new RangedValue { Min = 1, Current = 4, Max = 8, Step = 1 },
            Stamina    = new RangedValue { Min = 1, Current = 3, Max = 10, Step = 2 }
        },
        ActionDelayMs = new RangedValue { Min = 0, Current = 200, Max = 5000, Step = 50 }
    };

    private static (TestContext ctx, FakeHubClient hub, Mock<ITrainingApiClient> trainingMock, Mock<ISimulationApiClient> simMock) BuildCtx(
        SandboxSettings? settings = null)
    {
        var ctx = new TestContext();
        ctx.SetupWithMudServices();

        var hub = new FakeHubClient();
        var trainingMock = new Mock<ITrainingApiClient>();
        trainingMock.Setup(t => t.GetTrainedModelsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<TrainedModelInfoDto>());

        var simMock = new Mock<ISimulationApiClient>();
        simMock.Setup(s => s.StartSingleSimulationAsync(It.IsAny<StartSingleSimulationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new SimulationJobStartedDto { JobId = Guid.NewGuid(), StartedAt = DateTime.UtcNow });

        ctx.Services.AddSingleton<ISimulationHubClient>(hub);
        ctx.Services.AddSingleton<ISimulationApiClient>(simMock.Object);
        ctx.Services.AddSingleton<ITrainingApiClient>(trainingMock.Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);
        ctx.Services.AddSingleton<IOptions<SandboxSettings>>(
            Options.Create(settings ?? new SandboxSettings()));

        return (ctx, hub, trainingMock, simMock);
    }

    private static (TestContext ctx, FakeHubClient hub, Mock<ITrainingApiClient> trainingMock, Mock<ISimulationApiClient> simMock) BuildCtxWithModels(
        List<TrainedModelInfoDto> models)
    {
        var (ctx, hub, trainingMock, simMock) = BuildCtx();
        trainingMock.Setup(t => t.GetTrainedModelsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(models);
        return (ctx, hub, trainingMock, simMock);
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
                AgentType = ObjectType.Hero,
                Position  = new Coordinates(x, y)
            }
        ]
    };

    /// <summary>Creates a cell DTO; defaults to Empty with no effects.</summary>
    private static SimulationCellDto MakeCell(
        int x, int y,
        ObjectType objectType = ObjectType.Empty,
        AgentEffectDto[]? effects = null) =>
        new() { Position = new Coordinates(x, y), ObjectType = objectType, Effects = effects ?? [] };

    /// <summary>Helper to create a single agent effect.</summary>
    private static AgentEffectDto MakeEffect(ObjectType agentType, params EEffect[] effects) =>
        new() { AgentId = Guid.NewGuid(), AgentType = agentType, Effects = effects };

    /// <summary>Builds a minimal SimulationStartedDto with the given cells and no agents.</summary>
    private static SimulationStartedDto MakeStartedWithCells(params SimulationCellDto[] cells) => new()
    {
        Width    = 10,
        Height   = 10,
        MaxTurns = 100,
        Cells    = cells,
        Agents   = []
    };

    private static List<TrainedModelInfoDto> MakeSampleModels() =>
    [
        new TrainedModelInfoDto
        {
            Algorithm     = "PPO",
            ExperimentId  = "exp-001",
            ModelFilePath = @"C:\models\PPO\exp-001\model.zip",
            TrainedAt     = new DateTime(2025, 1, 15, 10, 0, 0),
            AgentType     = "HERO",
            Preconditions = new TrainingPreconditionsDto
            {
                Algorithm       = "PPO",
                ExperimentId    = "exp-001",
                MaxTurns        = 200,
                MapWidth        = 10,
                MapHeight       = 10,
                StepPenalty     = -0.01f,
                WinReward       = 1.0f,
                LossReward      = -1.0f,
                Hyperparameters = new() { ["learning_rate"] = "0.0003", ["n_steps"] = "2048" }
            }
        },
        new TrainedModelInfoDto
        {
            Algorithm     = "PPO",
            ExperimentId  = "exp-002",
            ModelFilePath = @"C:\models\PPO\exp-002\model.zip",
            TrainedAt     = new DateTime(2025, 1, 20, 14, 0, 0),
            AgentType     = "ENEMY",
            Preconditions = new TrainingPreconditionsDto
            {
                Algorithm       = "PPO",
                ExperimentId    = "exp-002",
                MaxTurns        = 300,
                MapWidth        = 15,
                MapHeight       = 15,
                StepPenalty     = -0.02f,
                WinReward       = 2.0f,
                LossReward      = -2.0f,
                Hyperparameters = new() { ["learning_rate"] = "0.001" }
            }
        },
        new TrainedModelInfoDto
        {
            Algorithm     = "A2C",
            ExperimentId  = "exp-a2c-01",
            ModelFilePath = @"C:\models\A2C\exp-a2c-01\model.zip",
            TrainedAt     = new DateTime(2025, 2, 1, 8, 0, 0),
            AgentType     = "HERO",
            Preconditions = null
        }
    ];

    /// <summary>Activates the simulation view and fires a SimulationStarted event.</summary>
    private static void ActivateSimulationView(IRenderedComponent<VisualizationPage> cut, FakeHubClient hub, SimulationStartedDto? startedDto = null)
    {
        cut.InvokeAsync(() => cut.Instance._simulationActive = true);
        hub.FireSimulationStarted(startedDto ?? MakeStartedWithCells());
    }

    // ── Configuration view tests ─────────────────────────────────────────────

    [TestMethod]
    public void DefaultState_ShowsConfigurationView()
    {
        var (ctx, _, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.Markup.Should().Contain("Agent AI Configuration");
            cut.Markup.Should().Contain("Start Simulation");
        }
    }

    [TestMethod]
    public void DefaultState_RandomAiIsSelected()
    {
        var (ctx, _, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.Markup.Should().Contain("Random AI");
            cut.Markup.Should().Contain("no training preconditions");
        }
    }

    [TestMethod]
    public void DefaultState_SimulationGridIsHidden()
    {
        var (ctx, _, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.Markup.Should().NotContain("Event Log");
            cut.Markup.Should().NotContain("Waiting for simulation data");
            cut.Instance._simulationActive.Should().BeFalse();
        }
    }

    [TestMethod]
    public void ConfigView_DataGridShowsRandomAiRow()
    {
        var (ctx, _, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("Random AI"));
        }
    }

    [TestMethod]
    public void ConfigView_WithModels_DataGridShowsGroupedRows()
    {
        var (ctx, _, _, _) = BuildCtxWithModels(MakeSampleModels());
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.WaitForAssertion(() =>
            {
                var rows = cut.Instance._rows;
                rows.Should().HaveCount(4);
                rows.Select(r => r.Name).Should().Contain("Random AI");
                rows.Select(r => r.Name).Should().Contain("exp-001");
                rows.Select(r => r.Name).Should().Contain("exp-002");
                rows.Select(r => r.Name).Should().Contain("exp-a2c-01");
            });
        }
    }

    [TestMethod]
    public void ConfigView_HeroGrid_ShowsOnlyHeroModelsAndRandom()
    {
        var (ctx, _, _, _) = BuildCtxWithModels(MakeSampleModels());
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.WaitForAssertion(() =>
            {
                var heroRows = cut.Instance._heroRows;
                heroRows.Should().HaveCount(3); // Random + exp-001 (HERO) + exp-a2c-01 (HERO)
                heroRows.Select(r => r.Name).Should().Contain("Random AI");
                heroRows.Select(r => r.Name).Should().Contain("exp-001");
                heroRows.Select(r => r.Name).Should().Contain("exp-a2c-01");
                heroRows.Select(r => r.Name).Should().NotContain("exp-002"); // ENEMY model
            });
        }
    }

    [TestMethod]
    public void ConfigView_EnemyGrid_ShowsOnlyEnemyModelsAndRandom()
    {
        var (ctx, _, _, _) = BuildCtxWithModels(MakeSampleModels());
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.WaitForAssertion(() =>
            {
                var enemyRows = cut.Instance._enemyRows;
                enemyRows.Should().HaveCount(2); // Random + exp-002 (ENEMY)
                enemyRows.Select(r => r.Name).Should().Contain("Random AI");
                enemyRows.Select(r => r.Name).Should().Contain("exp-002");
                enemyRows.Select(r => r.Name).Should().NotContain("exp-001"); // HERO model
            });
        }
    }

    [TestMethod]
    public void ConfigView_WithModels_ShowsGroupHeaders()
    {
        var (ctx, _, _, _) = BuildCtxWithModels(MakeSampleModels());
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.WaitForAssertion(() =>
            {
                var rows = cut.Instance._rows;
                rows.Should().Contain(r => r.Group == "Non AI");
                rows.Should().Contain(r => r.Group == "PPO");
                rows.Should().Contain(r => r.Group == "A2C");
            });
        }
    }

    [TestMethod]
    public void ConfigView_StartButtonEnabled_WhenRandomAiSelected()
    {
        var (ctx, _, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.WaitForAssertion(() =>
            {
                var startBtn = cut.FindComponents<MudButton>()
                    .First(b => b.Markup.Contains("Start Simulation"));
                startBtn.Instance.Disabled.Should().BeFalse();
            });
        }
    }

    [TestMethod]
    public void ConfigView_BackButtonNotShown()
    {
        var (ctx, _, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.Markup.Should().NotContain("Back");
        }
    }

    // ── Simulation view tests ────────────────────────────────────────────────

    [TestMethod]
    public void SimulationView_ShowsBackButton()
    {
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub);

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("Back"));
        }
    }

    [TestMethod]
    public void SimulationView_HidesConfigPanel()
    {
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub);

            cut.WaitForAssertion(() =>
            {
                cut.Markup.Should().NotContain("Agent AI Configuration");
                cut.Markup.Should().NotContain("Start Simulation");
                cut.Markup.Should().NotContain("Training Preconditions");
            });
        }
    }

    [TestMethod]
    public void SimulationView_ShowsEventLog()
    {
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub);

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("Event Log"));
        }
    }

    [TestMethod]
    public void SimulationView_WhenStopped_ShowsReRunButton()
    {
        var (ctx, hub, _, simMock) = BuildCtx();
        simMock.Setup(s => s.StopSimulationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub);

            // Simulate the simulation being not running (e.g., after stop)
            cut.InvokeAsync(() =>
            {
                // Directly set _running to false to simulate stopped state
                cut.Instance.GetType()
                    .GetField("_running", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .SetValue(cut.Instance, false);
                cut.Instance.GetType()
                    .GetMethod("StateHasChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .Invoke(cut.Instance, null);
            });

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("Re Run"));
        }
    }

    // ── Agent movement tests (simulation view) ──────────────────────────────

    [TestMethod]
    public void HandleAgentMoved_WhenIsSuccessTrue_MovesAgentCircleToNewCell()
    {
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub, MakeStartedWithAgent("agent-01", x: 2, y: 3));
            cut.WaitForAssertion(() => cut.Markup.Should().Contain("<svg"));

            hub.FireAgentMoved(new AgentMovedDto
            {
                AgentId   = "agent-01",
                AgentType = ObjectType.Hero,
                From      = new Coordinates(2, 3),
                To        = new Coordinates(4, 5),
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
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub, MakeStartedWithAgent("agent-01", x: 2, y: 3));
            cut.WaitForAssertion(() => cut.Markup.Should().Contain("<svg"));

            hub.FireAgentMoved(new AgentMovedDto
            {
                AgentId   = "agent-01",
                AgentType = ObjectType.Hero,
                From      = new Coordinates(2, 3),
                To        = new Coordinates(4, 5),
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
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub, MakeStartedWithAgent("agent-01", x: 2, y: 3));

            hub.FireAgentMoved(new AgentMovedDto
            {
                AgentId   = "agent-01",
                AgentType = ObjectType.Hero,
                From      = new Coordinates(2, 3),
                To        = new Coordinates(4, 5),
                IsSuccess = false
            });

            // The event log should record the move regardless of success
            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("Hero #agent"));
        }
    }

    [TestMethod]
    public void HandleAgentMoved_WhenIsSuccessFalse_StillUpdatesAgentSnapshot()
    {
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub, MakeStartedWithAgent("agent-01", x: 2, y: 3));
            cut.WaitForAssertion(() => cut.Markup.Should().Contain("<svg"));

            hub.FireAgentMoved(new AgentMovedDto
            {
                AgentId   = "agent-01",
                AgentType = ObjectType.Hero,
                From      = new Coordinates(2, 3),
                To        = new Coordinates(4, 5),
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
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub, MakeStartedWithCells(
                MakeCell(3, 4, effects: [MakeEffect(ObjectType.Hero, EEffect.Path)])));

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"hero-path\""));
        }
    }

    [TestMethod]
    public void EnemyPathCell_IsRenderedWithEnemyPathClass()
    {
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub, MakeStartedWithCells(
                MakeCell(5, 2, effects: [MakeEffect(ObjectType.Enemy, EEffect.Path)])));

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"enemy-path\""));
        }
    }

    [TestMethod]
    public void HeroVisionCell_IsRenderedWithHeroVisionClass()
    {
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub, MakeStartedWithCells(
                MakeCell(1, 1, effects: [MakeEffect(ObjectType.Hero, EEffect.Vision)])));

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"hero-vision\""));
        }
    }

    [TestMethod]
    public void EnemyVisionCell_IsRenderedWithEnemyVisionClass()
    {
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub, MakeStartedWithCells(
                MakeCell(7, 7, effects: [MakeEffect(ObjectType.Enemy, EEffect.Vision)])));

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"enemy-vision\""));
        }
    }

    [TestMethod]
    public void HeroPath_TakesPriorityOverEnemyPath_WhenBothEffectsPresent()
    {
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub, MakeStartedWithCells(
                MakeCell(4, 4, effects: [MakeEffect(ObjectType.Hero, EEffect.Path), MakeEffect(ObjectType.Enemy, EEffect.Path)])));

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"hero-path\""));
            cut.Markup.Should().NotContain("class=\"enemy-path\"");
        }
    }

    [TestMethod]
    public void HeroPath_TakesPriorityOverHeroVision_WhenBothEffectsPresent()
    {
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub, MakeStartedWithCells(
                MakeCell(2, 6, effects: [MakeEffect(ObjectType.Hero, EEffect.Vision), MakeEffect(ObjectType.Hero, EEffect.Path)])));

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"hero-path\""));
            cut.Markup.Should().NotContain("class=\"hero-vision\"");
        }
    }

    [TestMethod]
    public void BlockCell_WithEffects_DoesNotRenderEffectOverlay()
    {
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub, MakeStartedWithCells(
                MakeCell(3, 3, objectType: ObjectType.Block, effects: [MakeEffect(ObjectType.Hero, EEffect.Path)])));

            cut.WaitForAssertion(() => cut.Markup.Should().Contain("<svg"));
            cut.Markup.Should().NotContain("class=\"hero-path\"");
        }
    }

    [TestMethod]
    public void TurnCompleted_WithEffectCells_RendersEffectOverlays()
    {
        var (ctx, hub, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            ActivateSimulationView(cut, hub, MakeStartedWithCells(MakeCell(0, 0)));
            cut.WaitForAssertion(() => cut.Markup.Should().Contain("<svg"));

            // Turn update replaces the cell with an enemy-vision effect.
            hub.FireTurnCompleted(new TurnCompletedDto
            {
                TurnNumber   = 1,
                UpdatedCells = [MakeCell(0, 0, effects: [MakeEffect(ObjectType.Enemy, EEffect.Vision)])]
            });

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("class=\"enemy-vision\""));
        }
    }

    // ── Trained-model selection tests ────────────────────────────────────────

    [TestMethod]
    public void WhenRandomAI_PreconditionsNotShown()
    {
        var (ctx, _, _, _) = BuildCtxWithModels(MakeSampleModels());
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.Markup.Should().NotContain("Training Preconditions");
        }
    }

    [TestMethod]
    public void WhenRandomAI_InfoAlertShown()
    {
        var (ctx, _, _, _) = BuildCtxWithModels(MakeSampleModels());
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("no training preconditions"));
        }
    }

    [TestMethod]
    public void DataGrid_ShowsAllModelGroups()
    {
        var (ctx, _, _, _) = BuildCtxWithModels(MakeSampleModels());
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            cut.WaitForAssertion(() =>
            {
                // _rows still holds all models unfiltered
                cut.Instance._rows.Should().HaveCount(4); // 1 Random + 2 PPO + 1 A2C
                cut.Instance._rows.Should().Contain(r => r.Group == "Non AI");
                cut.Instance._rows.Should().Contain(r => r.Group == "PPO");
                cut.Instance._rows.Should().Contain(r => r.Group == "A2C");
            });
        }
    }

    // ── BuildDefaultOverride tests ───────────────────────────────────────────

    [TestMethod]
    public void BuildDefaultOverride_MapsAllSandboxSettingsCurrentValues()
    {
        var settings = MakeSandboxSettings();
        var (ctx, _, _, _) = BuildCtx(settings);
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            var ov = cut.Instance.BuildDefaultOverride();

            ov.MaxTurns.Should().Be(100);
            ov.MapWidth.Should().Be(20);
            ov.MapHeight.Should().Be(20);
            ov.BlocksPercent.Should().Be(5);
            ov.EnemiesPercent.Should().Be(1);
            ov.HeroSpeed.Should().Be(2);
            ov.HeroSightRange.Should().Be(5);
            ov.HeroStamina.Should().Be(15);
            ov.EnemySpeed.Should().Be(1);
            ov.EnemySightRange.Should().Be(4);
            ov.EnemyStamina.Should().Be(3);
            ov.ActionDelayMs.Should().Be(200);
        }
    }

    [TestMethod]
    public void BuildDefaultOverride_WithEmptySettings_ReturnsZeroDefaults()
    {
        var (ctx, _, _, _) = BuildCtx();
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            var ov = cut.Instance.BuildDefaultOverride();

            ov.MaxTurns.Should().Be(0);
            ov.MapWidth.Should().Be(0);
            ov.MapHeight.Should().Be(0);
            ov.BlocksPercent.Should().Be(0);
            ov.EnemiesPercent.Should().Be(0);
            ov.HeroSpeed.Should().Be(0);
            ov.HeroSightRange.Should().Be(0);
            ov.HeroStamina.Should().Be(0);
            ov.EnemySpeed.Should().Be(0);
            ov.EnemySightRange.Should().Be(0);
            ov.EnemyStamina.Should().Be(0);
            ov.ActionDelayMs.Should().Be(0);
        }
    }

    [TestMethod]
    public void BuildDefaultOverride_ActionDelayMs_IsPopulatedFromSettings()
    {
        var settings = MakeSandboxSettings();
        var (ctx, _, _, _) = BuildCtx(settings);
        using (ctx)
        {
            var cut = ctx.RenderComponent<VisualizationPage>();
            var ov = cut.Instance.BuildDefaultOverride();

            ov.ActionDelayMs.Should().Be(200);
        }
    }
}

[TestClass]
public class VisualizationPage_StaticHelperTests
{
    private static AgentEffectDto MakeEffect(ObjectType agentType, params EEffect[] effects) =>
        new() { AgentId = Guid.NewGuid(), AgentType = agentType, Effects = effects };

    private static SimulationCellDto MakeCell(
        ObjectType objectType = ObjectType.Empty,
        params AgentEffectDto[] effects) =>
        new() { Position = new Coordinates(0, 0), ObjectType = objectType, Effects = effects };

    // ── CellFill ─────────────────────────────────────────────────────────

    [TestMethod]
    [DataRow(ObjectType.Block,       "#616161")]
    [DataRow(ObjectType.BorderBlock, "#424242")]
    [DataRow(ObjectType.Exit,        "#43a047")]
    [DataRow(ObjectType.Empty,       "#fafafa")]
    public void CellFill_ReturnsCorrectColor(ObjectType type, string expected)
        => VisualizationPage.CellFill(type).Should().Be(expected);

    [TestMethod]
    public void CellFill_UnknownType_ReturnsFallbackColor()
        => VisualizationPage.CellFill(ObjectType.Hero).Should().Be("#fafafa");

    // ── AgentFill ────────────────────────────────────────────────────────

    [TestMethod]
    public void AgentFill_Hero_ReturnsBlue()
        => VisualizationPage.AgentFill(ObjectType.Hero).Should().Be("#1e88e5");

    [TestMethod]
    public void AgentFill_Enemy_ReturnsRed()
        => VisualizationPage.AgentFill(ObjectType.Enemy).Should().Be("#e53935");

    [TestMethod]
    public void AgentFill_OtherType_ReturnsPurple()
        => VisualizationPage.AgentFill(ObjectType.Empty).Should().Be("#9c27b0");

    // ── HasEffect ────────────────────────────────────────────────────────

    [TestMethod]
    public void HasEffect_ReturnsTrueWhenMatchingEffectPresent()
    {
        var cell = MakeCell(effects: MakeEffect(ObjectType.Hero, EEffect.Path));
        VisualizationPage.HasEffect(cell, ObjectType.Hero, EEffect.Path).Should().BeTrue();
    }

    [TestMethod]
    public void HasEffect_ReturnsFalseWhenAgentTypeMismatches()
    {
        var cell = MakeCell(effects: MakeEffect(ObjectType.Hero, EEffect.Path));
        VisualizationPage.HasEffect(cell, ObjectType.Enemy, EEffect.Path).Should().BeFalse();
    }

    [TestMethod]
    public void HasEffect_ReturnsFalseWhenEffectTypeMismatches()
    {
        var cell = MakeCell(effects: MakeEffect(ObjectType.Hero, EEffect.Path));
        VisualizationPage.HasEffect(cell, ObjectType.Hero, EEffect.Vision).Should().BeFalse();
    }

    [TestMethod]
    public void HasEffect_ReturnsFalseWhenNoEffects()
    {
        var cell = MakeCell();
        VisualizationPage.HasEffect(cell, ObjectType.Hero, EEffect.Path).Should().BeFalse();
    }

    [TestMethod]
    public void HasEffect_HandlesMixedEffectsOnSameAgent()
    {
        var cell = MakeCell(effects: MakeEffect(ObjectType.Hero, EEffect.Path, EEffect.Vision));
        VisualizationPage.HasEffect(cell, ObjectType.Hero, EEffect.Path).Should().BeTrue();
        VisualizationPage.HasEffect(cell, ObjectType.Hero, EEffect.Vision).Should().BeTrue();
        VisualizationPage.HasEffect(cell, ObjectType.Hero, EEffect.Run).Should().BeFalse();
    }

    // ── GetEffectFill ────────────────────────────────────────────────────

    [TestMethod]
    public void GetEffectFill_ReturnsNull_ForBlockCell()
    {
        var cell = MakeCell(ObjectType.Block, MakeEffect(ObjectType.Hero, EEffect.Path));
        VisualizationPage.GetEffectFill(cell).Should().BeNull();
    }

    [TestMethod]
    public void GetEffectFill_ReturnsNull_ForBorderBlockCell()
    {
        var cell = MakeCell(ObjectType.BorderBlock, MakeEffect(ObjectType.Hero, EEffect.Path));
        VisualizationPage.GetEffectFill(cell).Should().BeNull();
    }

    [TestMethod]
    public void GetEffectFill_ReturnsNull_ForEmptyCellWithNoEffects()
    {
        var cell = MakeCell();
        VisualizationPage.GetEffectFill(cell).Should().BeNull();
    }

    [TestMethod]
    public void GetEffectFill_HeroPath_ReturnsBlue60()
    {
        var cell = MakeCell(effects: MakeEffect(ObjectType.Hero, EEffect.Path));
        VisualizationPage.GetEffectFill(cell).Should().Be("rgba(30,136,229,0.60)");
    }

    [TestMethod]
    public void GetEffectFill_EnemyPath_ReturnsRed60()
    {
        var cell = MakeCell(effects: MakeEffect(ObjectType.Enemy, EEffect.Path));
        VisualizationPage.GetEffectFill(cell).Should().Be("rgba(229,57,53,0.60)");
    }

    [TestMethod]
    public void GetEffectFill_HeroVision_ReturnsBlue20()
    {
        var cell = MakeCell(effects: MakeEffect(ObjectType.Hero, EEffect.Vision));
        VisualizationPage.GetEffectFill(cell).Should().Be("rgba(30,136,229,0.20)");
    }

    [TestMethod]
    public void GetEffectFill_EnemyVision_ReturnsRed20()
    {
        var cell = MakeCell(effects: MakeEffect(ObjectType.Enemy, EEffect.Vision));
        VisualizationPage.GetEffectFill(cell).Should().Be("rgba(229,57,53,0.20)");
    }

    [TestMethod]
    public void GetEffectFill_HeroPathTakesPriorityOverEnemyPath()
    {
        var cell = MakeCell(effects: [
            MakeEffect(ObjectType.Enemy, EEffect.Path),
            MakeEffect(ObjectType.Hero,  EEffect.Path)]);
        VisualizationPage.GetEffectFill(cell).Should().Be("rgba(30,136,229,0.60)");
    }

    [TestMethod]
    public void GetEffectFill_PathTakesPriorityOverVision()
    {
        var cell = MakeCell(effects: [
            MakeEffect(ObjectType.Hero, EEffect.Vision),
            MakeEffect(ObjectType.Enemy, EEffect.Path)]);
        VisualizationPage.GetEffectFill(cell).Should().Be("rgba(229,57,53,0.60)");
    }

    [TestMethod]
    public void GetEffectFill_HeroVisionTakesPriorityOverEnemyVision()
    {
        var cell = MakeCell(effects: [
            MakeEffect(ObjectType.Enemy, EEffect.Vision),
            MakeEffect(ObjectType.Hero,  EEffect.Vision)]);
        VisualizationPage.GetEffectFill(cell).Should().Be("rgba(30,136,229,0.20)");
    }

    // ── GetEffectClass ───────────────────────────────────────────────────

    [TestMethod]
    public void GetEffectClass_HeroPath_ReturnsHeroPath()
    {
        var cell = MakeCell(effects: MakeEffect(ObjectType.Hero, EEffect.Path));
        VisualizationPage.GetEffectClass(cell).Should().Be("hero-path");
    }

    [TestMethod]
    public void GetEffectClass_EnemyPath_ReturnsEnemyPath()
    {
        var cell = MakeCell(effects: MakeEffect(ObjectType.Enemy, EEffect.Path));
        VisualizationPage.GetEffectClass(cell).Should().Be("enemy-path");
    }

    [TestMethod]
    public void GetEffectClass_HeroVision_ReturnsHeroVision()
    {
        var cell = MakeCell(effects: MakeEffect(ObjectType.Hero, EEffect.Vision));
        VisualizationPage.GetEffectClass(cell).Should().Be("hero-vision");
    }

    [TestMethod]
    public void GetEffectClass_EnemyVision_ReturnsEnemyVision()
    {
        var cell = MakeCell(effects: MakeEffect(ObjectType.Enemy, EEffect.Vision));
        VisualizationPage.GetEffectClass(cell).Should().Be("enemy-vision");
    }

    [TestMethod]
    public void GetEffectClass_NoEffects_ReturnsEmptyString()
    {
        var cell = MakeCell();
        VisualizationPage.GetEffectClass(cell).Should().BeEmpty();
    }

    [TestMethod]
    public void GetEffectClass_PrioritizesHeroPathOverEnemyVision()
    {
        var cell = MakeCell(effects: [
            MakeEffect(ObjectType.Enemy, EEffect.Vision),
            MakeEffect(ObjectType.Hero,  EEffect.Path)]);
        VisualizationPage.GetEffectClass(cell).Should().Be("hero-path");
    }
}
