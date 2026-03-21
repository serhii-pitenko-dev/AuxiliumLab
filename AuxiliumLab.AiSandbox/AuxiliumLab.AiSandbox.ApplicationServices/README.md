# AuxiliumLab.AiSandbox.ApplicationServices

**Onion layer: Application**  
Orchestrates use-cases by coordinating Domain, Infrastructure, and external services.  
Contains **no game rules** — those live exclusively in `Domain`.

## Purpose
- Execute simulation runs (single, batch, training, aggregation).
- Expose command and query interfaces consumed by Presentation layers (REST API, Console).
- Manage background jobs (training, simulation, aggregation) with status tracking.
- Map between domain objects and persistence/presentation models.
- Integrate AI decision-making into the simulation loop.

## Folder Structure
```
ApplicationServices/
├── Commands/
│   ├── Simulation/                 ISimulationCommands, SimulationCommandService
│   │   ├── Dto/                    StartSingleSimulationCommand, StartMassSimulationCommand, SimulationJobStartedDto
│   │   └── Playground/             IPlaygroundCommandsHandleService, PlaygroundCommandsHandleService
│   │       └── CreatePlayground/   CreatePlaygroundCommand + handler
│   ├── Training/                   ITrainingCommands, TrainingCommandService
│   │   └── Dto/                    StartPpoTrainingCommand, PpoHyperparametersDto, TrainingJobStartedDto …
│   └── AggregationRun/             IAggregationRunCommands, AggregationRunCommandService
│       └── Dto/                    StartAggregationCommand, AggregationJobStartedDto …
├── Queries/
│   ├── Simulation/                 ISimulationQueries, SimulationQueryService
│   │   ├── Dto/                    SimulationJobStatusDto
│   │   └── Map/                    IMapQueriesHandleService, GetMapLayout, GetAffectedCells
│   ├── Training/                   ITrainingQueries, TrainingQueryService
│   │   └── Dto/                    TrainingJobStatusDto, TrainedModelInfoDto, TrainingPreconditionsDto
│   ├── AggregationRun/             IAggregationRunQueries, AggregationRunQueryService
│   │   └── Dto/                    AggregationJobStatusDto
│   └── Statistic/                  IStatisticQueries, StatisticQueryService
│       └── Dto/                    CompletedSimulationRunDto, CompletedAggregationRunDto, AggregationStepResultDto
├── Executors/                      Core simulation loop implementations
├── Runner/
│   ├── SingleRunner/               One-shot run helpers
│   ├── MassRunner/                 Parallel batch + incremental sweep runner
│   ├── AggregationRunner/          AggregationRunner — ordered multi-step pipeline
│   ├── TestPreconditionSet/        Seeded precondition run support
│   └── LogsDto/                    Raw data log and performance DTOs
├── Saver/
│   └── Persistence/Sandbox/        State serialization + mapper
├── Trainer/                        TrainingRunner — orchestrates RL training
├── Converters/                     Domain ↔ DTO converters
└── Configuration/                  ApplicationServicesCollectionExtensions
```

## Core Components

### Commands

#### `ISimulationCommands` / `SimulationCommandService`
- `StartSingleSimulationAsync(StartSingleSimulationCommand, ct)` — fires a single simulation in the background; returns `SimulationJobStartedDto`.
- `StartMassSimulationAsync(StartMassSimulationCommand, ct)` — fires a mass (batch) simulation; returns `SimulationJobStartedDto`.

`StartSingleSimulationCommand.Kind`:
- `RandomAI` — random actions.
- `TrainedAI` — loads the latest trained model for the specified `Algorithm`.

#### `ITrainingCommands` / `TrainingCommandService`
- `StartPpoTrainingAsync(StartPpoTrainingCommand, ct)` — launches a PPO training run; returns `TrainingJobStartedDto`.
- All hyperparameter and sandbox fields are **optional** — missing values fall back to `appsettings.json → TrainingSettings` defaults.

#### `IAggregationRunCommands` / `AggregationRunCommandService`
- `StartAggregationAsync(StartAggregationCommand, ct)` — starts a multi-step aggregation pipeline; returns `AggregationJobStartedDto`.
- `Steps` may be empty → falls back to `appsettings.json → AggregationSettings`.
- `TrainingOverrides` optional — overrides PPO hyperparameters for the embedded Training step.

### Queries

#### `ISimulationQueries` / `SimulationQueryService`
- `GetSimulationStatusesAsync(ct)` → `IReadOnlyList<SimulationJobStatusDto>` — running and recently completed jobs.
- `GetSandboxDefaultsAsync(ct)` → `SandboxDefaultsDto` — reads defaults from configuration.

#### `ITrainingQueries` / `TrainingQueryService`
- `GetTrainingStatusesAsync(ct)` → `IReadOnlyList<TrainingJobStatusDto>`
- `GetTrainedModelsAsync(ct)` → `IReadOnlyList<TrainedModelInfoDto>` — scans the trained-models directory.

#### `IAggregationRunQueries` / `AggregationRunQueryService`
- `GetAggregationStatusesAsync(ct)` → `IReadOnlyList<AggregationJobStatusDto>` — with per-step progress.

#### `IStatisticQueries` / `StatisticQueryService`
- `GetCompletedSimulationRunsAsync(ct)` → `IReadOnlyList<CompletedSimulationRunDto>`
- `GetCompletedAggregationRunsAsync(ct)` → `IReadOnlyList<CompletedAggregationRunDto>`

### Playground Commands — `IPlaygroundCommandsHandleService`
Exposes every state-mutating operation on the `StandardPlayground` as a method (light command pattern).  
All commands go through the aggregate root — external code never manipulates the map directly.

Key operations:
- Create a new playground from configuration.
- Move an agent.
- Toggle an agent action (Run, etc.).
- Place objects on the map.

### Playground Queries — `IMapQueriesHandleService`
Read-only access to map state, designed for presentation consumers.

| Query | Returns | Description |
|---|---|---|
| `GetMapLayout` | `MapLayoutResponse` | Full 2D grid of `MapCell` DTOs |
| `GetAffectedCells` | `AffectedCellsResponse` | Only the cells that changed since last render |

### Executor Pattern

All simulation execution flows through an `Executor`.

```
IExecutor (base interface)
├── IExecutorForPresentation   — runs with event notifications (ConsolePresentation subscribes)
├── IStandardExecutor          — silent run, captures ParticularRun result
└── (Training executor)        — created inline by TrainingRunner per environment
```

**`Executor` base class** (`Executors/Executor.cs`):
- Holds repository and service references.
- `RunAsync(config)` — the main simulation loop:
  1. Creates playground via command handler.
  2. Sends `GameStartedEvent`.
  3. Loops per turn: prepares agents → AI decides → executes action → updates vision → checks win/loss.
  4. Saves state to file at configurable intervals.
  5. Sends `HeroWonEvent` or `HeroLostEvent` on termination.

**Win / Loss conditions:**

| Condition | Event published |
|---|---|
| Hero occupies the Exit cell | `HeroWonEvent(WinReason.ReachedExit)` |
| An Enemy occupies the Hero's cell | `HeroLostEvent(LostReason.CaughtByEnemy)` |
| `Turn >= MaxTurns` | `HeroLostEvent(LostReason.TurnLimitReached)` |

**`StandardExecutor`**: inherits `Executor`, suppresses agent notification events, captures result as `ParticularRun`.

**`ExecutorForPresentation`**: inherits `Executor`, publishes `OnBaseAgentActionEvent` and `TurnExecutedEvent` so the console renderer can animate changes.

### Runners

#### `SingleRunner`
Wraps a single executor call. Three modes:
```csharp
RunSingleAsync(IExecutorForPresentation)            // console visualization
RunSingleTrainedAsync(IStandardExecutor)            // trained-model run
RunTestPreconditionsAsync(IExecutorForPresentation) // seeded precondition run
```

#### `MassRunner`
Parallel batch execution using `Parallel.ForEachAsync`.

**Phases:**
1. **Standard phase** — runs `count` simulations in parallel, collects `ParticularRun` results.
2. **Incremental sweep phase** — for each configured property (e.g. `MaxTurns`, `SightRange`) sweeps its range in configured steps, one batch per step value.
3. **Area sweep phase** — optional, sweeps map area independently.

**Output:** CSV files with per-batch summaries, written via `IStatisticFileDataManager`.

#### `AggregationRunner`
Executes an ordered sequence of steps and produces a combined CSV report.

Supported step modes:
- **`Training`** — delegates to `TrainingRunner.RunTrainingAsync(algorithmType, overrides?)`, captures `TrainingRunInfo`.
- **`MassRandomAISimulation`** — delegates to `MassRunner.RunManyAsync` (random AI).
- **`MassTrainedAISimulation`** — delegates to `MassRunner.RunManyAsync` using `InferenceActions` (loads the latest `.zip` model found recursively under the algorithm folder).

Model discovery uses `Directory.GetFiles(algorithmFolder, "*", SearchOption.AllDirectories)` to find `.zip` files in subdirectories (e.g. `trained/PPO/{experimentId}/model.zip`).

#### `TrainingRunner`
Coordinates the full RL training loop:
1. Selects the correct `ITraining` implementation (PPO / A2C / DQN) from `AiTrainingOrchestrator`.
2. Resolves scoped executor pairs (one per physical CPU core).
3. Starts all executor tasks — each loops an `Sb3Actions`-driven simulation episode.
4. Calls `IPolicyTrainerClient.StartTrainingXxx()` to kick off the Python SB3 training.
5. Accepts an optional `StartPpoTrainingCommand? overrides` to apply per-request hyperparameter overrides on top of the `appsettings.json → TrainingSettings` defaults.

### Command / Query Services
Command services implement both the command and query interfaces for a feature.  
All command services run via `Task.Run` fire-and-forget and maintain an in-memory job registry (`ConcurrentDictionary`).

| Service | Implements | Function |
|---|---|---|
| `TrainingCommandService` | `ITrainingCommands`, `ITrainingQueries` | PPO training jobs |
| `SimulationCommandService` | `ISimulationCommands`, `ISimulationQueries` | Single and mass simulation jobs |
| `AggregationRunCommandService` | `IAggregationRunCommands`, `IAggregationRunQueries` | Multi-step aggregation jobs |
| `StatisticQueryService` | `IStatisticQueries` | Reads completed-run data from the in-memory job stores |

### Saver — Persistence
`Saver/Persistence/Sandbox/`:
- `StandardPlaygroundState` — serializable snapshot of a full playground.
- `IStandardPlaygroundMapper` — maps between `StandardPlayground` (domain) and `StandardPlaygroundState` (persistence DTO).
- Snapshots are saved to disk via `IFileDataManager<StandardPlaygroundState>` at a frequency controlled by `SandBox.SaveToFileRegularity`.

### `TestPreconditionData`
Wraps a pre-saved `StandardPlaygroundState`. When `IsPreconditionStart = true` in settings, the executor loads this state instead of generating a random map — useful for reproducible debugging and benchmarking.

## Execution Flow Summary

```
REST API / Startup
   └─ Command Service (e.g. TrainingCommandService)
         └─ Runner (TrainingRunner / MassRunner / AggregationRunner)
               └─ Executor.RunAsync()
                     ├─ PlaygroundCommandsHandleService.CreatePlayground()
                     │       └─ Domain: PlaygroundFactory builds StandardPlayground
                     ├─ Loop per turn
                     │     ├─ IAiActions.GetAction(AgentStateForAIDecision)
                     │     ├─ PlaygroundCommandsHandleService.MoveAgent() / ToggleAction()
                     │     └─ IMessageBroker.Publish(TurnExecutedEvent)
                     └─ Captures result → MassRunner aggregates → CSV output
```

## How to Add a New Use-Case
1. **Command:** Add a method to the relevant `IXxxCommands` interface + implement in `Jobs/Xxx/XxxJobService`.
2. **Query:** Add a method to the relevant `IXxxQueries` interface + implement in the same job service.
3. **New runner mode:** Implement in `Runner/`, inject whatever executors or services are needed. Register in `Configuration/ApplicationServicesCollectionExtensions`.
