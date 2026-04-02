# AuxiliumLab.AiSandbox.SharedBaseTypes

**Onion layer: Domain (innermost)**  
A leaf project with **no dependencies** on any other solution project. Referenced by every other project in the solution.

## Purpose
Holds all shared value types, enumerations, and message-type base classes. Because every project can depend on this one without creating circular references, it acts as the shared vocabulary of the system.

## Contents

### `ValueObjects/`

| Type | Kind | Description |
|---|---|---|
| `Coordinates` | `class` | `(int X, int Y)` grid position. `(0,0)` = bottom-left. Value-equality via manual `Equals`/`GetHashCode` overrides. |
| `ObjectType` | `enum` | `Empty, Block, Hero, Enemy, Exit, BorderBlock` |
| `AgentAction` | `enum` | `Run, Move` |
| `SandboxStatus` | `enum` | `InProgress, HeroWon, HeroLost, TurnLimitReached, Failed` |
| `MapType` | `enum` | `Standard = 1, Empty = 2` — map generation strategy |
| `AgentSnapshot` | `record` | Immutable copy of agent state at a point in time (used in events/messages) |

#### `StartupSettings/`
Enums used by the `Startup` project.

| Type | Description |
|---|---|
| `ExecutionMode` | `Training, SingleRandomAISimulation, SingleTrainedAISimulation, MassRandomAISimulation, MassTrainedAISimulation, LoadSimulation, TestPreconditions, AggregationRun` |
| `PresentationMode` | `WithoutVisualization, Console, Web` |

### `MessageTypes/`

Base types for the in-process pub/sub bus (`IMessageBroker`).

| Type | Description |
|---|---|
| `Message` | Abstract root for all messages. Carries a `Guid Id`. |
| `Command` | A message that requests a state change. |
| `Event` | A message that announces something that happened. |
| `Query` | A message that requests data (no side effects). |
| `Response` | A reply to a `Query`. |

**Convention:** every concrete message defined in `Common/MessageBroker/Contracts/` must inherit one of these four base types.

### `AiContract/` (defined in `Common/MessageBroker/Contracts/AiContract/`)
DTOs for the AI decision interface. These files live in the `Common` project but use the `AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract` namespace.

| Type | Description |
|---|---|
| `AgentStateForAIDecision` | Snapshot passed to `IAiActions.GetAction()` — visible cells, agent stats, available actions |

## Adding a New Value Object
- Place the new `record` / `enum` here if it is referenced by more than one project layer.
- Keep value objects immutable (`record struct` or `readonly record`).
- Do **not** add any logic that depends on Infrastructure or Application Services here.

## Adding a New Message Type
1. Decide whether it is a `Command`, `Event`, `Query`, or `Response`.
2. Create the record in `Common/MessageBroker/Contracts/<ContractFolder>/`.
3. The base types (`Command`, `Event`, etc.) live here in `SharedBaseTypes/MessageTypes/` — do not move them.
