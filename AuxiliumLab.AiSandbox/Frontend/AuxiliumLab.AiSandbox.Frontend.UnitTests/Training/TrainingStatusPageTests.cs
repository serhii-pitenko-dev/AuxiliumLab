using AuxiliumLab.Frontend.Features.Training.Pages;
using Bunit;
using MudBlazor.Services;

namespace AuxiliumLab.AiSandbox.Frontend.UnitTests.Training;

[TestClass]
public class TrainingStatusPageTests
{
    private static (TestContext ctx, Mock<ITrainingApiClient>, Mock<INotificationService>) BuildContext(
        List<TrainingJobStatusDto>? jobs = null)
    {
        var ctx = new TestContext();
        ctx.SetupWithMudServices();

        var mockApi = new Mock<ITrainingApiClient>();
        mockApi.Setup(a => a.GetTrainingStatusesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(jobs ?? []);
        mockApi.Setup(a => a.GetTrainedModelsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        var mockNotif = new Mock<INotificationService>();
        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(mockNotif.Object);

        return (ctx, mockApi, mockNotif);
    }

    [TestMethod]
    public void RendersPageHeader()
    {
        var (ctx, _, _) = BuildContext();
        using (ctx)
        {
            var cut = ctx.RenderComponent<TrainingStatusPage>();
            cut.Markup.Should().Contain("Training Status");
        }
    }

    [TestMethod]
    public void ShowsEmptyState_WhenNoJobs()
    {
        var (ctx, _, _) = BuildContext([]);
        using (ctx)
        {
            var cut = ctx.RenderComponent<TrainingStatusPage>();
            cut.Markup.Should().Contain("No training jobs found");
        }
    }

    [TestMethod]
    public async Task StopButton_CallsStopApi_ForRunningJob()
    {
        var jobId = Guid.NewGuid();
        var jobs  = new List<TrainingJobStatusDto>
        {
            new() { JobId = jobId, State = TrainingJobState.Running, Algorithm = "PPO",
                    ExperimentId = "exp1", StartedAt = DateTime.UtcNow }
        };

        var (ctx, mockApi, mockNotif) = BuildContext(jobs);
        mockApi.Setup(a => a.StopTrainingAsync(jobId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        using (ctx)
        {
            var cut = ctx.RenderComponent<TrainingStatusPage>();

            // Find stop icon button and click it
            var stopBtn = cut.Find("[aria-label='Stop']");
            await cut.InvokeAsync(() => stopBtn.Click());

            mockApi.Verify(a => a.StopTrainingAsync(jobId, It.IsAny<CancellationToken>()), Times.Once);
            mockNotif.Verify(n => n.Notify(It.IsAny<string>(), It.IsAny<NotificationSeverity>()), Times.Once);
        }
    }
}
