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
    public async Task StartButton_IsDisabled_WhenNoSteps()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        ctx.Services.AddSingleton(new Mock<IAggregationApiClient>().Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);

        var cut = ctx.RenderComponent<AggregationRunPage>();

        // Remove the default step (click delete button)
        var delBtn = cut.Find("[aria-label='Delete']");
        await cut.InvokeAsync(() => delBtn.Click());

        // Start button should now be disabled
        var startBtn = cut.Find("button[disabled]");
        startBtn.Should().NotBeNull();
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
}
