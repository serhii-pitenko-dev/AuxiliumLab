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
}
