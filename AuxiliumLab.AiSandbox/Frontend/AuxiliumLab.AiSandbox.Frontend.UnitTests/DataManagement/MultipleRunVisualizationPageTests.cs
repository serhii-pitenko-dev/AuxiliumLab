using AuxiliumLab.Frontend.Features.DataManagement.Pages;
using Bunit;
using MudBlazor.Services;

namespace AuxiliumLab.AiSandbox.Frontend.UnitTests.DataManagement;

[TestClass]
public class MultipleRunVisualizationPageTests
{
    [TestMethod]
    public void RendersPageHeader()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        var mockApi = new Mock<IStatisticsApiClient>();
        mockApi.Setup(a => a.GetCompletedSimulationsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        ctx.Services.AddSingleton(mockApi.Object);

        var cut = ctx.RenderComponent<MultipleRunVisualizationPage>();
        cut.Markup.Should().Contain("Multiple Run Visualization");
    }

    [TestMethod]
    public void ShowsEmptyMessage_WhenNoRuns()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        var mockApi = new Mock<IStatisticsApiClient>();
        mockApi.Setup(a => a.GetCompletedSimulationsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        ctx.Services.AddSingleton(mockApi.Object);

        var cut = ctx.RenderComponent<MultipleRunVisualizationPage>();
        cut.Markup.Should().Contain("No completed simulation runs found");
    }
}
