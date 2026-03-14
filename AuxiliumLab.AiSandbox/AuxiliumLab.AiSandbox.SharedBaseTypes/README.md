# AuxiliumLab.AiSandbox.SharedBaseTypes

**Onion layer: Domain (innermost)**  
A leaf project with **no dependencies** on any other solution project. Referenced by every other project in the solution.

## Purpose
Holds all shared value types, enumerations, and message-type base classes. Because every project can depend on this one without creating circular references, it acts as the shared vocabulary of the system.

## Contents

### `ValueObjects/`

| Type | Kind | Description |
|---|---|---|
| `Coordinates` | `record struct` | `(int X, int Y)` grid position. `(0,0)` = bottom-left |
| `ObjectType` | `enum` | `Empty, Block, Hero, Enemy, Exit, BorderBlock` |
| `AgentAction` | `enum` | `Move, Run` |
| `SandboxStatus` | `enum` | `InProgress, HeroWon, HeroLost, TurnLimitReached` |
| `MapType` | `enum` | Random vs. file-loaded map generation strategy |
| `AgentSnapshot` | `record` | Immutable copy of agent state at a point in time (used in events/messages) |

#### `StartupSettings/`
Strongly-typed settings records used by the `Startup` project.

| Type | Description |
|---|---|
| `StartupSettings` | Top-level settings: execution mode, presentation mode, web API flag |
| `ExecutionMode` | `SingleSimulation, MassRandomAISimulation, Training, TestPreconditions` |
| `PresentationMode` | `Console, WithoutVisualization` |

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

### `AiContract/` (inside `SharedBaseTypes` or referenced from `Common`)
DTOs for the AI decision interface:

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
