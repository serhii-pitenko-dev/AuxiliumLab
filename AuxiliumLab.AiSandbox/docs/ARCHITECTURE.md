# Architecture — AuxiliumLab.AiSandbox

## Onion Architecture

The solution strictly follows **Onion Architecture** (Clean / Hexagonal).  
The central rule: **inner rings cannot reference outer rings**.

```
                  ┌───────────────────────────────────────────────────┐
                  │            Composition Root / Hosts               │
                  │  Startup · ConsolePresentation · GrpcHost · WebApi│
                  │                                                   │
                  │   ┌───────────────────────────────────────────┐   │
                  │   │          Application Services             │   │
                  │   │  ApplicationServices · AiTrainingOrch.    │   │
                  │   │                                           │   │
                  │   │   ┌───────────────────────────────────┐   │   │
                  │   │   │         Infrastructure            │   │   │
                  │   │   │  Infrastructure · Statistics       │   │   │
                  │   │   │                                   │   │   │
                  │   │   │  ┌─────────────────────────────┐  │   │   │
                  │   │   │  │          Domain             │  │   │   │
                  │   │   │  │  Domain · SharedBaseTypes   │  │   │   │
                  │   │   │  │  Common (MessageBroker)     │  │   │   │
                  │   │   │  └─────────────────────────────┘  │   │   │
                  │   │   └───────────────────────────────────┘   │   │
                  │   └───────────────────────────────────────────┘   │
                  └───────────────────────────────────────────────────┘
```

## Project Dependency Graph

```
Startup
├── Ai
│   ├── Common
│   ├── Infrastructure
│   └── SharedBaseTypes
├── AiTrainingOrchestrator
│   ├── SharedBaseTypes
│   └── Common
├── ApplicationServices
│   ├── Domain
│   ├── Common
│   ├── SharedBaseTypes
│   ├── Infrastructure
│   └── AiTrainingOrchestrator
├── ConsolePresentation
│   ├── ApplicationServices
│   ├── Common
│   └── SharedBaseTypes
├── GrpcHost
│   ├── Common
│   └── SharedBaseTypes
├── Infrastructure
│   ├── Domain
│   ├── SharedBaseTypes
│   └── Statistics
└── WebApi
    ├── ApplicationServices
    └── SharedBaseTypes

Domain
└── SharedBaseTypes

Common
└── SharedBaseTypes

Statistics
├── SharedBaseTypes
└── Common

IntegrationTests (test-only references)
└── WebApi (via WebApplicationFactory<Program>)
```

## Key Design Patterns

### Aggregate Root — `StandardPlayground`
`StandardPlayground` is the aggregate root of the simulation.  
All interactions with map objects (agents, blocks, exits) go through `StandardPlayground`.  
Direct manipulation of `MapSquareCells` from outside the aggregate is not allowed.

### In-Process Message Broker (Pub/Sub)
`IMessageBroker` (in `Common`) provides a thread-safe publish/subscribe bus.  
It decouples the simulation engine (publisher) from all presentation and AI layers (subscribers).

```
Executor (Application) ──publish──► IMessageBroker
                                         │
                       ┌─────────────────┼──────────────────┐
                       ▼                 ▼                  ▼
              ConsoleRunner        SimulationService   AI observation
              (renders map)        (gRPC responses)   (training loop)
```

### Command / Query segregation (light CQRS)
`ApplicationServices` splits operations into two tiers:
- **Commands** — DTOs, interfaces, and command service implementations that mutate state (start a training run, start a simulation, etc.)
- **Queries** — DTOs, interfaces, and query service implementations that read state without mutation (get job status, list trained models, list completed runs)

Command services implement the command interfaces and maintain in-memory job registries.
Dedicated query services implement the query interfaces and read state from command services or configuration:
  - `TrainingCommandService` (`ITrainingCommands`) → `TrainingQueryService` (`ITrainingQueries`)
  - `SimulationCommandService` (`ISimulationCommands`) → `SimulationQueryService` (`ISimulationQueries`)
  - `AggregationRunCommandService` (`IAggregationRunCommands`) → `AggregationRunQueryService` (`IAggregationRunQueries`)
  - `StatisticQueryService` (`IStatisticQueries`)

Playground-level state-mutation is encapsulated in `IPlaygroundCommandsHandleService` and map-reading in `IMapQueriesHandleService`.

### Executor pattern
Each execution mode uses a different `IExecutor` implementation:

| Interface | Implementation | Mode |
|---|---|---|
| `IExecutorForPresentation` | `ExecutorForPresentation` | Console / interactive runs |
| `IStandardExecutor` | `StandardExecutor` | Batch simulations, inference |
| (base) `Executor` | `TrainingExecutor` (created by `TrainingRunner`) | RL training |

### Repository pattern (ports & adapters)
- `IFileDataManager<T>` — file-system persistence (Infrastructure implements, Domain/AppSvc use).
- `IMemoryDataManager<T>` — in-memory storage, ConcurrentDictionary-backed.

## Data Flow — REST API

```
  HTTP client
       │
       ▼
  Controller (WebApi/Features/*)
       │   e.g. POST /ai-sandbox/training/ppo
       ▼
  ITrainingCommands.StartPpoTrainingAsync()
       │   implemented by TrainingCommandService
       ▼
  Task.Run { TrainingRunner.RunTrainingAsync() }   ← fire-and-forget
       │
       ├─ GrpcTrainingHost (Kestrel :50062)
       ├─ NEnvs TrainingExecutors
       └─ IPolicyTrainerClient.StartTrainingPPOAsync() → Python SB3

  GET /ai-sandbox/training/status
       └─ ITrainingQueries.GetTrainingStatusesAsync()
              └─ in-memory job registry (ConcurrentDictionary)
```

## Data Flow — Single Simulation Turn

```
  ┌─────────────────────────┐
  │  Executor.RunAsync()     │
  └────────────┬────────────┘
               │
               ▼
  ┌─────────────────────────┐   IMessageBroker
  │  Create playground      │──► GameStartedEvent ──► ConsoleRunner renders map
  └────────────┬────────────┘
               │ loop per turn
               ▼
  ┌─────────────────────────┐
  │  PrepareAgentForTurn    │   (reset stamina, recalc available actions)
  └────────────┬────────────┘
               │
               ▼
  ┌─────────────────────────┐
  │  AI decides action      │   IAiActions.GetAction(AgentStateForAIDecision)
  └────────────┬────────────┘     │ SB3: calls Python via Sb3Contract messages
               │                  │ Random: picks random valid action
               ▼
  ┌─────────────────────────┐
  │  Execute action         │   PlaygroundCommandsHandleService
  │  (Move / Run / …)       │
  └────────────┬────────────┘
               │
               ▼
  ┌─────────────────────────┐   IMessageBroker
  │  TurnExecutedEvent      │──► ConsoleRunner re-renders affected cells
  └────────────┬────────────┘
               │
               ▼
  ┌─────────────────────────┐
  │  Check win/loss         │   HeroWonEvent / HeroLostEvent / TurnLimitReached
  └─────────────────────────┘
```

## Data Flow — Training Mode

```
  TrainingRunner (AppSvc)
       │
       ├─ Starts GrpcHost (Kestrel :50062) ──► accepts Python gym calls
       │
       ├─ Creates N TrainingExecutors (one per physical core)
       │    Each executor runs env episodes and responds to gym messages
       │
       └─ Sends StartTraining gRPC call → Python PolicyTrainerService (:50051)
            │
            Python SB3 service creates ExternalSimEnv
            ExternalSimEnv.reset() / step()
               └─ GrpcExternalEnvAdapter sends gRPC to C# GrpcHost :50062
                    └─ SimulationService publishes RequestSimulationResetCommand
                         └─ TrainingExecutor handles, runs one episode step
                              └─ Returns observation/reward/done via response message
```

## Configuration

All configuration lives in `Startup/appsettings.json`.  
Key sections:

| Section | Type | Purpose |
|---|---|---|
| `StartupSettings` | `StartupSettings` | Execution mode, presentation mode, precondition start |
| `SandBox` | `SandBoxConfiguration` | Map size, turn limits, element percentages |
| `PolicyTrainerClient` | `PolicyTrainerClientSettings` | Python service gRPC address |
| `Training` | `TrainingSettings` | Algorithm types, hyperparameters |
| `ConsoleSettings` | `ConsoleSettings` | Console size, color scheme, action timeout |

## Thread Safety

- `MessageBroker` — `ConcurrentDictionary` + lock per handler list.
- `MemoryDataManager<T>` — `ConcurrentDictionary`.
- `MassRunner` — `Parallel.ForEachAsync` with per-executor scoped DI containers.
- `TrainingRunner` — one scoped executor per physical CPU core, all sharing a single `IMessageBroker` singleton.
