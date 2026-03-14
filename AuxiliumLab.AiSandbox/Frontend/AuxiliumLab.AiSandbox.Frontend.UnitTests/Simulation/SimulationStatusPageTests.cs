using AuxiliumLab.Frontend.Features.Simulation.Pages;
using Bunit;
using MudBlazor.Services;

namespace AuxiliumLab.AiSandbox.Frontend.UnitTests.Simulation;

[TestClass]
public class SimulationStatusPageTests
{
    [TestMethod]
    public void RendersPageHeader()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        var mockApi = new Mock<ISimulationApiClient>();
        mockApi.Setup(a => a.GetSimulationStatusesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);

        var cut = ctx.RenderComponent<SimulationStatusPage>();
        cut.Markup.Should().Contain("Simulation Status");
    }

    [TestMethod]
    public void ShowsEmptyState_WhenNoJobs()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        var mockApi = new Mock<ISimulationApiClient>();
        mockApi.Setup(a => a.GetSimulationStatusesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        ctx.Services.AddSingleton(mockApi.Object);
        ctx.Services.AddSingleton(new Mock<INotificationService>().Object);

        var cut = ctx.RenderComponent<SimulationStatusPage>();
        cut.Markup.Should().Contain("No simulation jobs found");
    }
}
