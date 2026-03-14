using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace AuxiliumLab.AiSandbox.IntegrationTests.Statistic;

[TestClass]
public sealed class StatisticControllerTests
{
    private static AiSandboxWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        _factory = new AiSandboxWebApplicationFactory();
        _client  = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task GetCompletedSimulations_ReturnsOk()
    {
        var response = await _client.GetAsync("/ai-sandbox/statistic/simulations");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetCompletedAggregations_ReturnsOk()
    {
        var response = await _client.GetAsync("/ai-sandbox/statistic/aggregations");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
