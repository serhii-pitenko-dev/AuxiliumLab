using AuxiliumLab.Frontend.Features.DataManagement.Pages;
using Bunit;
using MudBlazor.Services;

namespace AuxiliumLab.AiSandbox.Frontend.UnitTests.DataManagement;

[TestClass]
public class AggregationVisualizationPageTests
{
    [TestMethod]
    public void RendersPageHeader()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        var mockApi = new Mock<IStatisticsApiClient>();
        mockApi.Setup(a => a.GetCompletedAggregationsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        ctx.Services.AddSingleton(mockApi.Object);

        var cut = ctx.RenderComponent<AggregationVisualizationPage>();
        cut.Markup.Should().Contain("Aggregation Run Visualization");
    }

    [TestMethod]
    public void ShowsEmptyMessage_WhenNoRuns()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();
        var mockApi = new Mock<IStatisticsApiClient>();
        mockApi.Setup(a => a.GetCompletedAggregationsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        ctx.Services.AddSingleton(mockApi.Object);

        var cut = ctx.RenderComponent<AggregationVisualizationPage>();
        cut.Markup.Should().Contain("No completed aggregation runs found");
    }

    [TestMethod]
    public void PopulatesDropdown_WhenRunsExist()
    {
        using var ctx = new TestContext();
        ctx.SetupWithMudServices();

        var run = new CompletedAggregationRunDto
        {
            JobId       = Guid.NewGuid(),
            StartedAt   = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            CompletedAt = new DateTime(2025, 1, 1, 11, 0, 0, DateTimeKind.Utc),
            Steps       = [new AggregationStepResultDto { StepName = "Step1", TotalRuns = 100, Wins = 60 }]
        };

        var mockApi = new Mock<IStatisticsApiClient>();
        mockApi.Setup(a => a.GetCompletedAggregationsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([run]);
        ctx.Services.AddSingleton(mockApi.Object);

        var cut = ctx.RenderComponent<AggregationVisualizationPage>();
        // MudSelectItems render into MudPopoverProvider, not directly in cut.Markup;
        // verify via the component tree instead.
        var items = cut.FindComponents<MudSelectItem<Guid?>>();
        items.Count.Should().Be(2); // "— Select —" + 1 loaded run
    }
}
