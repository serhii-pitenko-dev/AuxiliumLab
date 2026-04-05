# AuxiliumLab.AiSandbox.AiTrainingOrchestrator

**Onion layer: Application**  
Bridges the .NET simulation engine with the Python Stable Baselines3 training service.  
Depends on: `Ai`, `Infrastructure` (project references); also uses `Google.Protobuf`, `Grpc.Net.Client`, `Grpc.Tools` NuGet packages.

## Purpose
- Defines `ITraining` abstractions for each RL algorithm (PPO, A2C, DQN).
- Contains `BaseTraining`, which computes the number of parallel environments based on physical CPU core count.
- Provides `PolicyTrainerClient` — a gRPC client that forwards calls to the Python RL service.
- Manages experiment IDs and model save paths.

## Key Classes

### `BaseTraining`
Abstract base for all training configurations.

| Property / Method | Description |
|---|---|
| `PhysicalCores` | Number of physical CPU cores (auto-detected via `SystemInfo`) |
| `AlgorithmType` | Abstract — overridden by each algorithm implementation |
| `BuildExperimentId(settings)` | Creates a deterministic experiment ID: `{algo}_{params}_{date}` |
| `GetModelSavePath(experimentId)` | Returns `{FileStorage.BasePath}/TRAINED_ALGORITHMS/{algo}/{experimentId}` (base path from `appsettings.json → SandBox.FilesPath`) |

> **Note:** Remote environment scaling (`isSameMachine = false`) is not yet implemented.

### `ITraining` / Concrete Implementations

```
ITraining
├── PpoTraining   (BaseTraining)   — Proximal Policy Optimisation
├── A2cTraining   (BaseTraining)   — Advantage Actor-Critic
└── DqnTraining   (BaseTraining)   — Deep Q-Network
```

Each carries its `TrainingAlgorithmSettings` (hyperparameters loaded from `appsettings.json → TrainingSettings`).

### `IPolicyTrainerClient` / `PolicyTrainerClient`
Thin gRPC client wrapper for the Python `PolicyTrainerService` (port 50051).

| Method | Proto RPC | Description |
|---|---|---|
| `NegotiateEnvironmentAsync(request)` | `NegotiateEnvironment` | **Send environment spec before training.** Python validates the formula, stores config per experiment, echoes spec back. Must succeed before calling `StartTraining*`. |
| `StartTrainingPPOAsync(request)` | `StartTrainingPPO` | Start a PPO training run on the Python side |
| `StartTrainingA2CAsync(request)` | `StartTrainingA2C` | Start an A2C training run |
| `StartTrainingDQNAsync(request)` | `StartTrainingDQN` | Start a DQN training run |
| `GetTrainingStatusAsync(request)` | `GetTrainingStatus` | Poll training progress |
| `ActAsync(request)` | `Act` | Request an action from a loaded trained model (inference) |

The client uses `GrpcChannel.ForAddress(serverAddress)` where `serverAddress` comes from `appsettings.json` → `PolicyTrainerClient.ServerAddress` (default `http://localhost:50051`).

### `EnvironmentSpecBuilder`
Single source of truth for the environment contract sent to the Python RL service.

| Member | Description |
|---|---|
| `const int ScalarFeatureCount = 5` | Fixed scalar features: `x`, `y`, `is_run`, `stamina_frac`, `speed` |
| `const int ActionDim = 5` | Fixed action space size (Move N/S/E/W + Toggle Run) |
| `Build(settings, experimentId)` | Computes `obs_dim = 5 + (2×sightRange+1)²` from `appsettings.json` `SandBox.Hero.SightRange.Current`; generates all feature names (scalars + grid cells `grid[r,c]`) |
| `AssertEchoMatches(sent, echoed, experimentId)` | Throws `InvalidOperationException` if the spec echoed back by Python differs from what was sent |

Formula (observation dimension derivation):
```
gridSize  = 2 × sightRange + 1
obs_dim   = ScalarFeatureCount + gridSize²
          = 5 + (2×sightRange + 1)²
```
Example: `sightRange = 5` → `gridSize = 11` → `obs_dim = 5 + 121 = 126`

### `Configuration/`
- `TrainingSettings` — loaded from `appsettings.json → TrainingSettings`.  
  Contains a list of `TrainingAlgorithmSettings` — one per algorithm type.
- `TrainingAlgorithmSettings` — algorithm name + dictionary of hyperparameter key/value pairs.

## `appsettings.json → TrainingSettings` Reference
```json
{
  "Training": {
    "Algorithms": [
      {
        "Algorithm": "PPO",
        "Parameters": { "n_steps": "2048", "batch_size": "64", "n_epochs": "10" }
      },
      {
        "Algorithm": "A2C",
        "Parameters": { "n_steps": "5" }
      },
      {
        "Algorithm": "DQN",
        "Parameters": { "buffer_size": "50000", "batch_size": "32" }
      }
    ]
  }
}
```

## Proto Files
`Protos/policy_trainer.proto` — defines the `PolicyTrainerService` interface used by `PolicyTrainerClient`.  
The generated stubs are in the `AuxiliumLab.AiSandbox.GrpcHost` project (or imported from the shared proto).

## Training Workflow

`TrainingRunner.RunTrainingAsync()` executes the following steps in order:

1. **Health check** — verifies the Python RL service is reachable.
2. **Build experiment ID** — deterministic string from algorithm + params + date.
3. **Build `EnvironmentSpec`** — `EnvironmentSpecBuilder.Build(sandboxConfig, experimentId, traineeSightRange)` derives `obs_dim` and feature names using the trainee agent's sight range.
4. **`NegotiateEnvironment`** — sends the spec to Python (30 s timeout). Python validates, stores the spec for this experiment, and echoes it back.
5. **Echo verification** — `EnvironmentSpecBuilder.AssertEchoMatches(sent, echoed)` ensures the round-trip is lossless.
6. **`StartTraining*`** — Python pops the stored spec and begins training.

If step 4 or 5 fails, training is aborted before any GPU time is spent.

## Adding a New RL Algorithm
1. Add a new value to the `ModelType` enum in `SharedBaseTypes/AiContract/`.
2. Create a new class `XxxTraining : BaseTraining, ITraining`.
3. Add `StartTrainingXxx` RPC to `policy_trainer.proto` and regenerate code.
4. Add `XxxTrainingAsync` to `IPolicyTrainerClient` and implement in `PolicyTrainerClient`.
5. Update the `switch` in `TrainingRunner.RunTrainingAsync()` to handle the new algorithm.
6. Add an entry to the `TrainingSettings.Algorithms` section of `appsettings.json`.

## Inference Flow (`InferenceActions`)

`InferenceActions` drives a pre-trained model by calling the Python `Act` RPC for every agent decision:

1. Subscribes to `RequestAgentDecisionMakeCommand` on the message broker.
2. Builds an observation vector via `ObservationBuilder.Build(agent)`.
3. Sends an `ActRequest` with `RunId = <model file path>` **and** `AlgorithmType = <ppo|a2c|dqn>`.
4. The Python service uses `AlgorithmType` to load the correct SB3 class (PPO, A2C, or DQN) for the model file — this avoids unreliable filename-based guessing.
5. If the Act RPC returns `Success = false` or throws, `InferenceActions` logs a warning (once per episode) and defaults to action 0.

The optional `ILogger<InferenceActions>` parameter enables structured logging of Act failures — helpful for diagnosing model-loading issues without flooding logs during mass simulations.
