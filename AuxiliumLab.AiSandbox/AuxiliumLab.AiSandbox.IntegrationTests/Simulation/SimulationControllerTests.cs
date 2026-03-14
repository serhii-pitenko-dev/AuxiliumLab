using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuxiliumLab.AiSandbox.IntegrationTests.Simulation;

[TestClass]
public sealed class SimulationControllerTests
{
    private static AiSandboxWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

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

    [TestCleanup]
    public void TestCleanup() => _factory.CleanArtifacts();

    [TestMethod]
    public async Task GetSimulationStatus_ReturnsOk()
    {
        var response = await _client.GetAsync("/ai-sandbox/simulation/status");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task MassSimulation_RandomAI_10Runs_10x10Map_CompletesSuccessfully()
    {
        // Arrange
        var body = new
        {
            Kind            = "RandomAI",
            SimulationCount = 10,
            SandboxSettings = new
            {
                MapWidth       = 10,
                MapHeight      = 10,
                BlocksPercent  = 0,
                EnemiesPercent = 0
            }
        };

        // Act – start the job (expect 202 Accepted)
        var startResponse = await _client.PostAsJsonAsync(
            "/ai-sandbox/simulation/run/mass", body, JsonOptions);

        Assert.AreEqual(HttpStatusCode.Accepted, startResponse.StatusCode,
            "Expected 202 Accepted when submitting a mass simulation job.");

        var started = await startResponse.Content.ReadFromJsonAsync<JobStartedDto>(JsonOptions);
        Assert.IsNotNull(started);
        Assert.AreNotEqual(Guid.Empty, started.JobId);

        // Poll until Completed or Failed (or 10s timeout)
        JobStatusDto? status = null;
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var statusResponse = await _client.GetAsync("/ai-sandbox/simulation/status");
            Assert.AreEqual(HttpStatusCode.OK, statusResponse.StatusCode);

            var statuses = await statusResponse.Content
                .ReadFromJsonAsync<List<JobStatusDto>>(JsonOptions);

            status = statuses?.FirstOrDefault(s => s.JobId == started.JobId);
            if (status?.State is "Completed" or "Failed")
                break;

            await Task.Delay(200);
        }

        // Assert
        Assert.IsNotNull(status, "Job status not found after polling.");
        Assert.AreEqual("Completed", status.State,
            $"Expected Completed but got {status.State}. Error: {status.ErrorMessage}");
        Assert.AreEqual(10, status.TotalRuns);
        Assert.AreEqual(10, status.CompletedRuns);
        Assert.IsNull(status.ErrorMessage);
    }

    // Minimal deserialization helpers scoped to this test class
    private sealed class JobStartedDto
    {
        public Guid JobId { get; set; }
    }

    private sealed class JobStatusDto
    {
        public Guid    JobId         { get; set; }
        public string? State         { get; set; }
        public string? ErrorMessage  { get; set; }
        public int     TotalRuns     { get; set; }
        public int     CompletedRuns { get; set; }
    }
}
