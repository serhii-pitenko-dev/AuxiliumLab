using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuxiliumLab.AiSandbox.IntegrationTests.AggregationRun;

[TestClass]
public sealed class AggregationRunControllerTests
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
    public async Task GetAggregationStatus_ReturnsOk()
    {
        var response = await _client.GetAsync("/ai-sandbox/aggregation/status");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Runs a full aggregation sequence:
    ///   1. PPO Training  (100 timesteps, 1 gym, 10×10 map)
    ///   2. Mass Random AI Simulation  (3 runs)
    ///   3. Mass Trained AI Simulation (3 runs using the model trained in step 1)
    ///
    /// Requires the Python gRPC training service to be running on port 50051.
    /// All artifacts are stored in the per-test temp directory and cleaned up by
    /// TestCleanup/ClassCleanup via <see cref="AiSandboxWebApplicationFactory.CleanArtifacts"/>.
    /// </summary>
    [TestMethod]
    public async Task FullAggregation_PPOTraining_RandomAI_TrainedAI_CompletesSuccessfully()
    {
        // Arrange – 3-step pipeline with minimal settings so the test finishes quickly.
        var body = new
        {
            Steps = new[]
            {
                new { Name = "PPO Training",  Mode = "Training" },
                new { Name = "Random AI",     Mode = "MassRandomAISimulation" },
                new { Name = "PPO - AI",      Mode = "MassTrainedAISimulation" }
            },
            StandardSimulationCount = 3,
            Algorithm  = "PPO",
            PolicyType = "MLP",
            // Keep training fast: 1 gym, tiny rollout, 1 epoch.
            TrainingOverrides = new
            {
                Hyperparameters = new
                {
                    TotalTimesteps = 100,
                    NEnvs          = 1,
                    NSteps         = 16,
                    BatchSize      = 16,
                    NEpochs        = 1
                },
                SandboxSettings = new
                {
                    MapWidth       = 10,
                    MapHeight      = 10,
                    BlocksPercent  = 0,
                    EnemiesPercent = 0
                }
            }
        };

        // Act – start the aggregation job (expect 202 Accepted)
        var startResponse = await _client.PostAsJsonAsync(
            "/ai-sandbox/aggregation/run", body, JsonOptions);

        Assert.AreEqual(HttpStatusCode.Accepted, startResponse.StatusCode,
            "Expected 202 Accepted when submitting an aggregation run.");

        var started = await startResponse.Content
            .ReadFromJsonAsync<AggregationJobStartedDto>(JsonOptions);
        Assert.IsNotNull(started);
        Assert.AreNotEqual(Guid.Empty, started.JobId);
        Assert.AreEqual(3, started.StepNames?.Count,
            "Expected 3 step names in the job descriptor.");

        // Poll until terminal state (max 5 minutes: training + 2 mass sims)
        AggregationJobStatusDto? status = null;
        var deadline = DateTime.UtcNow.AddMinutes(5);
        while (DateTime.UtcNow < deadline)
        {
            var statusResponse = await _client.GetAsync("/ai-sandbox/aggregation/status");
            Assert.AreEqual(HttpStatusCode.OK, statusResponse.StatusCode);

            var statuses = await statusResponse.Content
                .ReadFromJsonAsync<List<AggregationJobStatusDto>>(JsonOptions);

            status = statuses?.FirstOrDefault(s => s.JobId == started.JobId);
            if (status?.State is "Completed" or "Failed")
                break;

            await Task.Delay(500);
        }

        // Assert
        Assert.IsNotNull(status, "Aggregation job status not found after polling.");
        Assert.AreEqual("Completed", status.State,
            $"Expected Completed but got '{status.State}'. " +
            $"Error: {status.ErrorMessage ?? "(none)"}\n" +
            "Ensure the Python gRPC training service is running on port 50051.");
        Assert.IsNull(status.ErrorMessage,
            $"Expected no error message, but got: {status.ErrorMessage}");
        Assert.AreEqual(3, status.CompletedSteps,
            $"Expected all 3 steps to be completed, but only {status.CompletedSteps} were.");
        Assert.IsNotNull(status.CompletedAt);
    }

    // ── Minimal deserialization helpers ────────────────────────────────────────

    private sealed class AggregationJobStartedDto
    {
        public Guid              JobId      { get; set; }
        public List<string>?     StepNames  { get; set; }
        public DateTime          StartedAt  { get; set; }
    }

    private sealed class AggregationJobStatusDto
    {
        public Guid      JobId          { get; set; }
        public string?   State          { get; set; }
        public string?   ErrorMessage   { get; set; }
        public int       CompletedSteps { get; set; }
        public DateTime? CompletedAt    { get; set; }
    }
}
