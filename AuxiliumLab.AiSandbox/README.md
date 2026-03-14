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
│  ConsolePresentation · GrpcHost · WebApi · Startup             │
├─────────────────────────────────────────────────────────────────┤
│  Application Services                                           │
│  ApplicationServices · AiTrainingOrchestrator                  │
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
| `AuxiliumLab.AiSandbox.ApplicationServices` | Application | Use-cases, executors, runners, jobs, commands, queries, persistence mappers |
| `AuxiliumLab.AiSandbox.Infrastructure` | Infrastructure | File & in-memory data managers |
| `AuxiliumLab.AiSandbox.Statistics` | Infrastructure | Batch run statistics, CSV export |
| `AuxiliumLab.AiSandbox.ConsolePresentation` | Presentation | Real-time Spectre.Console terminal renderer |
| `AuxiliumLab.AiSandbox.AiTrainingOrchestrator` | Application | Training settings, gRPC client to Python |
| `AuxiliumLab.AiSandbox.GrpcHost` | Presentation | gRPC server exposing simulation as a gym |
| `AuxiliumLab.AiSandbox.WebApi` | Presentation | ASP.NET Core REST API with Training, Simulation, Aggregation and Statistic endpoints |
| `AuxiliumLab.AiSandbox.Startup` | Composition Root | DI wiring, entry point, menu |
| `AuxiliumLab.AiSandbox.UnitTests` | Tests | MSTest unit test suite |
| `AuxiliumLab.AiSandbox.IntegrationTests` | Tests | MSTest end-to-end integration tests (WebApplicationFactory + live gRPC) |

## Build & Run

```powershell
# Build everything
dotnet build AuxiliumLab.AiSandbox.sln

# Run (interactive menu will appear)
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
| `CONSOLE_PRESENTATION_DEBUG` | Enables verbose debug output in the `ConsolePresentation` layer (frame timings, render traces) |
| `PERFORMANCE_ANALYSIS` | Activates coarse-grained performance instrumentation across hot paths |
| `PERFORMANCE_DETAILED_ANALYSIS` | Adds fine-grained per-step timing; implies `PERFORMANCE_ANALYSIS` overhead |

By default all three constants are **disabled** in every configuration. To enable them, edit `Directory.Build.props` and uncomment the `DefineConstants` line shown above.

## Execution Modes

| Mode | Description |
|---|---|
| `SingleSimulation` | One interactive run with console visualization |
| `MassRandomAISimulation` | Parallel batch runs without visualization |
| `MassTrainedAISimulation` | Parallel batch runs using a trained model |
| `Training` | Full RL training loop (requires Python service running) |
| `AggregationRun` | Ordered sequence of steps (Training → MassRandom → MassTrained) producing a combined CSV report |
| `TestPreconditions` | Run seeded from saved precondition data |

## REST API

When `IsWebApiEnabled = true` (set in `appsettings.json`) an ASP.NET Core API starts on `http://localhost:5000`. All routes are under `/ai-sandbox/`.

| Controller | Verb | Route | Description |
|---|---|---|---|
| `TrainingController` | POST | `/ai-sandbox/training/ppo` | Start a PPO training job (202 Accepted) |
| | GET | `/ai-sandbox/training/models` | List trained models |
| | GET | `/ai-sandbox/training/status` | Training job statuses |
| `SimulationController` | POST | `/ai-sandbox/simulation/run/single` | Start a single simulation (RandomAI or TrainedAI) |
| | POST | `/ai-sandbox/simulation/run/mass` | Start a mass simulation batch |
| | GET | `/ai-sandbox/simulation/status` | Simulation job statuses |
| `AggregationRunController` | POST | `/ai-sandbox/aggregation/run` | Start an aggregation run |
| | GET | `/ai-sandbox/aggregation/status` | Aggregation job statuses |
| `StatisticController` | GET | `/ai-sandbox/statistic/simulations` | Completed simulation summaries |
| | GET | `/ai-sandbox/statistic/aggregations` | Completed aggregation summaries |

## Documentation

| File | Contents |
|---|---|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Onion architecture, dependency graph, data flow |
| [docs/DOMAIN.md](docs/DOMAIN.md) | Domain model reference |
| [docs/APPLICATION_SERVICES.md](docs/APPLICATION_SERVICES.md) | Application layer execution logic |
| [docs/CONSOLE_PRESENTATION.md](docs/CONSOLE_PRESENTATION.md) | Console rendering system |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Contribution guidelines |
