# AI_GUIDELINES — AuxiliumLab Repository

> These guidelines are for AI-assisted development tools (Copilot, agents, etc.).
> Read this file before implementing any feature or making structural changes.

---

## 1. Project Overview

This repository contains two top-level sub-solutions:

| Folder | Technology | Role |
|---|---|---|
| `AuxiliumLab.AiSandbox/` | .NET 9, C# | Grid-based simulation engine + REST API + gRPC host |
| `auxiliumlab-rl-service-baselines3/` | Python 3, Stable Baselines3 | gRPC RL training service |
| `AuxiliumLab.AiSandbox/Frontend/` | .NET 10, Blazor WebAssembly | SPA frontend (MudBlazor, SignalR, ApexCharts) |

**What the system does:** A configurable 2D grid world where a Hero agent learns (via PPO, A2C, or DQN) to navigate obstacles and enemies to reach the Exit tile. The .NET engine acts as a Gymnasium-compatible environment; Python SB3 does the RL training; the Blazor frontend visualises results.

---

## 2. Architectural Style

### .NET Solution — Onion Architecture (Clean / Hexagonal)

```
┌──────────────────────────────────────────────────────────────┐
│  Presentation / Hosts                                        │
│  ConsolePresentation · GrpcHost · WebApi · Startup          │
├──────────────────────────────────────────────────────────────┤
│  Application                                                 │
│  ApplicationServices · Ai · AiTrainingOrchestrator          │
├──────────────────────────────────────────────────────────────┤
│  Infrastructure                                              │
│  Infrastructure · Statistics                                 │
├──────────────────────────────────────────────────────────────┤
│  Domain (innermost — no outward dependencies)                │
│  Domain · Common · SharedBaseTypes                           │
└──────────────────────────────────────────────────────────────┘
```

**The dependency rule is absolute: inner layers never reference outer layers.**

### Python Service — Layered

```
transport/   ← gRPC server, servicers
core/        ← business logic (algorithms, training, env)
infra/       ← infrastructure (config, storage, adapters)
```

---

## 3. Project Responsibilities

### Domain layer

| Project | Responsibility |
|---|---|
| `SharedBaseTypes` | Enums (`ObjectType`, `AgentAction`, `ModelType`), value objects (`Coordinates`, `AgentSnapshot`), message-type base classes (`Command`, `Event`, `Query`, `Response`). No logic. |
| `Common` | `IMessageBroker` — in-process pub/sub bus. `IBrokerRpcClient` — request/response wrapper over the broker. All message contracts (`AiContract`, `CoreServicesContract`, `GlobalMessagesContract`, `Sb3Contract`). |
| `Domain` | `StandardPlayground` (aggregate root), map grid (`MapSquareCells`, `Cell`), agents (`Hero`, `Enemy`), inanimate objects (`Block`, `BorderBlock`, `EmptyCell`, `Exit`), `VisibilityService` (Bresenham line-of-sight), validators, factories. |

**Domain has zero knowledge of files, gRPC, HTTP, or application workflows.**

### Application layer

| Project | Responsibility |
|---|---|
| `ApplicationServices` | All use-case orchestration: commands (`ISimulationCommands`, `ITrainingCommands`, `IAggregationRunCommands`), queries (`ISimulationQueries`, `ITrainingQueries`, etc.), background jobs (`TrainingJobService`, `SimulationJobService`, `AggregationJobService`), executors (`Executor`, `StandardExecutor`, `ExecutorForPresentation`), runners (`SingleRunner`, `MassRunner`, `AggregationRunner`, `TrainingRunner`), playground state persistence mapper. |
| `Ai` | `IAiActions` interface + implementations: `RandomActions` (random baseline), `Sb3Actions` (RL bridge), `ObservationBuilder` (encodes agent state + vision grid into a flat `float[]`). Registered as `IAiActions` in DI. |
| `AiTrainingOrchestrator` | `ITraining` / `PpoTraining` / `A2cTraining` / `DqnTraining` (hyperparameters), `IPolicyTrainerClient` / `PolicyTrainerClient` (gRPC to Python :50051), `EnvironmentSpecBuilder` (env spec negotiation), `BaseTraining` (physical-core scaling). |

### Infrastructure layer

| Project | Responsibility |
|---|---|
| `Infrastructure` | `IFileDataManager<T>` / `FileDataManager<T>` (JSON persistence), `NullFileDataManager<T>` (no-op for training mode), `IMemoryDataManager<T>` / `MemoryDataManager<T>` (in-memory store), JSON converters for domain types. |
| `Statistics` | Result DTOs (`ParticularRun`, `BatchSummary`, `MassRunSummary`, etc.), `IStatisticFileDataManager` / `StatisticFileDataManager` (CSV/JSON aggregation report output), `TableConverter`. |

### Presentation / Host layer

| Project | Responsibility |
|---|---|
| `ConsolePresentation` | `ConsoleRunner` — subscribes to `IMessageBroker` events and renders the grid using Spectre.Console. Optimised for partial re-render via `GetAffectedCells()`. |
| `GrpcHost` | `SimulationService` — Gymnasium gRPC endpoint (`:50062`). Bridges Python `reset`/`step` calls to `IMessageBroker` using `TaskCompletionSource` correlation. Active only during training. |
| `WebApi` | ASP.NET Core controllers (`TrainingController`, `SimulationController`, `AggregationRunController`, `StatisticController`). All routes under `/ai-sandbox/`. Write endpoints return 202 Accepted (fire-and-forget). |
| `Startup` | Composition root: DI wiring, `MenuRunner`, host selection (Kestrel for training, generic host otherwise), `WebApiHost` launch. |
| `Frontend` | Blazor WebAssembly SPA. Feature-based folder layout under `Features/`. Connects to the REST API and to `/hubs/simulation` (SignalR) for live grid visualisation. |

---

## 4. Key Domain Concepts

| Concept | Description |
|---|---|
| `StandardPlayground` | Aggregate root. **All** simulation state mutations go through it. Never manipulate `MapSquareCells` directly from outside. |
| `Cell` | A single grid position. Always occupied — empty positions hold an `EmptyCell` (Null Object pattern). |
| `Coordinates` | `(int X, int Y)`, `(0,0)` = **bottom-left**. Y increases upward in domain; the console renderer flips it. |
| `BorderBlock` | Auto-placed perimeter wall. Never persisted — re-created on every load by `PlaygroundBuilder.SetMap()`. |
| `AgentAction` | `Move` or `Run` (toggle sprint). Validated by `AgentActionAddValidator` before being added to `AvailableActions`. |
| `SandboxStatus` | `InProgress`, `HeroWon`, `HeroLost`, `TurnLimitReached`. Determines the terminal condition. |
| `IMessageBroker` | Singleton pub/sub bus. Decouples executor from all consumers (UI, gRPC, AI). Handlers are invoked **synchronously** on the publisher's thread. |
| `IAiActions` | Single interface for all agent decision logic. `RandomActions` = baseline; `Sb3Actions` = RL bridge. Registered as scoped per execution context. |
| `ObservationBuilder` | Encodes `AgentStateForAIDecision` into a flat `float[]`: 5 scalar features + `gridSize²` vision grid values. **Must stay in sync with `OBS_DIM` / `obs_dim` in the Python service.** |
| `EnvironmentSpec` | Sent from .NET to Python before every training run via `NegotiateEnvironment`. Contains `obs_dim`, `action_dim`, and named feature list. Python echoes it back; any mismatch aborts training. |

### Observation vector format (default `SightRange = 5` → 126 floats)

```
[x, y, is_run, stamina_frac, speed,  grid[0,0], grid[0,1] … grid[10,10]]
  0  1    2         3          4         5 …                       130
```

Vision grid is row-major, top-to-bottom (Y increases downward in the grid encoding).
Cell values: `-1.0` = not visible, `0.0` = empty, `1.0` = hero, `2.0` = enemy, `3.0` = block, `4.0` = exit.

### Win / Loss conditions

| Condition | Event |
|---|---|
| Hero occupies the Exit cell | `HeroWonEvent(WinReason.ReachedExit)` |
| Enemy occupies the Hero's cell | `HeroLostEvent(LostReason.CaughtByEnemy)` |
| `Turn >= MaxTurns` | `HeroLostEvent(LostReason.TurnLimitReached)` |

---

## 5. Feature Boundaries

These boundaries must not be crossed when adding new functionality:

| Rule | Rationale |
|---|---|
| Domain never imports Application, Infrastructure, or Presentation types | Enforces testability and independence of game rules |
| `Ai` project contains **only** decision logic and observation encoding — no game rules | Game rules belong in Domain |
| `ApplicationServices` contains **no game rules** — orchestration only | Game rules belong in Domain |
| `Infrastructure` only implements interfaces defined in Application or Domain | Dependency inversion: swap storage backend without touching AppSvc |
| `GrpcHost` only knows `IMessageBroker` and `SharedBaseTypes` — no direct domain or application imports | Keeps the gRPC surface thin and independently replaceable |
| `Statistics` only depends on `SharedBaseTypes` and `Common` | Result DTOs and CSV output are infrastructure-level concerns |
| `Startup` is the **only** project that wires all DI registrations and knows the full dependency graph | Everything else is wired by its own `XxxServiceCollectionExtensions` and composed here |
| `Frontend` communicates with the backend **only** through the REST API and the `/hubs/simulation` SignalR hub | No direct coupling to backend internals |

---

## 6. Naming Conventions

### C# (.NET)

| Item | Convention | Example |
|---|---|---|
| Projects | `AuxiliumLab.AiSandbox.<Feature>` | `AuxiliumLab.AiSandbox.ApplicationServices` |
| Interfaces | `I` prefix | `IMessageBroker`, `IAiActions`, `ITrainingCommands` |
| Commands (AppSvc DTOs) | `Start…Command`, `Create…Command` | `StartPpoTrainingCommand` |
| Queries (AppSvc DTOs) | `Get…Query` suffix optional, result = `…Dto` | `TrainingJobStatusDto` |
| Events (MessageBroker) | Past tense: `…Event` | `HeroWonEvent`, `TurnExecutedEvent` |
| Commands (MessageBroker) | Imperative: `Request…Command` | `RequestSimulationStepCommand` |
| Responses (MessageBroker) | `…Response` | `SimulationStepResponse` |
| Job services | `…JobService` | `TrainingJobService`, `SimulationJobService` |
| Executors | `…Executor` | `StandardExecutor`, `ExecutorForPresentation` |
| Runners | `…Runner` | `TrainingRunner`, `MassRunner` |
| Factories | `…Factory` | `PlaygroundFactory`, `HeroFactory` |
| Builders | `…Builder` | `PlaygroundBuilder`, `EnvironmentSpecBuilder` |
| Settings | `…Settings` | `StartupSettings`, `TrainingSettings` |
| DI extensions | `…ServiceCollectionExtensions` | `ApplicationServicesCollectionExtensions` |
| Test classes | `<ClassUnderTest>Tests` or `<ClassUnderTest>Test` | `VisibilityServiceBasicTests` |
| Test methods | `MethodName_Scenario_ExpectedOutcome` | `UpdateVisibleCells_BlockBehindWall_NotVisible` |

### Python

| Item | Convention | Example |
|---|---|---|
| Modules / files | `snake_case` | `trainer_servicer.py`, `model_store.py` |
| Classes | `PascalCase` | `TrainingOrchestrator`, `ExternalSimEnv` |
| Functions / methods | `snake_case` | `start_training`, `get_run_status` |
| Constants | `UPPER_SNAKE_CASE` | `DEFAULT_CHECKPOINT_FREQ` |
| Config env vars | `UPPER_SNAKE_CASE` | `GRPC_PORT`, `MODELS_DIR` |

---

## 7. Implementation Rules

### General

- **Do not add game rules outside the `Domain` project.** Any change to movement, vision, win/loss logic, or agent stats belongs in `Domain`.
- **Do not add new command/query entries to `ApplicationServices` without a corresponding `IXxxCommands` / `IXxxQueries` interface extension.** Job services implement both.
- **Do not add direct field mutations to `StandardPlayground` from outside the aggregate.** Always call a method on the aggregate root or use `IPlaygroundCommandsHandleService`.
- **Do not use `IMemoryDataManager<T>` for new domain-independent data** (e.g. API session state). Use a dedicated in-memory store or a command/query service instead.
- **All write endpoints in `WebApi` return 202 Accepted.** Job results are queried via polling the corresponding GET status endpoint.
- **New execution modes must be added to the `ExecutionMode` enum** in `SharedBaseTypes/ValueObjects/StartupSettings/`, the `MenuRunner` switch, and the `Program.cs` dispatch in `Startup`.
- **The `EnvironmentSpec` sent to Python must exactly match what `ObservationBuilder` produces.** When changing `SightRange`, update `EnvironmentSpecBuilder.ScalarFeatureCount` constants or the observation encoding together.
- **Do not modify auto-generated gRPC stubs** (`generated/` in the Python project, protobuf-generated `.cs` files in the C# projects). Regenerate them from `.proto` files.

### Observation / action space changes

When the observation vector length or action count changes:
1. Update `ObservationBuilder` in `AuxiliumLab.AiSandbox.Ai`.
2. Update `EnvironmentSpecBuilder.ScalarFeatureCount` / `ActionDim` / feature names in `AiTrainingOrchestrator`.
3. Update `OBSERVATION_DIM` / `ACTION_DIM` env vars or defaults in the Python service (`infra/config.py`).
4. Update `ExternalSimEnv` `observation_space` / `action_space` in `core/env.py`.
5. Retrain — existing models are incompatible with a changed observation shape.

### Adding a new RL algorithm

1. Add value to `ModelType` enum in `SharedBaseTypes/AiContract/`.
2. Create `XxxTraining : BaseTraining, ITraining` in `AiTrainingOrchestrator`.
3. Add `StartTrainingXxx` RPC to `proto/policy_trainer.proto`, regenerate.
4. Add `StartTrainingXxxAsync` to `IPolicyTrainerClient` + implement in `PolicyTrainerClient`.
5. Add `AlgorithmType.XXX` to `core/dto.py` and a branch in `build_model()` in `core/algorithms.py`.
6. Update `TrainingRunner` switch in `ApplicationServices`.
7. Add an algorithm entry to `Startup/training-settings.json`.

### Adding a new map object type

1. Add value to `ObjectType` enum in `SharedBaseTypes`.
2. Create a class inheriting `SandboxMapBaseObject`; set `Transparent` appropriately.
3. For agent types: inherit `Agent`, implement `Clone()`, add a factory in `Agents/Factories/`.
4. Update `PlaygroundBuilder` / `PlaygroundFactory` to place the object.
5. Update `ConsoleRunner` / `_cellData` dictionary in `ConsolePresentation` (colour + character).
6. Update vision-grid cell encoding in `ObservationBuilder` (new numeric value).
7. Update the Python cell-value comment table in the relevant README / docstring.

### Adding a new statistic column

1. Add the property to the relevant result DTO (`ParticularRun`, `BatchSummary`, etc.) in `Statistics/Result/`.
2. Update `TableConverter` to include the new value in its row array.
3. Update `MassRunner` to populate the property when constructing the DTO.

---

## 8. Dependency Rules (enforcement checklist)

Before opening a pull request, verify:

| Check | Pass condition |
|---|---|
| `Domain` project has no reference to `ApplicationServices`, `Infrastructure`, or any Presentation project | `dotnet list reference` is clean |
| `Common` project has no reference to `ApplicationServices`, `Infrastructure`, or Domain projects (only `SharedBaseTypes`) | true |
| `Ai` project references only `Common`, `Infrastructure`, `SharedBaseTypes` | true |
| `Statistics` project references only `SharedBaseTypes`, `Common` | true |
| `GrpcHost` project references only `Common`, `SharedBaseTypes` | true |
| `ApplicationServices` does not reference `ConsolePresentation`, `WebApi`, `GrpcHost`, or `Startup` | true |
| `WebApi` does not reference `ConsolePresentation`, `GrpcHost`, or `Startup` | true |
| No project except `Startup` references `Startup` | true |

---

## 9. Testing Expectations

### Unit tests (`AuxiliumLab.AiSandbox.UnitTests`)

- **Framework:** MSTest (`[TestClass]`, `[TestMethod]`)
- **Pattern:** Construct real domain objects directly — no DI container, no mocks library.
- **Repository alternatives:** Use `MemoryDataManager<T>` instead of `FileDataManager<T>` to avoid file I/O.
- **Broker:** Instantiate a real `MessageBroker` — it is cheap to construct.
- **Naming:** `MethodName_Scenario_ExpectedOutcome`.
- **Folder structure:** Mirror the production project folder structure inside the test project.
- New tests for Domain should go in `AuxiliumLab.AiSandbox.Domain/` subfolder; for AppSvc in `AuxiliumLab.AiSandbox.ApplicationServices/` subfolder.

### Integration tests (`AuxiliumLab.AiSandbox.IntegrationTests`)

- **Framework:** MSTest + `WebApplicationFactory<Program>` (`AiSandboxWebApplicationFactory`).
- **Always redirect file storage** to a temp folder via the factory — never write to production paths.
- **Always call `CleanArtifacts()` in `[TestCleanup]`** to prevent cross-test pollution.
- **Polling pattern for async jobs:** `GET .../status` with deadline (≤ 5 minutes) and 500 ms interval.
- **Training tests require the Python RL service** running on `localhost:50051`. Other tests do not.
- **Test ordering:** Classes run alphabetically; within a class tests run sequentially.

### Python tests (`auxiliumlab-rl-service-baselines3/tests/`)

- **Framework:** pytest
- **Fake adapter:** Use `FakeExternalEnvAdapter` for any test that does not need a live .NET process.
- **Test files:** `test_algorithms.py`, `test_env_wrapper.py`, `test_grpc_training_smoke.py`, `test_health_check.py`.
- Run all: `pytest` from the project root with the venv activated.

### Frontend tests (`AuxiliumLab.AiSandbox.Frontend.UnitTests`)

- **Framework:** MSTest + bUnit + FluentAssertions + Moq.
- Register `MudBlazor` services (`ctx.Services.AddMudServices()`) before rendering.
- Mock all injected interfaces via Moq; assert on markup or mock invocations.

---

## 10. Configuration Reference

| File | Location | Contents |
|---|---|---|
| `appsettings.json` | `Startup/` | `StartupSettings`, `SandBox`, `PolicyTrainerClient`, `ConsoleSettings`, Kestrel |
| `training-settings.json` | `Startup/` | Algorithm hyperparameter defaults for PPO, A2C, DQN |
| `aggregation-settings.json` | `Startup/` | Default ordered step list for `AggregationRun` |
| `Directory.Build.props` | Solution root | Solution-wide MSBuild properties, optional diagnostic `DefineConstants` |
| `wwwroot/appsettings.json` | `Frontend/AuxiliumLab.Frontend/` | `ApiSettings.AiSandboxBaseUrl` pointing at the .NET REST API (overridden at runtime by `entrypoint.sh` in Docker) |
| `docker-compose.yml` | Workspace root | Orchestrates all three services; defines ports, volumes, health checks, and env vars |
| `.env` / environment vars | Python service | `GRPC_PORT`, `MODELS_DIR`, `CHECKPOINT_DIR`, `OBSERVATION_DIM`, `ACTION_DIM`, `MAX_STEPS` |
| `AISANDBOX_BASE_URL` | Frontend container env var | Injected by `entrypoint.sh` into `wwwroot/appsettings.json` at container start; defaults to `http://localhost:5000` |

### Key `appsettings.json` keys

| Key | Default | Effect |
|---|---|---|
| `StartupSettings.IsPreconditionStart` | `true` | Skip interactive menu, use file settings |
| `StartupSettings.IsWebApiEnabled` | — | Launch `WebApiHost` on `:5000` |
| `SandBox.MaxTurns.Current` | `10` | Turn limit per episode |
| `SandBox.MapSettings.Size.Width/Height.Current` | `20` | Map dimensions |
| `SandBox.Hero.SightRange.Current` | `5` | Vision radius; **drives `obs_dim` calculation** |
| `PolicyTrainerClient.ServerAddress` | `http://localhost:50051` | Python gRPC service address |
| `TrainingSettings.Rewards.StepPenalty` | `-0.1` | Reward per step (encourages speed) |
| `TrainingSettings.Rewards.WinReward` | `+10.0` | Reward on exit reached |
| `TrainingSettings.Rewards.LossReward` | `-10.0` | Reward on loss or timeout |

---

## 11. gRPC Port Map

| Port | Direction | Protocol | Purpose |
|---|---|---|---|
| `:50051` | .NET → Python | HTTP/2 gRPC | `PolicyTrainerService`: StartTraining, GetStatus, Act |
| `:50062` | Python → .NET | HTTP/2 gRPC | `SimulationService`: gym Reset / Step / Close |
| `:5000` | Any → .NET | HTTP/1.1 REST | WebApi: training, simulation, aggregation, statistics |
| `:7001` / `:5001` | Browser → Blazor | HTTPS / HTTP | Blazor WebAssembly frontend dev server |
| `:8080` | Browser → Docker | HTTP | Frontend container (`StaticHost`) mapped from container-internal `:80` |

---

## 12. Documentation Index

All Markdown files in this repository, grouped by area.

### Root

| File | Contents |
|---|---|
| [`AI_GUIDELINES.md`](AI_GUIDELINES.md) | Rules for AI-assisted development (this file) |
| [`DEVELOPER_SETUP.md`](DEVELOPER_SETUP.md) | End-to-end setup guide: Python service, .NET build, port reference, test commands |

### Python RL Service

| File | Contents |
|---|---|
| [`auxiliumlab-rl-service-baselines3/README.md`](auxiliumlab-rl-service-baselines3/README.md) | Service architecture, key components, algorithms, API usage, default hyperparameters |
| [`auxiliumlab-rl-service-baselines3/QUICKSTART.md`](auxiliumlab-rl-service-baselines3/QUICKSTART.md) | 5-minute setup, PowerShell scripts, health check, troubleshooting |

### .NET Solution — Overview

| File | Contents |
|---|---|
| [`AuxiliumLab.AiSandbox/README.md`](AuxiliumLab.AiSandbox/README.md) | Solution structure, build/run, execution modes, REST API endpoint table, debug constants |
| [`AuxiliumLab.AiSandbox/docs/ARCHITECTURE.md`](AuxiliumLab.AiSandbox/docs/ARCHITECTURE.md) | Onion architecture diagram, full project dependency graph, key design patterns, data-flow diagrams, thread safety |

### .NET Solution — Domain Layer

| File | Contents |
|---|---|
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.SharedBaseTypes/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.SharedBaseTypes/README.md) | Value objects, enums, message-type base classes |
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.Common/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.Common/README.md) | `IMessageBroker`, `IBrokerRpcClient`, all message contracts, helper utilities |
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.Domain/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.Domain/README.md) | Aggregate root, map grid, agents, vision service, validators, factories, extension guides |

### .NET Solution — Application Layer

| File | Contents |
|---|---|
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.Ai/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.Ai/README.md) | `IAiActions`, `RandomActions`, `Sb3Actions` lifecycle, observation encoding, reward scheme, action space |
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.ApplicationServices/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.ApplicationServices/README.md) | Commands, queries, job services, executors, runners, persistence mapper, execution flow |
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.AiTrainingOrchestrator/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.AiTrainingOrchestrator/README.md) | `ITraining` implementations, `PolicyTrainerClient`, `EnvironmentSpecBuilder`, training workflow |

### .NET Solution — Infrastructure Layer

| File | Contents |
|---|---|
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.Infrastructure/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.Infrastructure/README.md) | `FileDataManager`, `MemoryDataManager`, JSON converters, configuration keys |
| [`AuxiliumLab.AiSandbox/AuxiliumLab.Statistics/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.Statistics/README.md) | Result DTOs, `StatisticFileDataManager`, CSV report format, incremental sweep preconditions |

### .NET Solution — Presentation / Host Layer

| File | Contents |
|---|---|
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.ConsolePresentation/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.ConsolePresentation/README.md) | `ConsoleRunner`, event subscriptions, partial render optimisation, coordinate flip, cell rendering |
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.GrpcHost/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.GrpcHost/README.md) | `SimulationService`, gym reset/step bridging via `IMessageBroker`, correlation, configuration |
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.WebApi/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.WebApi/README.md) | All REST endpoints, request/response DTOs, configuration, adding new endpoints |
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.Startup/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.Startup/README.md) | Startup sequence, execution modes, aggregation run config, `RegisterCoreServices`, DI lifetimes, `appsettings.json` keys |

### Frontend

| File | Contents |
|---|---|
| [`AuxiliumLab.AiSandbox/Frontend/README.md`](AuxiliumLab.AiSandbox/Frontend/README.md) | Folder guide: project list with links |
| [`AuxiliumLab.AiSandbox/Frontend/AuxiliumLab.Frontend/README.md`](AuxiliumLab.AiSandbox/Frontend/AuxiliumLab.Frontend/README.md) | Stack, feature pages, SignalR events, notifications, architecture (feature folder tree), configuration |

### Tests

| File | Contents |
|---|---|
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.UnitTests/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.UnitTests/README.md) | Test coverage table, structure, conventions, known gaps |
| [`AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.IntegrationTests/README.md`](AuxiliumLab.AiSandbox/AuxiliumLab.AiSandbox.IntegrationTests/README.md) | `AiSandboxWebApplicationFactory`, test coverage per controller, conventions, adding new tests |
| [`AuxiliumLab.AiSandbox/Frontend/AuxiliumLab.AiSandbox.Frontend.UnitTests/README.md`](AuxiliumLab.AiSandbox/Frontend/AuxiliumLab.AiSandbox.Frontend.UnitTests/README.md) | bUnit + MSTest stack, test coverage per feature, test patterns |

---

## 13. Quick Checklist for New Features

1. Identify which layer the change belongs to (see §3 and §5).
2. Check dependency rules (§8) before adding a `ProjectReference`.
3. Follow naming conventions (§6) for new types, interfaces, and files.
4. If changing the observation/action space, follow all steps in §7.
5. Add or update unit tests for Domain/AppSvc changes; add integration tests for new API endpoints.
6. Do not hard-code file system paths — use `appsettings.json` keys resolved through `IOptions<T>`.
7. Do not modify auto-generated protobuf files — regenerate from `.proto` sources.
8. Keep `EnvironmentSpec` in sync between `EnvironmentSpecBuilder` (.NET) and `ExternalSimEnv` (Python).
