using AuxiliumLab.Frontend.Features.Simulation.Pages;
using Bunit;
using MudBlazor.Services;

namespace AuxiliumLab.AiSandbox.Frontend.UnitTests.Simulation;

[TestClass]
public class MultipleRunPageTests
{
    [TestMethod]
    public void RendersPageHeader()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        ctx.Services.AddSingleton(new Mock<ISimulationApiClient>().Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);

        var cut = ctx.RenderComponent<MultipleRunPage>();
        cut.Markup.Should().Contain("Multiple Simulation Run");
    }

    [TestMethod]
    public async Task StartButton_CallsMassSimulationApi()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();

        var mockApi = new Mock<ISimulationApiClient>();
        mockApi.Setup(a => a.StartMassSimulationAsync(It.IsAny<StartMassSimulationCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new SimulationJobStartedDto { JobId = Guid.NewGuid(), Kind = SimulationKind.RandomAI });

        var mockNotif = new Mock<INotificationService>();
        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(mockNotif.Object);

        var cut = ctx.RenderComponent<MultipleRunPage>();
        var btn = cut.Find("[aria-label='Start Mass Simulation']");
        await cut.InvokeAsync(() => btn.Click());

        mockApi.Verify(a => a.StartMassSimulationAsync(It.IsAny<StartMassSimulationCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
