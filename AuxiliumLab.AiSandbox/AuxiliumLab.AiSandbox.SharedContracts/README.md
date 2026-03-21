# AuxiliumLab.AiSandbox.SharedContracts

**Cross-cutting DTOs** shared between the backend (WebApi, ApplicationServices) and the Blazor frontend.  
No business logic — pure data contracts for serialization over SignalR / HTTP.

## Simulation DTOs (`Simulation/SimulationHubEvents.cs`)

All simulation visualization events use strongly typed enums instead of raw strings.

### Enums

| Type | Values | Purpose |
|---|---|---|
| `EEffect` | `Path`, `Vision`, `Run` | Effect types agents apply to map cells |

### DTOs

| Class | Key fields | Description |
|---|---|---|
| `AgentEffectDto` | `AgentId`, `AgentType` (`ObjectType`), `Effects` (`EEffect[]`) | Describes a single agent's effects on a cell |
| `SimulationCellDto` | `X`, `Y`, `ObjectType`, `Effects` (`AgentEffectDto[]`) | Single cell transmitted via SignalR |
| `AgentSnapshotDto` | `Id`, `Type` (`ObjectType`), `Speed`, `SightRange`, `IsRun`, `Stamina`, … | Agent state accompanying action events |
| `InitialAgentDto` | `AgentId`, `AgentType` (`ObjectType`), `X`, `Y`, `Snapshot` | Initial agent position at simulation start |
| `SimulationStartedDto` | `Width`, `Height`, `MaxTurns`, `Cells`, `Agents` | Full initial map + agent positions |
| `AgentMovedDto` | `AgentId`, `AgentType` (`ObjectType`), `From/To`, `IsSuccess`, `Agent`, `UpdatedCells` | Agent move action |
| `AgentToggledDto` | `AgentId`, `AgentType` (`ObjectType`), `Action`, `IsActivated`, `Agent` | Agent toggle action (Run on/off) |
| `TurnCompletedDto` | `TurnNumber`, `UpdatedCells` | Per-turn cell updates |
| `SimulationEndedDto` | `Outcome`, `Reason`, `FinalTurn` | Simulation end event |

### Other

| File | Types | Description |
|---|---|---|
| `SimulationApiContracts.cs` | `StartSingleSimulationCommand`, `StartMassSimulationCommand`, `SimulationJobStartedDto`, … | REST API request/response DTOs |

## Dependencies

- `SharedBaseTypes` — `ObjectType` enum (`Hero`, `Enemy`, `Block`, `BorderBlock`, `Exit`, `Empty`)
