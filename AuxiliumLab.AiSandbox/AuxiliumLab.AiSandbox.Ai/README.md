# AuxiliumLab.AiSandbox.Ai

This project is the **AI decision-making layer** of the AuxiliumLab sandbox. It defines how agents choose actions at each simulation step — whether through random selection, or through a trained Stable-Baselines3 (SB3) reinforcement-learning model running in Python.

---

## Project Structure

```
AuxiliumLab.AiSandbox.Ai/
├── IAiActions.cs               # Common interface for all AI implementations
├── RandomActions.cs            # Baseline: random policy (no ML)
├── Sb3Actions.cs               # SB3 RL policy: bridges Python gym ↔ .NET simulation
├── Sb3AlgorithmTypeProvider.cs # Factory for Sb3Actions instances
└── Configuration/
    ├── AiConfiguration.cs      # Per-instance config (ModelType, PolicyType, Version)
    ├── AiPolicy.cs             # Enum: MLP | LSTM
    ├── ModelType.cs            # Enum: Random | PPO | A2C | DQN
    └── AiSandBoxCollectionExtensions.cs  # DI registration
```

---

## IAiActions

The common contract for all AI implementations:

```csharp
public interface IAiActions
{
    AiConfiguration AiConfiguration { get; init; }
    void Initialize();
}
```

`Initialize()` is called once at startup. It must subscribe to all message-broker events the implementation needs to function.

---

## RandomActions

A **baseline random policy** used in non-training modes (Simulation, Testing, etc.).

- Registered as `IAiActions` in DI for all `ExecutionMode` values except `Training`.
- Each step it picks a random action from `agent.AvailableLimitedActions`.
- Supported actions:
  - **Move** — moves to a random neighbouring cell (Chebyshev distance = 1).
  - **Run** — 10% chance to toggle the run ability on; 10% chance to toggle it off.
- Does **not** build observations — it reads `AgentStateForAIDecision` directly but ignores the vision grid.

---

## Sb3Actions

The **RL training/inference bridge**. One instance per parallel gym environment (identified by `GymId`).

### Role

Acts as the adapter between:
- The **Python SB3 gym** (sends Reset/Step/Close commands via gRPC → message broker)
- The **.NET simulation** (sends GameStarted, DecisionMakeCommand, HeroWon/Lost events)

### Lifecycle

```
Python gym          Message Broker          .NET Simulation
─────────           ──────────────          ───────────────
Reset ──────────►  OnSimulationReset
                        │ starts episode ──► GameStartedEvent
                        │                ◄── RequestAgentDecisionMakeCommand
                        │ BuildObservation
                   SimulationResetResponse
◄── initial obs ──

Step(action) ───►  OnSimulationStep
                        │ forwards action to _actionTcs
                        │                ──► AgentDecisionMoveResponse
                        │                ◄── RequestAgentDecisionMakeCommand  (or HeroWon/Lost)
                        │ BuildObservation + reward
                   SimulationStepResponse
◄── obs, reward ──
```

### Parallelism model

N gym environments run **simultaneously** (width), not nested. Each gym has its own `Sb3Actions` instance, its own isolated message broker, and its own executor — so there is no cross-gym state sharing.

Within a single gym, episodes are **sequential**: one episode completes before the next `Reset` starts the next. However, the message broker is asynchronous, so broker events from episode N (e.g. a late `RequestAgentDecisionMakeCommand`) can arrive after episode N+1 has already begun. The `_playgroundId` field guards against this: `OnDecisionRequest`, `OnHeroWon`, and `OnHeroLost` all check `evt.PlaygroundId == _playgroundId` and silently drop messages from the previous episode. `OnGameStarted` has no guard because it is the event that *establishes* `_playgroundId` for the new episode.

### Reward Scheme

| Event | Reward | Configured via |
|---|---|---|
| Each step taken | `_stepPenalty` (default `-0.1`) | `appsettings.json → TrainingSettings.Rewards.StepPenalty` |
| Hero reached exit | `_winReward` (default `+10.0`) | `appsettings.json → TrainingSettings.Rewards.WinReward` |
| Hero died / timed out | `_lossReward` (default `-10.0`) | `appsettings.json → TrainingSettings.Rewards.LossReward` |

The step penalty encourages the agent to reach the exit in as few steps as possible.

### Action Space (discrete, 5 actions)

| Index | Effect |
|---|---|
| 0 | Move up — `Y - 1` |
| 1 | Move down — `Y + 1` |
| 2 | Move left — `X - 1` |
| 3 | Move right — `X + 1` |
| 4 | Toggle run ability |

### Observation Space

A flat `float[]` of size `5 + gridSize²` where `gridSize = 2 * SightRange + 1`.

With the default `SightRange = 5`: `5 + 11² = 126` floats.

**Must stay in sync with `OBS_DIM` in the Python service.**

#### Scalar features — indices 0–4

| Index | Value |
|---|---|
| 0 | `agent.Coordinates.X` — absolute map column (0-based) |
| 1 | `agent.Coordinates.Y` — absolute map row (0-based) |
| 2 | `agent.IsRun ? 1.0 : 0.0` — run mode flag |
| 3 | `agent.Stamina / agent.MaxStamina` — stamina fraction [0, 1] |
| 4 | `agent.Speed` — cells traversable per turn |

#### Vision grid — indices 5 .. (5 + gridSize² − 1)

A `gridSize × gridSize` local grid **centred on the agent**, stored in **row-major order** (top row first).

**Cell value encoding:**

| Value | Meaning |
|---|---|
| `-1.0` | Not visible (outside SightRange or blocked by LOS) |
| `0.0` | `ObjectType.Empty` |
| `1.0` | `ObjectType.Hero` |
| `2.0` | `ObjectType.Enemy` |
| `3.0` | `ObjectType.Block` |
| `4.0` | `ObjectType.Exit` |

**Grid layout** (Y increases downward — screen convention):

```
gy=0  → map Y = agentY - SightRange   (topmost row, smallest Y, "above" agent)
gy=1  → map Y = agentY - SightRange+1
...
gy=SR → map Y = agentY                ← agent row (centre)
...
gy=2*SR → map Y = agentY + SightRange (bottommost row, largest Y, "below" agent)
```

**Concrete example** — agent at (X=3, Y=9), SightRange=2, gridSize=5:

```
gy=0 → map Y=7:  (1,7)(2,7)(3,7)(4,7)(5,7)  → obs[5..9]
gy=1 → map Y=8:  (1,8)(2,8)(3,8)(4,8)(5,8)  → obs[10..14]
gy=2 → map Y=9:  (1,9)(2,9)(3,9)(4,9)(5,9)  → obs[15..19]  ← agent centre
gy=3 → map Y=10: (1,10)...(5,10)             → obs[20..24]
gy=4 → map Y=11: (1,11)...(5,11)             → obs[25..29]
```

**Index formula:**
```
obs[5 + gy * gridSize + gx]
     │    │              └─ column in local grid
     │    └─ skip gy complete rows of gridSize cells each
     └─ skip the 5 scalar features at the front
```

Where:
- `gx = dx + SightRange`,  `dx = cell.X - agent.X`
- `gy = dy + SightRange`,  `dy = cell.Y - agent.Y`

---

## Sb3AlgorithmTypeProvider

A **factory** that creates `Sb3Actions` instances. Each call to `Create()` produces a fresh instance with a new `GymId`.

Used by `TrainingRunner` to create one `Sb3Actions` per parallel executor (gym environment). Reward parameters are forwarded from `TrainingSettings.Rewards` in `appsettings.json`.

---

## Configuration

### ModelType

```csharp
public enum ModelType { Random, PPO, A2C, DQN }
```

Determines which SB3 algorithm Python will use. Passed to Python via gRPC at training start.

### AiPolicy

```csharp
public enum AiPolicy { MLP, LSTM }
```

- **MLP** — stateless feed-forward network. The current observation must contain all information the agent needs. Currently used.
- **LSTM** — recurrent network with hidden state across steps. Useful if partial observability cannot be resolved by the local vision grid alone.

### AiConfiguration

Carried on each `IAiActions` instance. Consumed by the gRPC training orchestrator to tell Python the algorithm and policy type.

### DI Registration (`AiSandBoxCollectionExtensions`)

```csharp
services.AddAiSandboxServices(executionMode);
```

- **All modes except Training**: registers `RandomActions` as `IAiActions` (scoped).
- **Training mode**: `IAiActions` is **not** registered from DI. `TrainingRunner` creates `Sb3Actions` instances directly via `Sb3AlgorithmTypeProvider`, one per gym.

---

## Adding a New AI Implementation

1. Create a class implementing `IAiActions`.
2. Subscribe to relevant message-broker events in `Initialize()`.
3. If it requires DI registration for non-training modes, add it to `AiSandBoxCollectionExtensions`.
4. If it is an RL algorithm, add the value to `ModelType` and handle it in the Python service.

---

## Key Constraints

- `BuildObservation` output size **must match** `OBS_DIM` in the Python `server.py` / `env.py`. If `SightRange` changes in `appsettings.json`, update `OBS_DIM` in Python accordingly: `OBS_DIM = 5 + (2 * SightRange + 1) ** 2`.
- `Sb3Actions` is **not thread-safe** across multiple gyms — each parallel gym must have its own instance (enforced by `Sb3AlgorithmTypeProvider`).
- `VisibleCells` is pre-filtered by `VisibilityService` (Bresenham LOS + range check). `BuildObservation` does not re-filter.
