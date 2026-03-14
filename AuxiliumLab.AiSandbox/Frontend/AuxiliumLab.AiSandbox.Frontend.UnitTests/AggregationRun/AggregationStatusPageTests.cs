using AuxiliumLab.Frontend.Features.AggregationRun.Pages;
using Bunit;
using MudBlazor.Services;

namespace AuxiliumLab.AiSandbox.Frontend.UnitTests.AggregationRun;

[TestClass]
public class AggregationStatusPageTests
{
    [TestMethod]
    public void RendersPageHeader()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        var mockApi = new Mock<IAggregationApiClient>();
        mockApi.Setup(a => a.GetAggregationStatusesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);

        var cut = ctx.RenderComponent<AggregationStatusPage>();
        cut.Markup.Should().Contain("Aggregation Status");
    }

    [TestMethod]
    public void ShowsEmptyState_WhenNoJobs()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        var mockApi = new Mock<IAggregationApiClient>();
        mockApi.Setup(a => a.GetAggregationStatusesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);

        var cut = ctx.RenderComponent<AggregationStatusPage>();
        cut.Markup.Should().Contain("No aggregation jobs found");
    }

    [TestMethod]
    public async Task StopButton_CallsStopApi()
    {
        var jobId = Guid.NewGuid();
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();

        var mockApi = new Mock<IAggregationApiClient>();
        mockApi.Setup(a => a.GetAggregationStatusesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([new AggregationJobStatusDto { JobId = jobId, State = AggregationJobState.Running, StartedAt = DateTime.UtcNow }]);
        mockApi.Setup(a => a.StopAggregationAsync(jobId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        var mockNotif = new Mock<INotificationService>();
        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(mockNotif.Object);

        var cut = ctx.RenderComponent<AggregationStatusPage>();
        var stopBtn = cut.Find("[aria-label='Stop']");
        await cut.InvokeAsync(() => stopBtn.Click());

        mockApi.Verify(a => a.StopAggregationAsync(jobId, It.IsAny<CancellationToken>()), Times.Once);
        mockNotif.Verify(n => n.Notify(It.IsAny<string>(), It.IsAny<NotificationSeverity>()), Times.Once);
    }
}
