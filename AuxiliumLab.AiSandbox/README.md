# AuxiliumLab.AiSandbox

A grid-based simulation engine written in **.NET 9** for testing and training reinforcement-learning agents.  
The solution follows **Onion Architecture** (also known as Clean / Hexagonal Architecture).

## Goals
- Provide a deterministic, configurable grid world for RL experiments.
- Support multiple execution modes: interactive console, batch simulation, AI training, and REST API.
- Bidirectional gRPC integration with a Python RL training service (Stable Baselines3).

## Non-goals
- No real-time graphics.
- No multiplayer.

## Onion Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│  Presentation / Hosts                                           │
│  GrpcHost · WebApi · SharedContracts · Startup                   │
├─────────────────────────────────────────────────────────────────┤
│  Application Services                                           │
│  ApplicationServices · Ai                  │
├─────────────────────────────────────────────────────────────────┤
│  Infrastructure (implements domain ports)                       │
│  Infrastructure · Statistics                                    │
├─────────────────────────────────────────────────────────────────┤
│  Domain (innermost — no outward dependencies)                   │
│  Domain · Common · SharedBaseTypes                              │
└─────────────────────────────────────────────────────────────────┘
```

Dependency rule: **inner layers never reference outer layers**.  
The `Domain` project has zero dependencies on Infrastructure, Application, or Presentation.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full dependency graph.

## Solution Structure

| Project | Layer | Purpose |
|---|---|---|
| `AuxiliumLab.AiSandbox.Domain` | Domain | Map, agents, game rules, vision |
| `AuxiliumLab.AiSandbox.SharedBaseTypes` | Domain | Value objects, enums, message contracts |
| `AuxiliumLab.AiSandbox.Common` | Domain/Cross-cutting | In-process pub/sub message broker |
| `AuxiliumLab.AiSandbox.Ai` | Application | AI decision layer, training orchestration, gRPC client to Python |
| `AuxiliumLab.AiSandbox.ApplicationServices` | Application | Use-cases, executors, runners, jobs, commands, queries, persistence mappers |
| `AuxiliumLab.AiSandbox.Infrastructure` | Infrastructure | File & in-memory data managers |
| `AuxiliumLab.AiSandbox.Statistics` | Infrastructure | Batch run statistics, CSV export |
| `AuxiliumLab.AiSandbox.SharedContracts` | Presentation | Cross-cutting DTOs shared between backend and frontend |
| `AuxiliumLab.AiSandbox.GrpcHost` | Presentation | gRPC server exposing simulation as a gym |
| `AuxiliumLab.AiSandbox.WebApi` | Presentation | ASP.NET Core REST API with Training, Simulation, Aggregation and Statistic endpoints |
| `AuxiliumLab.AiSandbox.Startup` | Composition Root | DI wiring, entry point |
| `AuxiliumLab.Frontend` | Frontend | Blazor WebAssembly SPA (MudBlazor, SignalR, ApexCharts) |
| `AuxiliumLab.AiSandbox.UnitTests` | Tests | MSTest unit test suite |
| `AuxiliumLab.AiSandbox.IntegrationTests` | Tests | MSTest end-to-end integration tests (WebApplicationFactory + live gRPC) |
| `AuxiliumLab.AiSandbox.Frontend.UnitTests` | Tests | bUnit + MSTest Blazor component unit tests |

## Build & Run

```powershell
# Build everything
dotnet build AuxiliumLab.AiSandbox.sln

# Run (REST API on :5000, gRPC gym on :50062)
dotnet run --project AuxiliumLab.AiSandbox.Startup
```

## Build Configuration — Directory.Build.props

Solution-wide MSBuild properties are defined in `Directory.Build.props` at the solution root.  
In **Debug** builds you can opt-in to additional diagnostic constants by uncommenting the relevant line:

```xml
<DefineConstants>$(DefineConstants);CONSOLE_PRESENTATION_DEBUG;PERFORMANCE_ANALYSIS;PERFORMANCE_DETAILED_ANALYSIS</DefineConstants>
```

| Constant | Effect |
|---|---|
| `CONSOLE_PRESENTATION_DEBUG` | Enables verbose debug output in `Executor` (frame timings, render traces) |
| `PERFORMANCE_ANALYSIS` | Activates coarse-grained performance instrumentation across hot paths |
| `PERFORMANCE_DETAILED_ANALYSIS` | Adds fine-grained per-step timing; implies `PERFORMANCE_ANALYSIS` overhead |

By default all three constants are **disabled** in every configuration. To enable them, edit `Directory.Build.props` and uncomment the `DefineConstants` line shown above.

## Execution Modes

| Mode | Description |
|---|---|
| `Training` | Full RL training loop (requires Python service running) |
| `SingleRandomAISimulation` | One run with random agent actions |
| `SingleTrainedAISimulation` | One run using a trained model |
| `MassRandomAISimulation` | Parallel batch runs with random AI |
| `MassTrainedAISimulation` | Parallel batch runs using a trained model |
| `LoadSimulation` | Play back a previously recorded simulation |
| `TestPreconditions` | Generate playground with preconditions and test them |
| `AggregationRun` | Ordered sequence of steps (Training → MassRandom → MassTrained) producing a combined CSV report |

## REST API

The REST API runs on `http://localhost:5000`. All routes are under `/ai-sandbox/`.

| Controller | Verb | Route | Description |
|---|---|---|---|
| `TrainingController` | POST | `/ai-sandbox/training/ppo` | Start a PPO training job (202 Accepted) |
| | GET | `/ai-sandbox/training/models` | List trained models |
| | GET | `/ai-sandbox/training/status` | Training job statuses |
| | POST | `/ai-sandbox/training/{jobId}/stop` | Stop a training job |
| `SimulationController` | POST | `/ai-sandbox/simulation/run/single` | Start a single simulation (RandomAI or TrainedAI) |
| | POST | `/ai-sandbox/simulation/run/mass` | Start a mass simulation batch |
| | POST | `/ai-sandbox/simulation/{jobId}/stop` | Stop a simulation job |
| | POST | `/ai-sandbox/simulation/{jobId}/pause` | Pause a simulation job |
| | POST | `/ai-sandbox/simulation/{jobId}/resume` | Resume a paused simulation job |
| | GET | `/ai-sandbox/simulation/status` | Simulation job statuses |
| `AggregationRunController` | POST | `/ai-sandbox/aggregation/run` | Start an aggregation run |
| | GET | `/ai-sandbox/aggregation/status` | Aggregation job statuses |
| | POST | `/ai-sandbox/aggregation/{jobId}/stop` | Stop an aggregation run |
| `StatisticController` | GET | `/ai-sandbox/statistic/simulations` | Completed simulation summaries |
| | GET | `/ai-sandbox/statistic/aggregations` | Completed aggregation summaries |

## Documentation

| File | Contents |
|---|---|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Onion architecture, dependency graph, data flow |
| [AuxiliumLab.AiSandbox.Domain/README.md](AuxiliumLab.AiSandbox.Domain/README.md) | Domain model reference |
| [AuxiliumLab.AiSandbox.ApplicationServices/README.md](AuxiliumLab.AiSandbox.ApplicationServices/README.md) | Application layer execution logic |
| [AuxiliumLab.AiSandbox.SharedContracts/README.md](AuxiliumLab.AiSandbox.SharedContracts/README.md) | Cross-cutting DTOs shared between backend and frontend |
| [AuxiliumLab.AiSandbox.Ai/README.md](AuxiliumLab.AiSandbox.Ai/README.md) | AI decision layer: actions, observation encoding, reward scheme |
