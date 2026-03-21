using AuxiliumLab.Frontend.Features.Training.Pages;
using Bunit;
using MudBlazor.Services;

namespace AuxiliumLab.AiSandbox.Frontend.UnitTests.Training;

[TestClass]
public class PpoTrainingPageTests
{
    [TestMethod]
    public void PageRendersWithDefaultFormValues()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        var mockTrainingApi = new Mock<ITrainingApiClient>();
        var mockNotifications = new Mock<INotificationService>();
        ctx.Services.AddSingleton(mockTrainingApi.Object);
        ctx.Services.AddSingleton(mockNotifications.Object);

        // Act
        var cut = ctx.RenderComponent<PpoTrainingPage>();

        // Assert – page header is present
        cut.Markup.Should().Contain("PPO Training");
    }

    [TestMethod]
    public async Task StartTraining_CallsApiAndNotifies()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();

        var started = new TrainingJobStartedDto { JobId = Guid.NewGuid(), Algorithm = "PPO", ExperimentId = "exp1" };
        var mockApi  = new Mock<ITrainingApiClient>();
        mockApi.Setup(a => a.StartPpoTrainingAsync(It.IsAny<StartPpoTrainingCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(started);

        var mockNotif = new Mock<INotificationService>();
        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(mockNotif.Object);

        var cut = ctx.RenderComponent<PpoTrainingPage>();

        // Act – click Start button
        var btn = cut.Find("[aria-label='Start Training']");
        await cut.InvokeAsync(() => btn.Click());

        // Assert
        mockApi.Verify(a => a.StartPpoTrainingAsync(It.IsAny<StartPpoTrainingCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        mockNotif.Verify(n => n.Notify(It.IsAny<string>(), It.IsAny<NotificationSeverity>()), Times.Once);
    }

    [TestMethod]
    public async Task StartTraining_SendsCorrectDefaultHyperparameters()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();

        StartPpoTrainingCommand? capturedCmd = null;
        var mockApi = new Mock<ITrainingApiClient>();
        mockApi.Setup(a => a.StartPpoTrainingAsync(It.IsAny<StartPpoTrainingCommand>(), It.IsAny<CancellationToken>()))
               .Callback<StartPpoTrainingCommand, CancellationToken>((cmd, _) => capturedCmd = cmd)
               .ReturnsAsync(new TrainingJobStartedDto { JobId = Guid.NewGuid(), Algorithm = "PPO", ExperimentId = "exp1" });

        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);

        var cut = ctx.RenderComponent<PpoTrainingPage>();
        var btn = cut.Find("[aria-label='Start Training']");
        await cut.InvokeAsync(() => btn.Click());

        capturedCmd.Should().NotBeNull();
        var hp = capturedCmd!.Hyperparameters!;
        hp.TotalTimesteps.Should().Be(100_000);
        hp.LearningRate.Should().Be(0.0003);
        hp.NSteps.Should().Be(256);
        hp.BatchSize.Should().Be(64);
        hp.NEpochs.Should().Be(5);
        hp.Gamma.Should().Be(0.90);
        hp.GaeLambda.Should().Be(0.95);
        hp.ClipRange.Should().Be(0.2);
        hp.EntCoef.Should().Be(0.1);
        hp.Seed.Should().Be(42);
        hp.NEnvs.Should().Be(4);
    }

    [TestMethod]
    public async Task StartTraining_SendsCorrectDefaultRewardSettings()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();

        StartPpoTrainingCommand? capturedCmd = null;
        var mockApi = new Mock<ITrainingApiClient>();
        mockApi.Setup(a => a.StartPpoTrainingAsync(It.IsAny<StartPpoTrainingCommand>(), It.IsAny<CancellationToken>()))
               .Callback<StartPpoTrainingCommand, CancellationToken>((cmd, _) => capturedCmd = cmd)
               .ReturnsAsync(new TrainingJobStartedDto { JobId = Guid.NewGuid(), Algorithm = "PPO", ExperimentId = "exp1" });

        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);

        var cut = ctx.RenderComponent<PpoTrainingPage>();
        var btn = cut.Find("[aria-label='Start Training']");
        await cut.InvokeAsync(() => btn.Click());

        capturedCmd.Should().NotBeNull();
        var rewards = capturedCmd!.RewardSettings!;
        rewards.StepPenalty.Should().Be(-0.1f);
        rewards.WinReward.Should().Be(10.0f);
        rewards.LossReward.Should().Be(-10.0f);
    }
}
