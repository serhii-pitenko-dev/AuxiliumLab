# AuxiliumLab.AiSandbox.Domain

**Onion layer: Domain (innermost)**  
The pure domain model. Has **zero dependencies** on any other project in this solution — only references `AuxiliumLab.AiSandbox.SharedBaseTypes`.

## Purpose
Encapsulates all game rules, entities, and domain services. Nothing here knows about databases, files, gRPC, HTTP, or application workflows. This is the heart of the simulation.

## Key Concepts

### `SandboxMapBaseObject`
Abstract base for every object that can exist on the map.  
Provides: `Id` (Guid), `Type` (ObjectType), `Coordinates` (derived from its `Cell`), and `Transparent` flag (controls line-of-sight blocking).

### Map
```csharp
MapSquareCells          // 2D grid of Cell objects
Cell                    // A single grid position; holds exactly one SandboxMapBaseObject
```
- Coordinates start at `(0,0)` in the **bottom-left** corner.
- Every cell is always occupied — an empty cell holds an `EmptyCell` object (Null Object pattern).
- `MapSquareCells.MoveObject(from, to)` is the only way to relocate an object; it enforces that the source is occupied and the destination is empty.
- `CutOutPartOfTheMap(point, radius)` extracts a rectangular sub-grid used by the vision system.

### Inanimate Objects
| Class | `Transparent` | Description |
|---|---|---|
| `EmptyCell` | `true` | Placeholder for an unoccupied cell |
| `Block` | `false` | Solid obstacle; blocks movement and line-of-sight |
| `BorderBlock` | `false` | Impassable perimeter wall placed automatically on every map edge by `PlaygroundBuilder.SetMap()`. Inherits `Block`; carries its own `ObjectType.BorderBlock` so the renderer can colour it distinctly. Never persisted in saved state — re-created on every load. |
| `Exit` | `true` | Goal tile — hero reaching it triggers a win |

### Agents
```
Agent (abstract)
├── Hero    — the AI-controlled protagonist
└── Enemy   — pursues / reacts to the Hero
```

**`Agent` properties:**
| Property | Description |
|---|---|
| `Speed` | Cells moved per turn |
| `SightRange` | Radius for vision calculations |
| `Stamina` / `MaxStamina` | Running budget |
| `IsRun` | Whether agent is currently sprinting |
| `PathToTarget` | A* path to current navigation target |
| `VisibleCells` | Cells the agent can currently see |
| `AvailableActions` | Actions the agent may take this turn |
| `ExecutedActions` | Actions already taken this turn |
| `OrderInTurnQueue` | Turn priority |

**`AgentAction` enum:**
- `Move` — move one cell in a direction
- `Run` — toggle sprinting (consumes stamina, increases speed)

### Vision — `VisibilityService`
`IVisibilityService.UpdateVisibleCells(agent, playground)`:
1. Cuts the map around the agent within `SightRange` radius.
2. Iterates every cell in the square sub-grid.
3. Skips cells outside the circular sight radius.
4. Uses **Bresenham line-of-sight** (`HasLineOfSight`) — a ray is blocked the moment it passes through a non-transparent (`Block`) cell.
5. Populates `agent.VisibleCells` with references to the live `Cell` objects on the map.

### `StandardPlayground` — Aggregate Root
The single entry point for all interactions with the simulation state.

| Method | Description |
|---|---|
| `PlaceHero(hero, coords)` | Place the hero on the map |
| `PlaceEnemy(enemy, coords)` | Add an enemy to the map |
| `PlaceBlock(block, coords)` | Add a wall/obstacle |
| `PlaceExit(exit, coords)` | Set the exit tile |
| `MoveObject(from, to)` | Delegate move to `MapSquareCells` |
| `LookAroundEveryone()` | Refresh visible cells for all agents |
| `UpdateAgentVision(agent)` | Refresh visible cells for one agent |
| `PrepareAgentForTurnActions(agent)` | Reset stamina, recalculate available actions |
| `OnStartTurnActions()` | Increment turn counter |
| `CutMapPart(point, radius)` | Expose map slice for vision/AI |
| `GetCell(coords)` | Direct cell lookup |

### Validation
`Validation/Agents/AgentActionAddValidator` — guards against adding duplicate or invalid actions to an agent's available-action list.
`MapValidator` — validates the map before a simulation starts (hero present, exit present, dimensions valid, etc.).

### Factories & Builders
- `Playgrounds/Builders/` — fluent builder pattern for constructing a `StandardPlayground` from configuration.
- `Playgrounds/Factories/` — creates playgrounds either randomly or from a saved file.
- `Agents/Factories/` — creates `Hero` and `Enemy` instances from `InitialAgentCharacters`.

## Folder Structure
```
Domain/
├── Agents/
│   ├── Entities/           Agent, Hero, Enemy, InitialAgentCharacters
│   ├── Factories/          HeroFactory, EnemyFactory
│   └── Services/Vision/    IVisibilityService, VisibilityService
├── Configuration/          DomainServiceCollectionExtensions (DI registration)
├── InanimateObjects/       Block, BorderBlock, EmptyCell, Exit
├── Maps/                   Cell, MapSquareCells
├── Playgrounds/
│   ├── Builders/           PlaygroundBuilder
│   ├── Factories/          PlaygroundFactory, PlaygroundFromFileFactory
│   └── StandardPlayground.cs
├── Validation/
│   ├── Agents/             AgentActionAddValidator
│   └── MapValidator.cs
└── SandboxMapBaseObject.cs
```

## Adding a New Map Object Type
1. Add the new value to `ObjectType` enum in `SharedBaseTypes`.
2. Create a class inheriting `SandboxMapBaseObject`, set `Transparent` appropriately.
3. If it is an agent type, inherit `Agent` and implement `Clone()`.
4. Register via a factory in `Agents/Factories/` or `InanimateObjects/`.
5. Update `PlaygroundBuilder` / `PlaygroundFactory` to place the new object.

## Adding a New Agent Action
1. Add the value to the `AgentAction` enum in `SharedBaseTypes`.
2. Override `DoAction(action, isActivated)` in the relevant `Agent` subclass.
3. Update `Agent.ReCalculateAvailableActions()` to include the new action when appropriate.
4. Update the AI observation contracts in `SharedBaseTypes.AiContract` so the model can select the action.
