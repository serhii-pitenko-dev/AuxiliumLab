using AuxiliumLab.Frontend.Features.Training.Pages;
using Bunit;
using MudBlazor.Services;

namespace AuxiliumLab.AiSandbox.Frontend.UnitTests.Training;

[TestClass]
public class TrainingStatusPageTests
{
    private static (TestContext ctx, Mock<ITrainingApiClient>, Mock<INotificationService>) BuildContext(
        List<TrainingJobStatusDto>? jobs = null,
        List<TrainedModelInfoDto>? models = null)
    {
        var ctx = new TestContext();
        ctx.SetupWithMudServices();

        var mockApi = new Mock<ITrainingApiClient>();
        mockApi.Setup(a => a.GetTrainingStatusesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(jobs ?? []);
        mockApi.Setup(a => a.GetTrainedModelsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(models ?? []);

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

    [TestMethod]
    public void DatePickers_AreRendered()
    {
        var (ctx, _, _) = BuildContext();
        using (ctx)
        {
            var cut = ctx.RenderComponent<TrainingStatusPage>();
            cut.Markup.Should().Contain("From");
            cut.Markup.Should().Contain("To");
        }
    }

    [TestMethod]
    public void DefaultDateFilter_ShowsAllJobs()
    {
        var jobs = new List<TrainingJobStatusDto>
        {
            new() { JobId = Guid.NewGuid(), State = TrainingJobState.Completed, Algorithm = "PPO",
                    ExperimentId = "exp1", StartedAt = DateTime.UtcNow.AddDays(-10) },
            new() { JobId = Guid.NewGuid(), State = TrainingJobState.Running, Algorithm = "PPO",
                    ExperimentId = "exp2", StartedAt = DateTime.UtcNow }
        };

        var (ctx, _, _) = BuildContext(jobs);
        using (ctx)
        {
            var cut = ctx.RenderComponent<TrainingStatusPage>();

            // With no date filter, all jobs should be shown
            cut.WaitForAssertion(() =>
            {
                cut.Markup.Should().Contain("exp1");
                cut.Markup.Should().Contain("exp2");
            });
        }
    }

    [TestMethod]
    public async Task WhenFromDateSet_FiltersOlderJobs()
    {
        var oldJob = new TrainingJobStatusDto
        {
            JobId = Guid.NewGuid(), State = TrainingJobState.Completed, Algorithm = "PPO",
            ExperimentId = "old-job", StartedAt = new DateTime(2025, 1, 1)
        };
        var newJob = new TrainingJobStatusDto
        {
            JobId = Guid.NewGuid(), State = TrainingJobState.Completed, Algorithm = "PPO",
            ExperimentId = "new-job", StartedAt = new DateTime(2025, 6, 15)
        };

        var (ctx, _, _) = BuildContext([oldJob, newJob]);
        using (ctx)
        {
            var cut = ctx.RenderComponent<TrainingStatusPage>();

            // Both jobs visible initially
            cut.WaitForAssertion(() =>
            {
                cut.Markup.Should().Contain("old-job");
                cut.Markup.Should().Contain("new-job");
            });

            // Set _from to filter out old job and manually trigger refresh
            await cut.InvokeAsync(async () =>
            {
                cut.Instance._from = new DateTime(2025, 6, 1);
                // Call the Refresh button handler via reflection or directly trigger re-render
                var refreshMethod = cut.Instance.GetType()
                    .GetMethod("RefreshAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                await (Task)refreshMethod.Invoke(cut.Instance, null)!;
            });

            cut.WaitForAssertion(() =>
            {
                cut.Markup.Should().Contain("new-job");
                cut.Markup.Should().NotContain("old-job");
            });
        }
    }

    [TestMethod]
    public void FailedTrainingHistory_ExcludesDuplicatesAlreadyInJobStatus()
    {
        const string sharedExperimentId = "ppo_1000_4_0.0003_256_64_5_0.9_0.95_0.2_0.1_42_20260403";

        // In-memory job status (from /training/status API)
        var failedJob = new TrainingJobStatusDto
        {
            JobId = Guid.NewGuid(),
            State = TrainingJobState.Failed,
            Algorithm = "PPO",
            ExperimentId = sharedExperimentId,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow.AddMinutes(-3),
            ErrorMessage = "DEADLINE_EXCEEDED"
        };

        // On-disk failed model with the SAME ExperimentId (from /training/models API)
        var failedModel = new TrainedModelInfoDto
        {
            Algorithm = "PPO",
            ExperimentId = sharedExperimentId,
            IsFailed = true,
            ErrorMessage = "DEADLINE_EXCEEDED",
            TrainedAt = DateTime.UtcNow.AddMinutes(-3)
        };

        var (ctx, _, _) = BuildContext(
            jobs: [failedJob],
            models: [failedModel]);

        using (ctx)
        {
            var cut = ctx.RenderComponent<TrainingStatusPage>();

            cut.WaitForAssertion(() =>
            {
                // The experiment should appear in the main status section
                cut.Markup.Should().Contain(sharedExperimentId);

                // "Failed Trainings (History)" header should NOT appear
                // because all failed models are already represented in _jobs
                cut.Markup.Should().NotContain("Failed Trainings (History)");
            });
        }
    }

    [TestMethod]
    public void FailedTrainingHistory_ShowsOnlyUniqueFailures()
    {
        const string jobExperimentId = "ppo_job_experiment";
        const string diskOnlyExperimentId = "ppo_old_failure";

        var failedJob = new TrainingJobStatusDto
        {
            JobId = Guid.NewGuid(),
            State = TrainingJobState.Failed,
            Algorithm = "PPO",
            ExperimentId = jobExperimentId,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow.AddMinutes(-3),
            ErrorMessage = "Some error"
        };

        // Two failed models on disk: one matching the job, one older
        var failedModelSame = new TrainedModelInfoDto
        {
            Algorithm = "PPO",
            ExperimentId = jobExperimentId,
            IsFailed = true,
            ErrorMessage = "Some error",
            TrainedAt = DateTime.UtcNow.AddMinutes(-3)
        };
        var failedModelOld = new TrainedModelInfoDto
        {
            Algorithm = "PPO",
            ExperimentId = diskOnlyExperimentId,
            IsFailed = true,
            ErrorMessage = "Old failure",
            TrainedAt = DateTime.UtcNow.AddDays(-7)
        };

        var (ctx, _, _) = BuildContext(
            jobs: [failedJob],
            models: [failedModelSame, failedModelOld]);

        using (ctx)
        {
            var cut = ctx.RenderComponent<TrainingStatusPage>();

            cut.WaitForAssertion(() =>
            {
                // Job experiment in main status
                cut.Markup.Should().Contain(jobExperimentId);

                // Old disk-only failure in history section
                cut.Markup.Should().Contain("Failed Trainings (History)");
                cut.Markup.Should().Contain(diskOnlyExperimentId);
            });
        }
    }
}
