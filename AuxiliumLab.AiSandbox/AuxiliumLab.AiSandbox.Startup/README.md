# AuxiliumLab.AiSandbox.Startup

**Onion layer: Composition Root**  
The application entry point and Dependency Injection root.  
Depends on: every other project in the solution.

## Purpose
- Wires all services together (composition root).
- Presents the interactive start-up menu.
- Selects the correct host type (generic host vs. Kestrel WebApplication) based on execution mode.
- Dispatches to the correct runner.

## Startup Sequence

```
Program.cs
  │
  ├─ 1. Read configuration files
  │       appsettings.json           →  StartupSettings
  │       training-settings.json     →  TrainingSettings
  │       aggregation-settings.json  →  AggregationSettings  (optional)
  │
  ├─ 2. Interactive menu (unless IsPreconditionStart = true)
  │       MenuRunner.ResolveSettings()
  │         ├─ Choose PresentationMode (Console / Web / WithoutVisualization)
  │         ├─ Choose ExecutionMode
  │         └─ Choose Algorithm (Training mode only)
  │
  ├─ 3. Build host
  │       Training, or AggregationRun containing a Training step
  │                     →  GrpcTrainingHost (Kestrel + HTTP/2 on :50062)
  │       All else      →  Host.CreateDefaultBuilder (pure generic host)
  │
  ├─ 4. Start ConsoleRunner (if Console mode)
  │
  ├─ 5. host.StartAsync()
  │
  ├─ 6. Optionally launch WebApiHost in background (if IsWebApiEnabled)
  │
  └─ 7. Dispatch on ExecutionMode:
          Training                   → TrainingRunner.RunTrainingAsync()
          SingleRandomAISimulation   → SingleRunner.RunSingleAsync(IExecutorForPresentation)
          MassRandomAISimulation     → MassRunner.RunManyAsync()
          SingleTrainedAISimulation  → (Console) SingleRunner.RunSingleAsync(IExecutorForPresentation)
                                       (Other)   SingleRunner.RunSingleTrainedAsync(IStandardExecutor)
          MassTrainedAISimulation    → MassRunner.RunManyAsync()
          TestPreconditions          → SingleRunner.RunTestPreconditionsAsync(IExecutorForPresentation)
          AggregationRun             → AggregationRunner.RunAggregationAsync()
```

## Execution Modes

| `ExecutionMode` | Host | Description | Status |
|---|---|---|---|
| `Training` | Kestrel (GrpcTrainingHost) | Full RL training with Python SB3 | ✅ |
| `SingleRandomAISimulation` | Generic | One run, random AI, optional console | ✅ |
| `MassRandomAISimulation` | Generic | Parallel batch runs with statistics | ✅ |
| `TestPreconditions` | Generic | Seeded run from saved precondition state | ✅ |
| `SingleTrainedAISimulation` | Generic | Single run using a trained model | ✅ |
| `MassTrainedAISimulation` | Generic | Batch runs using trained models | ✅ |
| `LoadSimulation` | Generic | Load and continue a saved simulation state | ⏭️ |
| `AggregationRun` | Generic | Runs a configurable sequence of jobs and produces a combined CSV report | ✅ |

## AggregationRun

`AggregationRun` lets you define an ordered sequence of jobs to be executed one after another in a single launch. When all steps finish, a combined comparison report is written to disk.

### How it works

1. On startup, `Program.cs` loads `aggregation-settings.json` (located next to the executable).
2. `AggregationRunner.RunAggregationAsync` iterates the step list in order:
   - **`Training`** — delegates to `TrainingRunner.RunTrainingAsync` and captures the algorithm name, experiment ID and hyperparameters as `TrainingRunInfo`.
   - **`MassRandomAISimulation`** — delegates to `MassRunner.RunManyAsync` (random AI) and captures the full `MassRunCapturedResult`.
   - **`MassTrainedAISimulation`** — delegates to `MassRunner.RunManyAsync` via `InferenceExecutorFactory`, which wires `InferenceActions` (uses the last trained model). Captures `MassRunCapturedResult`.
3. After all steps, `IStatisticFileDataManager.SaveAggregationReportAsync` is called to write the report.

### Configuration — `aggregation-settings.json`

The file lives in the `AuxiliumLab.AiSandbox.Startup` project and is copied to the output directory automatically.

```json
{
  "AggregationSettings": {
    "Steps": [
      { "Name": "Random AI", "Mode": "MassRandomAISimulation" },
      { "Name": "PPO - AI",  "Mode": "MassTrainedAISimulation" }
    ]
  }
}
```

> The `Training` step is **optional**. When omitted the aggregation uses whatever trained model already exists on disk for `MassTrainedAISimulation`. Add it as the first step when you want the run to train a fresh model before comparing.

Each step has two fields:

| Field | Description |
|---|---|
| `Name` | Human-readable label used as a column header in the CSV report |
| `Mode` | One of the `ExecutionMode` enum values (`Training`, `MassRandomAISimulation`, `MassTrainedAISimulation`) |

Steps are executed in the order they appear in the array. The `Training` step, if present, must appear before any `MassTrainedAISimulation` step so the model is ready.

For output file location and CSV report structure, see [AuxiliumLab.Statistics/README.md](../AuxiliumLab.Statistics/README.md).

## REST API (Web mode)

When `IsWebApiEnabled = true` a background `WebApiHost` starts on `http://localhost:5000`.  
See [AuxiliumLab.AiSandbox.WebApi/README.md](../AuxiliumLab.AiSandbox.WebApi/README.md) for the full endpoint reference.

## `RegisterCoreServices`
```csharp
services.AddEventAggregator();        // Common: IMessageBroker, IBrokerRpcClient
services.AddInfrastructureServices(); // Infrastructure: file & memory managers
services.AddDomainServices();         // Domain: IVisibilityService
services.AddApplicationServices();   // ApplicationServices: executors, commands, queries, jobs
services.AddAiSandboxServices(mode);  // AiTrainingOrchestrator: IAiActions, IPolicyTrainerClient
```

## `GrpcTrainingHost`
When `ExecutionMode = Training`, a `WebApplicationBuilder`-based host is used:
- Configures Kestrel on **port 50062** with HTTP/2 for gRPC.
- Registers `SimulationService` (gRPC).
- Calls `RegisterCoreServices` for the full DI stack.

## `MenuRunner`
Interactive console menu (uses plain `Console.ReadLine`).  
Overrides the defaults from `appsettings.json → StartupSettings`.  
Resides here (not in `ConsolePresentation`) to avoid a circular reference through `WebApi`.

### Menu Workflow

**Step 1 — Presentation type** (always shown)

```
1. Console
2. Web
3. Without visualization  (training and mass-run modes)
```

**Step 2 — Execution mode** (options depend on Step 1)

| Choice | `PresentationMode.WithoutVisualization` | `Console` or `Web` |
|---|---|---|
| 1 | Training | Single Random AI Simulation |
| 2 | Single Random AI Simulation | Single Trained AI Simulation |
| 3 | Single Trained AI Simulation | Load Simulation |
| 4 | Mass Random AI Simulation | Test Preconditions |
| 5 | Mass Trained AI Simulation | |
| 6 | Aggregation Run | |
| 7 | Load Simulation | |
| 8 | Test Preconditions | |

**Step 3 — Algorithm** (only when `ExecutionMode = Training`)

```
1. PPO
2. A2C
3. DQN
```

### Console presentation and `IExecutorForPresentation`

When `PresentationMode.Console` is selected, the simulation executor **must** publish events to the
shared `IMessageBroker` so that `ConsoleRunner` can subscribe and render them.  `IExecutorForPresentation`
uses the shared singleton broker for this purpose, whereas `IStandardExecutor` creates a private
broker per simulation (for parallelism isolation in mass runs).

Consequently, for every single-run mode with console presentation, `IExecutorForPresentation` is resolved
from the DI container:

| ExecutionMode | Console path | Non-console path |
|---|---|---|
| `SingleRandomAISimulation` | `IExecutorForPresentation` → `RunSingleAsync` | same |
| `SingleTrainedAISimulation` | `IExecutorForPresentation` → `RunSingleAsync` | `IStandardExecutor` → `RunSingleTrainedAsync` |
| `TestPreconditions` | `IExecutorForPresentation` → `RunTestPreconditionsAsync` | same |

For `SingleTrainedAISimulation`, `IAiActions` is overridden to `InferenceActions` in the DI
container before the host is built, so resolving `IExecutorForPresentation` from the scope
automatically uses the trained model while still publishing broker events for the console to render.

## `appsettings.json` Key Settings

| Key | Default | Description |
|---|---|---|
| `StartupSettings.IsPreconditionStart` | `true` | Skip menu and use settings from file directly |
| `StartupSettings.ExecutionMode` | `MassRandomAISimulation` | Default mode when skipping menu |
| `StartupSettings.PresentationMode` | `WithoutVisualization` | Default presentation |
| `StartupSettings.StandardSimulationCount` | `0` | Number of standard batch runs |
| `SandBox.MaxTurns.Current` | `10` | Default turn limit |
| `SandBox.MapSettings.Size.Width/Height.Current` | `20` | Default map size |
| `PolicyTrainerClient.ServerAddress` | `http://localhost:50051` | Python service address |

## DI Lifetime Conventions

| Service | Lifetime | Reason |
|---|---|---|
| `IMessageBroker` | Singleton | All executors and presenters share one bus |
| `IMemoryDataManager<T>` | Singleton | Shared in-memory store across executors |
| `IFileDataManager<T>` | Scoped | Each scope (executor) gets its own file-access instance |
| `IPlaygroundCommandsHandleService` | Scoped | Per-execution to carry the active playground |
| `IExecutorForPresentation` | Scoped | One per simulation run |
