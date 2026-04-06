# AuxiliumLab.AiSandbox.IntegrationTests

**Onion layer: Tests**  
MSTest end-to-end integration tests that spin up the full ASP.NET Core application in-process and exercise the REST API over real HTTP.  
Depends on: `WebApi` (via `WebApplicationFactory<Program>`), `ApplicationServices`, `Ai`.

## Prerequisites

Training tests require the **Python gRPC RL service** to be running on `localhost:50051`:

```powershell
# From the auxiliumlab-rl-service-baselines3/ folder:
python server.py
```

All non-training tests (simulation, statistic, aggregation status queries) run without the Python service.

## Running Tests

```powershell
cd AuxiliumLab.AiSandbox
dotnet test AuxiliumLab.AiSandbox.IntegrationTests\AuxiliumLab.AiSandbox.IntegrationTests.csproj
```

Or run from VS Code via the Test Explorer panel.

Tests run **sequentially** within each class (MSTest default, no parallelization attribute).  
Classes run in alphabetical order: `AggregationRun` → `Simulation` → `Statistic` → `Training`.

## Infrastructure — `AiSandboxWebApplicationFactory`

A custom `WebApplicationFactory<Program>` that:

1. **Redirects file storage** to a fresh `%TEMP%\AiSandboxTests_{guid}\` folder so tests never touch production data.
2. **Replaces the `TrainingSettings` singleton** with lightweight test defaults (`total_timesteps=100, n_envs=1, n_steps=16`) so all training tests complete in ~1 second regardless of `appsettings.json`.
3. **Starts a real Kestrel gRPC listener on port 50062** (HTTP/2) so the Python RL service can call back into the C# gym service during training tests.

### `CleanArtifacts()`

Deletes all **files** (not directories) under the temp storage path.  
Called from `[TestCleanup]` in every test class to prevent cross-test artifact pollution.  
Directories are intentionally preserved so singleton-cached folder paths (e.g. `StatisticFileDataManager`) remain valid within a test class lifetime.

## Test Coverage

### `AggregationRunControllerTests`

| Test | Endpoint | Description |
|---|---|---|
| `GetAggregationStatus_ReturnsOk` | `GET /ai-sandbox/aggregation/status` | Empty status list returns 200 |
| `FullAggregation_PPOTraining_RandomAI_TrainedAI_CompletesSuccessfully` | `POST /ai-sandbox/aggregation/run` + polling | 3-step pipeline: PPO train (100 steps, 1 env) → MassRandom (3 runs) → MassTrained (3 runs); asserts `State == Completed`, `CompletedSteps == 3` |

### `SimulationControllerTests`

| Test | Endpoint | Description |
|---|---|---|
| `GetSimulationStatus_ReturnsOk` | `GET /ai-sandbox/simulation/status` | Empty status list returns 200 |
| `MassSimulation_RandomAI_10Runs_10x10Map_CompletesSuccessfully` | `POST /ai-sandbox/simulation/run/mass` + polling | 10 random-AI runs on 10×10 map; asserts job completes with `CompletedRuns == 10` |

### `StatisticControllerTests`

| Test | Endpoint | Description |
|---|---|---|
| `GetCompletedSimulations_ReturnsOk` | `GET /ai-sandbox/statistic/simulations` | Returns 200 |
| `GetCompletedAggregations_ReturnsOk` | `GET /ai-sandbox/statistic/aggregations` | Returns 200 |

### `TrainingControllerTests`

| Test | Endpoint | Description |
|---|---|---|
| `GetTrainedModels_ReturnsOk` | `GET /ai-sandbox/training/models` | Returns 200 |
| `GetTrainingStatus_ReturnsOk` | `GET /ai-sandbox/training/status` | Returns 200 |
| `PpoTraining_100Timesteps_10x10Map_CompletesSuccessfully` | `POST /ai-sandbox/training/ppo` + polling | PPO job with 100 timesteps; asserts `State == Completed` within 3 minutes |

## Project Structure

```
AuxiliumLab.AiSandbox.IntegrationTests/
├── AiSandboxWebApplicationFactory.cs   Shared factory (file storage override, TrainingSettings override, gRPC port)
├── AggregationRun/
│   └── AggregationRunControllerTests.cs
├── Simulation/
│   └── SimulationControllerTests.cs
├── Statistic/
│   └── StatisticControllerTests.cs
└── Training/
    └── TrainingControllerTests.cs
```

## Conventions

- **Framework:** MSTest (`[TestClass]`, `[TestMethod]`)
- **Pattern:** `[ClassInitialize]` / `[ClassCleanup]` for one `HttpClient` per class; `[TestCleanup]` calls `CleanArtifacts()`.
- **Polling:** Tests that trigger async jobs poll `GET .../status` with a deadline (3–5 minutes) and a 500 ms interval.
- **Naming:** `Scenario_ExpectedOutcome` or `ControllerFeature_Scenario_ExpectedOutcome`.
- **No mocking** — all services including job runners, file storage, and (for training tests) the Python gRPC backend are real.

## Adding New Integration Tests

1. Create a `[TestClass]` in the appropriate feature folder.
2. Use `AiSandboxWebApplicationFactory` to get an `HttpClient`.
3. Add `[TestCleanup] public void TestCleanup() => _factory.CleanArtifacts();`.
4. For jobs that complete asynchronously, poll the corresponding `GET .../status` endpoint until the job reaches a terminal state (`Completed` or `Failed`).
