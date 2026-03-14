# AuxiliumLab.AiSandbox.Common

**Onion layer: Domain / Cross-cutting**  
Contains cross-cutting concerns shared by all layers.  
Depends only on `SharedBaseTypes`.

## Purpose
Provides an in-process **publish/subscribe message broker** and shared helper utilities. This is the main decoupling mechanism between the simulation engine and all consumers (UI, gRPC, AI).

## `MessageBroker`

### Interface
```csharp
public interface IMessageBroker
{
    void Publish<TMessage>(TMessage message) where TMessage : Message;
    void Subscribe<TMessage>(Action<TMessage> handler) where TMessage : notnull, Message;
    void Unsubscribe<TMessage>(Action<TMessage> handler) where TMessage : Message;
}
```

### Implementation — `MessageBroker`
- Thread-safe: uses `ConcurrentDictionary<Type, List<Delegate>>` with per-handler-list locking.
- All handlers are invoked **synchronously** on the publisher's thread.
- A separate handler-list exists for `Response` messages (used in request/response patterns).
- Registered as a **singleton** in DI — the same instance is shared across the entire process.

### `IBrokerRpcClient` / `BrokerRpcClient`
A wrapper that lets an executor send gRPC messages via the message broker (Sb3Contract RPC pattern). Used when the Python SB3 training loop needs a synchronous request/response cycle within a single gym step.

## Message Contracts

All message contracts live in `MessageBroker/Contracts/`:

```
Contracts/
├── AiContract/           Commands and events for AI decision-making
│   ├── Commands/         RequestAiActionCommand
│   ├── Events/           (AI-related events)
│   └── Responses/        AiActionResponse, AgentObservationResponse
├── CoreServicesContract/ Internal simulation events
│   └── Events/           GameStartedEvent, TurnExecutedEvent, OnBaseAgentActionEvent
├── GlobalMessagesContract/ Game outcome events
│   └── Events/
│       ├── Win/          HeroWonEvent (WinReason enum)
│       └── Lost/         HeroLostEvent (LostReason enum)
└── Sb3Contract/          Gym interface messages for SB3 training
    ├── Commands/         RequestSimulationResetCommand, RequestSimulationStepCommand
    └── Responses/        SimulationResetResponse, SimulationStepResponse
```

### Key Message Flow

| Event | Published by | Consumed by |
|---|---|---|
| `GameStartedEvent` | `Executor` | `ConsoleRunner` (renders initial map) |
| `TurnExecutedEvent` | `Executor` | `ConsoleRunner` (re-renders changed cells) |
| `OnBaseAgentActionEvent` | `ExecutorForPresentation` | `ConsoleRunner` (animates per-action updates) |
| `HeroWonEvent` | `Executor` | `ConsoleRunner`, `StandardExecutor` (captures result) |
| `HeroLostEvent` | `Executor` | `ConsoleRunner`, `StandardExecutor` (captures result) |
| `RequestSimulationResetCommand` | `SimulationService` (gRPC) | Training executor (resets playground) |
| `RequestSimulationStepCommand` | `SimulationService` (gRPC) | Training executor (executes one step) |
| `SimulationResetResponse` | Training executor | `SimulationService` (completes gRPC call) |
| `SimulationStepResponse` | Training executor | `SimulationService` (completes gRPC call) |

## `Helpers/`

### `SystemInfo`
```csharp
SystemInfo.GetPhysicalCoreCount()   // Returns the number of physical CPU cores
```
Used by `BaseTraining` in `AiTrainingOrchestrator` to scale the number of parallel training environments to the machine's physical core count.

## How to Add a New Message Type
1. Decide on the contract folder: `AiContract`, `CoreServicesContract`, `GlobalMessagesContract`, or `Sb3Contract`. Create a new folder if the contract belongs to a different subsystem.
2. Create a `record` inheriting `Command`, `Event`, `Query`, or `Response` (from `SharedBaseTypes.MessageTypes`).
3. The publisher calls `_messageBroker.Publish(new MyEvent(...))`.
4. Any subscriber calls `_messageBroker.Subscribe<MyEvent>(handler)` at startup and `Unsubscribe` on disposal.

## Registration
`Common/Extensions/CommonServiceCollectionExtensions.cs` registers:
- `IMessageBroker` as singleton `MessageBroker`.
- `IBrokerRpcClient` as singleton `BrokerRpcClient`.
