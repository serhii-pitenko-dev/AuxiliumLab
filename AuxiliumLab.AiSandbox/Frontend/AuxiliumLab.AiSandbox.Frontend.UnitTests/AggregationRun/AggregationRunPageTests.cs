using AuxiliumLab.Frontend.Features.AggregationRun.Pages;
using Bunit;
using MudBlazor.Services;

namespace AuxiliumLab.AiSandbox.Frontend.UnitTests.AggregationRun;

[TestClass]
public class AggregationRunPageTests
{
    [TestMethod]
    public void RendersPageHeader()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        ctx.Services.AddSingleton(new Mock<IAggregationApiClient>().Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);

        var cut = ctx.RenderComponent<AggregationRunPage>();
        cut.Markup.Should().Contain("Aggregation Run");
    }

    [TestMethod]
    public async Task StartButton_CallsApi_WhenStepsPresent()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();

        var started = new AggregationJobStartedDto { JobId = Guid.NewGuid(), StepNames = ["Step1"], StartedAt = DateTime.UtcNow };
        var mockApi = new Mock<IAggregationApiClient>();
        mockApi.Setup(a => a.StartAggregationAsync(It.IsAny<StartAggregationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(started);

        var mockNotif = new Mock<INotificationService>();
        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(mockNotif.Object);

        var cut = ctx.RenderComponent<AggregationRunPage>();

        // First button with text "Start Aggregation"
        var btn = cut.FindAll("button").First(b => b.TextContent.Contains("Start Aggregation"));
        await cut.InvokeAsync(() => btn.Click());

        mockApi.Verify(a => a.StartAggregationAsync(It.IsAny<StartAggregationCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        mockNotif.Verify(n => n.Notify(It.IsAny<string>(), It.IsAny<NotificationSeverity>()), Times.Once);
    }

    [TestMethod]
    public async Task StartAggregation_SendsCorrectDefaultSteps()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();

        StartAggregationCommand? capturedCmd = null;
        var mockApi = new Mock<IAggregationApiClient>();
        mockApi.Setup(a => a.StartAggregationAsync(It.IsAny<StartAggregationCommand>(), It.IsAny<CancellationToken>()))
               .Callback<StartAggregationCommand, CancellationToken>((cmd, _) => capturedCmd = cmd)
               .ReturnsAsync(new AggregationJobStartedDto { JobId = Guid.NewGuid(), StepNames = ["s1"], StartedAt = DateTime.UtcNow });

        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);

        var cut = ctx.RenderComponent<AggregationRunPage>();
        var btn = cut.FindAll("button").First(b => b.TextContent.Contains("Start Aggregation"));
        await cut.InvokeAsync(() => btn.Click());

        capturedCmd.Should().NotBeNull();
        capturedCmd!.Steps.Should().HaveCount(2);
        capturedCmd.Steps[0].Name.Should().Be("Random AI");
        capturedCmd.Steps[0].Mode.Should().Be("MassRandomAISimulation");
        capturedCmd.Steps[1].Name.Should().Be("PPO - AI");
        capturedCmd.Steps[1].Mode.Should().Be("MassTrainedAISimulation");
    }

    [TestMethod]
    public async Task StartAggregation_SendsCorrectDefaultSettings()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();

        StartAggregationCommand? capturedCmd = null;
        var mockApi = new Mock<IAggregationApiClient>();
        mockApi.Setup(a => a.StartAggregationAsync(It.IsAny<StartAggregationCommand>(), It.IsAny<CancellationToken>()))
               .Callback<StartAggregationCommand, CancellationToken>((cmd, _) => capturedCmd = cmd)
               .ReturnsAsync(new AggregationJobStartedDto { JobId = Guid.NewGuid(), StepNames = ["s1"], StartedAt = DateTime.UtcNow });

        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);

        var cut = ctx.RenderComponent<AggregationRunPage>();
        var btn = cut.FindAll("button").First(b => b.TextContent.Contains("Start Aggregation"));
        await cut.InvokeAsync(() => btn.Click());

        capturedCmd.Should().NotBeNull();
        capturedCmd!.StandardSimulationCount.Should().Be(100);
        capturedCmd.Algorithm.Should().Be(ModelType.PPO);
        capturedCmd.PolicyType.Should().Be(AiPolicy.MLP);
    }

    [TestMethod]
    public async Task DeleteAllSteps_DisablesStartButton()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        ctx.Services.AddSingleton(new Mock<IAggregationApiClient>().Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);

        var cut = ctx.RenderComponent<AggregationRunPage>();

        // Delete both default steps
        while (cut.FindAll("[aria-label='Delete']").Any())
        {
            var delBtn = cut.Find("[aria-label='Delete']");
            await cut.InvokeAsync(() => delBtn.Click());
        }

        // Start button should be disabled
        var startBtn = cut.Find("button[disabled]");
        startBtn.Should().NotBeNull();
    }
}
