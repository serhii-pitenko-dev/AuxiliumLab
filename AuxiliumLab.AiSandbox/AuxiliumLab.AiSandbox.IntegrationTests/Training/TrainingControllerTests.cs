using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuxiliumLab.AiSandbox.IntegrationTests.Training;

[TestClass]
public sealed class TrainingControllerTests
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
    public async Task GetTrainedModels_ReturnsOk()
    {
        var response = await _client.GetAsync("/ai-sandbox/training/models");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetTrainingStatus_ReturnsOk()
    {
        var response = await _client.GetAsync("/ai-sandbox/training/status");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Submits a minimal PPO training job (100 timesteps, 10×10 map, no blocks, no enemies)
    /// and waits for it to complete. Requires the Python gRPC training service to be running.
    /// </summary>
    [TestMethod]
    public async Task PpoTraining_100Timesteps_10x10Map_CompletesSuccessfully()
    {
        // Arrange – 1 gym, tiny rollout (16 steps), 1 epoch → fast finish
        var body = new
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
        };

        // Act – start the job (expect 202 Accepted)
        var startResponse = await _client.PostAsJsonAsync(
            "/ai-sandbox/training/ppo", body, JsonOptions);

        Assert.AreEqual(HttpStatusCode.Accepted, startResponse.StatusCode,
            "Expected 202 Accepted when submitting a PPO training job.");

        var started = await startResponse.Content.ReadFromJsonAsync<JobStartedDto>(JsonOptions);
        Assert.IsNotNull(started);
        Assert.AreNotEqual(Guid.Empty, started.JobId);
        Assert.AreEqual("PPO", started.Algorithm);

        // Poll until terminal state (max 3 minutes – gRPC startup + gym env spin-up + training)
        JobStatusDto? status = null;
        var deadline = DateTime.UtcNow.AddMinutes(3);
        while (DateTime.UtcNow < deadline)
        {
            var statusResponse = await _client.GetAsync("/ai-sandbox/training/status");
            Assert.AreEqual(HttpStatusCode.OK, statusResponse.StatusCode);

            var statuses = await statusResponse.Content
                .ReadFromJsonAsync<List<JobStatusDto>>(JsonOptions);

            status = statuses?.FirstOrDefault(s => s.JobId == started.JobId);
            if (status?.State is "Completed" or "Failed")
                break;

            await Task.Delay(500);
        }

        // Assert
        Assert.IsNotNull(status, "Job status not found after polling.");
        Assert.AreEqual("Completed", status.State,
            $"Expected Completed but got {status.State}. " +
            $"Error: {status.ErrorMessage ?? "(none)"}\n" +
            "Ensure the Python gRPC training service is running on port 50051.");
        Assert.IsNull(status.ErrorMessage);
        Assert.IsNotNull(status.CompletedAt);
    }

    // Minimal deserialization helpers
    private sealed class JobStartedDto
    {
        public Guid   JobId     { get; set; }
        public string Algorithm { get; set; } = string.Empty;
    }

    private sealed class JobStatusDto
    {
        public Guid     JobId        { get; set; }
        public string?  State        { get; set; }
        public string?  ErrorMessage { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
