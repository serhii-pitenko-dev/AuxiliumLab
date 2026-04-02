# AuxiliumLab.AiSandbox.WebApi

**Onion layer: Presentation / Host**  
ASP.NET Core REST API that runs alongside the simulation engine.  
Depends on: `SharedContracts`, `ApplicationServices`, `AiTrainingOrchestrator`, `Ai`, `Infrastructure`, `Statistics`.

## Purpose
Provides an HTTP REST API entry point for the Blazor Frontend to trigger training runs, simulations, aggregation pipelines, and query results.

## Running
The Web API is hosted via `GrpcTrainingHost.Build()` in `Startup/Program.cs`, running on port 5000.
Controllers are registered via `AddWebApiPresentationServices()` and mapped with `app.MapControllers()`.

## `WebApiHost`
Static entry point that builds and runs the `WebApplication`:
```csharp
WebApiHost.RunAsync(args, cancellationToken)
```
- Calls `builder.Services.AddWebApiPresentationServices()` for controller and service registration.
- Maps controllers via `app.MapControllers()`.

## Folder Structure
```
WebApi/
├── Features/
│   ├── Training/            TrainingController
│   ├── Simulation/          SimulationController
│   ├── AggregationRun/      AggregationRunController
│   └── Statistic/           StatisticController
├── Configuration/           WebApiServiceCollectionExtensions
├── WebApiHost.cs
└── Program.cs               (standalone entry point: await WebApiHost.RunAsync(args))
```

## REST Endpoints

All routes are prefixed with `/ai-sandbox/`.  
Every write operation returns **202 Accepted** immediately (fire-and-forget job pattern).

### Training — `/ai-sandbox/training`

| Verb | Route | Body / Query | Response | Description |
|---|---|---|---|---|
| POST | `/ppo` | `StartPpoTrainingCommand` | `TrainingJobStartedDto` (202) | Start a PPO training run |
| GET | `/models` | — | `TrainedModelInfoDto[]` | List all trained models on disk |
| GET | `/status` | — | `TrainingJobStatusDto[]` | Status of all training jobs |
| POST | `/{jobId}/stop` | — | 202 | Stop a running training job |

**`StartPpoTrainingCommand`** fields (all optional — fall back to `appsettings.json → TrainingSettings` defaults):

| Field | Type | Description |
|---|---|---|
| `Hyperparameters.TotalTimesteps` | `int?` | Total environment steps |
| `Hyperparameters.NEnvs` | `int?` | Parallel gym environments (default = physical cores) |
| `Hyperparameters.NSteps` | `int?` | Rollout steps per env |
| `Hyperparameters.BatchSize` | `int?` | Mini-batch size |
| `Hyperparameters.NEpochs` | `int?` | Epochs per update |
| `Hyperparameters.LearningRate` | `double?` | Adam LR |
| `Hyperparameters.Gamma` | `double?` | Discount factor |
| `Hyperparameters.GaeLambda` | `double?` | GAE lambda |
| `Hyperparameters.ClipRange` | `double?` | PPO clip range |
| `Hyperparameters.EntCoef` | `double?` | Entropy coefficient |
| `Hyperparameters.Seed` | `int?` | Random seed |
| `SandboxSettings.*` | `int?/double?` | Map size, turn limit, block/enemy density, hero sight, speed, stamina |
| `RewardSettings.StepPenalty` | `float?` | Reward per survived step |
| `RewardSettings.WinReward` | `float?` | Reward on exit reached |
| `RewardSettings.LossReward` | `float?` | Reward on loss/timeout |

### Simulation — `/ai-sandbox/simulation`

| Verb | Route | Body | Response | Description |
|---|---|---|---|---|
| POST | `/run/single` | `StartSingleSimulationCommand` | `SimulationJobStartedDto` (202) | Single simulation run |
| POST | `/run/mass` | `StartMassSimulationCommand` | `SimulationJobStartedDto` (202) | Mass (batch) simulation run |
| POST | `/{jobId}/stop` | — | 202 | Stop a simulation job |
| POST | `/{jobId}/pause` | — | 202 | Pause a simulation job |
| POST | `/{jobId}/resume` | — | 202 | Resume a paused simulation job |
| GET | `/status` | — | `SimulationJobStatusDto[]` | Status of all simulation jobs |

**`StartSingleSimulationCommand`**:

| Field | Type | Description |
|---|---|---|
| `Kind` | `RandomAI \| TrainedAI` | Agent type to use |
| `Algorithm` | `PPO \| A2C \| DQN` | Algorithm for TrainedAI (ignored for RandomAI) |
| `SandboxSettings` | `SimulationSandboxOverrideDto?` | Optional map/agent overrides |

**`StartMassSimulationCommand`** adds:

| Field | Type | Description |
|---|---|---|
| `SimulationCount` | `int` | Standard parallel runs (default 100) |
| `IncrementalSweep` | `IncrementalSweeperDto?` | Optional incremental property sweep |

### Aggregation Run — `/ai-sandbox/aggregation`

| Verb | Route | Body | Response | Description |
|---|---|---|---|---|
| POST | `/run` | `StartAggregationCommand` | `AggregationJobStartedDto` (202) | Start a multi-step aggregation pipeline |
| POST | `/{jobId}/stop` | — | 202 | Stop an aggregation run |
| GET | `/status` | — | `AggregationJobStatusDto[]` | Status of all aggregation jobs |

**`StartAggregationCommand`**:

| Field | Type | Description |
|---|---|---|
| `Steps` | `AggregationStepDto[]` | Ordered steps; empty = use `appsettings.json → AggregationSettings` |
| `StandardSimulationCount` | `int` | Runs per mass-run step |
| `Algorithm` | `ModelType` | Algorithm for training/trained steps |
| `PolicyType` | `AiPolicy` | Policy network type (e.g. `MLP`) |
| `IncrementalSweep` | `AggregationIncrementalSweeperDto?` | Optional sweep |
| `TrainingOverrides` | `StartPpoTrainingCommand?` | Hyperparameter overrides for the Training step |

### Statistic — `/ai-sandbox/statistic`

| Verb | Route | Response | Description |
|---|---|---|---|
| GET | `/simulations` | `CompletedSimulationRunDto[]` | All completed simulation summaries |
| GET | `/aggregations` | `CompletedAggregationRunDto[]` | All completed aggregation summaries with per-step results |

## Configuration
Add Kestrel port configuration to `appsettings.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:5000" }
    }
  }
}
```

## Adding a New Endpoint
1. Create a controller under `Features/<FeatureName>/` inheriting `ControllerBase`.
2. Inject the relevant `IXxxCommands` / `IXxxQueries` interfaces from `ApplicationServices`.
3. Register any additional services inside `AddWebApiPresentationServices()`.
4. The controller is auto-discovered by `MapControllers()` (same assembly as `WebApiHost`).
