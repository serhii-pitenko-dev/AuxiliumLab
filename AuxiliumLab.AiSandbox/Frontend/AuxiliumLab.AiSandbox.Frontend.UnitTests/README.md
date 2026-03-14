# AuxiliumLab.AiSandbox.Frontend.UnitTests

bUnit + MSTest unit test project for the Blazor WebAssembly frontend.

## Stack

| Package | Purpose |
|---|---|
| `MSTest.TestFramework` | Test runner |
| `bunit` | Blazor component rendering in tests |
| `FluentAssertions` | Readable assertions |
| `Moq` | Mocking interfaces |

## Running tests

```bash
dotnet test
```

## Test coverage

| Folder | Tests |
|---|---|
| `Training/` | `PpoTrainingPageTests`, `TrainingStatusPageTests` |
| `Simulation/` | `MultipleRunPageTests`, `SimulationStatusPageTests` |
| `AggregationRun/` | `AggregationRunPageTests`, `AggregationStatusPageTests` |
| `DataManagement/` | `AggregationVisualizationPageTests`, `MultipleRunVisualizationPageTests` |
| `Services/` | `NotificationServiceTests` |

## Patterns

Each test class:
1. Creates a `bunit.TestContext`
2. Calls `ctx.Services.AddMudServices()` to register MudBlazor
3. Registers mock implementations of all injected interfaces
4. Renders the component under test with `ctx.RenderComponent<T>()`
5. Asserts on markup or verifies mock invocations
