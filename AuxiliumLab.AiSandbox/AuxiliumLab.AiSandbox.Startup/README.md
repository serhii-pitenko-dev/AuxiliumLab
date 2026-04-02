# AuxiliumLab.AiSandbox.Startup

**Onion layer: Composition Root**  
The application entry point and Dependency Injection root.  
Depends on: every other project in the solution.

## Purpose
- Wires all services together (composition root).
- Builds the unified Kestrel host (REST + gRPC).
- All execution modes (Training, Simulation, Aggregation) are triggered exclusively via the **REST API** from the Blazor Frontend.

## Startup Sequence

```
Program.cs
  │
  ├─ 1. Read appsettings.json
  │       ├─ FileSource           (storage paths, precreated map toggle)
  │       └─ PolicyTrainerClient  (gRPC address for Python RL service)
  │
  ├─ 2. Ensure storage folders exist (FileSource.FileStorage.BasePath)
  │
  ├─ 3. Build unified host via GrpcTrainingHost.Build()
  │       ├─ REST API on port 5000 (HTTP/1.1)
  │       └─ gRPC gym server on port 50062 (HTTP/2)
  │
  ├─ 4. Map SignalR hub (/hubs/simulation)
  │
  └─ 5. host.RunAsync()
```

## Execution Modes

All modes are triggered via REST API endpoints from the Frontend:

| `ExecutionMode` | API Endpoint | Description |
|---|---|---|
| `Training` | `POST /ai-sandbox/training/ppo` | Full RL training with Python SB3 |
| `SingleRandomAISimulation` | `POST /ai-sandbox/simulation/run/single` | One run with random AI |
| `SingleTrainedAISimulation` | `POST /ai-sandbox/simulation/run/single` | One run with trained model |
| `MassRandomAISimulation` | `POST /ai-sandbox/simulation/run/mass` | Parallel batch runs (random AI) |
| `MassTrainedAISimulation` | `POST /ai-sandbox/simulation/run/mass` | Parallel batch runs (trained model) |
| `AggregationRun` | `POST /ai-sandbox/aggregation/run` | Multi-step pipeline producing CSV report |

## REST API

The API starts on `http://localhost:5000`.  
See [AuxiliumLab.AiSandbox.WebApi/README.md](../AuxiliumLab.AiSandbox.WebApi/README.md) for the full endpoint reference.

## DI Registration
```csharp
services.AddEventAggregator();        // Common: IMessageBroker, IBrokerRpcClient
services.AddInfrastructureServices(); // Infrastructure: file & memory managers, AggregationSettings
services.AddPolicyTrainerClient();    // AiTrainingOrchestrator: gRPC client to Python
services.AddDomainServices();         // Domain: IVisibilityService
services.AddAiSandboxServices(mode);  // AiTrainingOrchestrator: IAiActions
services.AddApplicationServices();    // ApplicationServices: executors, commands, queries, jobs
services.AddWebApiPresentationServices(); // WebApi: controllers
```

## `GrpcTrainingHost`
Builds a `WebApplication` with Kestrel configured for dual-protocol:
- **Port 5000** — HTTP/1.1 for REST controllers.
- **Port 50062** — HTTP/2 for gRPC gym server (`SimulationService`).
- Reads `TrainingSettings` from `appsettings.json` and registers as singleton.

## `appsettings.json` Key Settings

| Key | Default | Description |
|---|---|---|
| `PolicyTrainerClient.ServerAddress` | `http://localhost:50051` | Python RL training service address |
| `FileSource.FileStorage.BasePath` | `C:\FILE_STORAGE` | Root storage directory |
| `FileSource.PrecreatedMap.IsEnabled` | `false` | Load a pre-saved playground by ID |
| `FileSource.PrecreatedMap.PlaygroundId` | `""` | Playground GUID to load when enabled |

> **Note:** Sandbox settings (map size, turn limits, agent stats), training hyperparameters (rewards, algorithms), and aggregation steps are **not** stored in `appsettings.json`. They are provided via the REST API command DTOs (e.g. `StartPpoTrainingCommand`, `StartSingleSimulationCommand`) and use code-level defaults when not specified.

## DI Lifetime Conventions

| Service | Lifetime | Reason |
|---|---|---|
| `IMessageBroker` | Singleton | All executors and presenters share one bus |
| `IMemoryDataManager<T>` | Singleton | Shared in-memory store across executors |
| `IFileDataManager<T>` | Scoped | Each scope (executor) gets its own file-access instance |
| `IPlaygroundCommandsHandleService` | Scoped | Per-execution to carry the active playground |
| `IExecutorForPresentation` | Scoped | One per simulation run |
