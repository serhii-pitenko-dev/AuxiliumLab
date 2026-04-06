# AuxiliumLab.AiSandbox.UnitTests

**Onion layer: Tests**  
MSTest unit test suite covering Domain, ApplicationServices, and Common.  
Depends on: `Domain`, `ApplicationServices`, `Common`.

## Running Tests

```powershell
cd AuxiliumLab.AiSandbox
dotnet test AuxiliumLab.AiSandbox.sln
```

Or run from VS Code via the Test Explorer panel.

## Test Coverage Overview

| Area | File(s) | What is tested |
|---|---|---|
| **Vision — basic** | `VisibilityServiceBasicTests` | Agent can see cells within radius; cells outside radius are not visible |
| **Vision — blocking** | `VisibilityServiceBlockingTests` | Blocks occlude cells behind them; transparent objects (agents, exit) do not block |
| **Vision — complex** | `VisibilityServiceComplexTests` | Diagonal vision, partial occlusion, multi-block walls |
| **Vision — edge cases** | `VisibilityServiceEdgeCaseTests` | Agent at map edge, single-cell map, zero-radius sight |
| **Agent — run ability** | `AgentTestRunAbility` | Stamina depletion on run, stamina does not go below zero, run toggle |
| **Playground** | `StandardPlaygroundTest` | Place agents and objects, move objects, turn counter, vision update |
| **Mapper** | `StandardPlaygroundMapperTest` | Round-trip serialisation: domain → state → domain preserves all properties |
| **MessageBroker** | `BrokerRpcClientTest` | Request/response correlation, timeout, multiple concurrent requests |
| **AI subscription** | `AiActionsSubscriptionCleanupTests` | InferenceActions / RandomActions unsubscribe on dispose; no handler leaks |
| **Executor isolation** | `ExecutorFactoryBrokerIsolationTests` | Parallel executors use isolated brokers; presentation uses the shared broker |
| **Executor presentation** | `ExecutorForPresentationBrokerDispatchTests` | Presentation executor dispatches events correctly through the shared broker |
| **Executor save state** | `ExecutorSavePlaygroundStateTests` | Executor saves playground state to the file system |
| **Inference — algorithm type** | `InferenceActionsAlgorithmTypeTests` | `ActRequest.AlgorithmType` is set from `ModelType`; failure logging fires once per episode |
| **Environment spec** | `EnvironmentSpecBuilderTests` | Observation dim calculation, feature naming, echo mismatch detection |
| **Sandbox overrides** | `ApplySandboxOverridesTests` | API command sandbox overrides are correctly applied to configuration |
| **Single runner** | `SingleRunnerTests` | Single simulation runner lifecycle |
| **Visualization bridge** | `SimulationVisualizationBridgeTests` | SignalR simulation visualization bridge attach/detach |

## Test Project Structure

```
AuxiliumLab.AiSandbox.UnitTests/
├── AuxiliumLab.AiSandbox.Ai/
│   ├── EnvironmentSpecBuilderTests.cs
│   └── InferenceActionsAlgorithmTypeTests.cs
├── AuxiliumLab.AiSandbox.ApplicationServices/
│   ├── Commands/Simulation/
│   │   └── ApplySandboxOverridesTests.cs
│   ├── Executors/
│   │   ├── AiActionsSubscriptionCleanupTests.cs
│   │   ├── ExecutorFactoryBrokerIsolationTests.cs
│   │   ├── ExecutorForPresentationBrokerDispatchTests.cs
│   │   └── ExecutorSavePlaygroundStateTests.cs
│   ├── Runner/
│   │   └── SingleRunnerTests.cs
│   └── Saver/Persistence/Sandbox/Mappers/
│       └── StandardPlaygroundMapperTest.cs
├── AuxiliumLab.AiSandbox.Common/
│   └── MessageBrokers/
│       └── BrokerRpcClientTest.cs
├── AuxiliumLab.AiSandbox.Domain/
│   ├── Agents/
│   │   ├── Entities/AgentTest/
│   │   │   └── AgentTestRunAbility.cs
│   │   └── Services/Vision/
│   │       ├── VisibilityServiceTestBase.cs    ← shared map builders
│   │       ├── VisibilityServiceBasicTests.cs
│   │       ├── VisibilityServiceBlockingTests.cs
│   │       ├── VisibilityServiceComplexTests.cs
│   │       └── VisibilityServiceEdgeCaseTests.cs
│   └── Playgrounds/
│       └── StandardPlaygroundTest.cs
└── AuxiliumLab.AiSandbox.WebApi/
    └── Features/Simulation/
        └── SimulationVisualizationBridgeTests.cs
```

## Conventions

- **Framework:** MSTest (`[TestClass]`, `[TestMethod]`)
- **Naming:** `MethodName_Scenario_ExpectedOutcome` where applicable.
- **Test base classes:** `VisibilityServiceTestBase` provides shared playground/map builders shared across the four vision test files.
- **No mocking framework** is currently used — tests construct real domain objects directly, keeping unit tests fast and dependency-free.

## Adding New Tests

1. Mirror the production project folder structure inside `AuxiliumLab.AiSandbox.UnitTests/`.
2. Add `[TestClass]` + `[TestMethod]` attributes.
3. For Domain tests: construct objects directly (no DI needed).
4. For ApplicationServices tests that need repositories: use `MemoryDataManager<T>` (in-memory, no files).
5. For broker tests: use a real `MessageBroker` instance — it is cheap to construct.

## Known Gaps / Future Tests
- `MassRunner` parallel execution and CSV output.
- `PlaygroundFactory` round-trip.
- Agent pathfinding.

> End-to-end HTTP + gRPC integration tests live in the separate `AuxiliumLab.AiSandbox.IntegrationTests` project.
