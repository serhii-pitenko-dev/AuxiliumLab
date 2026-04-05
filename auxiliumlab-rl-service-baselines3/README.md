# RL Training Service (Stable Baselines3)

A **domain-agnostic gRPC service** for training Reinforcement Learning agents using **Stable Baselines3**. Supports PPO, A2C, and DQN algorithms. Integrates bidirectionally with any gRPC-based simulation engine (e.g. the .NET `AuxiliumLab.AiSandbox`).

## Role in the System

```
Caller (e.g. .NET AiSandbox)
  PolicyTrainerClient  ──── gRPC :50051 ────►  This Python service
                                                  TrainingOrchestrator
                                                    ExternalSimEnv
                                                      GrpcExternalEnvAdapter
                                                        └─── gRPC :50062 ────►  Simulation Host
                                                                                  SimulationService
                                                                                   (gym reset/step)
```

- **Caller → Python (port 50051):** `NegotiateEnvironment`, `StartTrainingPPO/A2C/DQN`, `GetTrainingStatus`, `Act`.
- **Python → Simulation (port 50062):** Python gym calls `Reset`, `Step`, `Close` on the simulation during training.

The service knows nothing about the domain — all environment dimensions, hyperparameters, and step limits are provided by the caller via `NegotiateEnvironment` before each training run.

## Architecture

```
auxilium_rl/
├── transport/          gRPC server layer
│   ├── grpc_server.py      Server factory and startup
│   ├── trainer_servicer.py gRPC handler for PolicyTrainerService RPCs
│   └── health_servicer.py  gRPC health check protocol
├── core/               Business logic (no transport concerns)
│   ├── training.py         TrainingOrchestrator + CheckpointCallback
│   ├── algorithms.py       SB3 model factory (PPO / A2C / DQN)
│   ├── env.py              ExternalSimEnv (gymnasium.Env wrapper)
│   └── dto.py              TrainingConfig, RunInfo, RunStatus, AlgorithmType
└── infra/              Infrastructure
    ├── config.py           ServiceConfig and EnvConfig
    ├── external_env_adapter.py  ExternalEnvAdapter ABC + FakeAdapter + GrpcAdapter
    ├── model_store.py      Model/checkpoint save & load (zip format)
    └── logging.py          Logging setup
```

## Key Components

### `TrainingOrchestrator` (`core/training.py`)
Thread-safe manager for multiple concurrent training runs.
- `start_training(config, adapter)` — starts training in a background thread, returns `run_id`.
- `get_run_status(run_id)` — returns `RunInfo` (timesteps done, status, last checkpoint path).
- `get_model(run_id)` — returns the trained `BaseAlgorithm` for inference.

Training uses `CheckpointCallback` to save intermediate checkpoints every `checkpoint_freq` steps (default 10 000).

### `ExternalSimEnv` (`core/env.py`)
Standard `gymnasium.Env` that delegates all `reset()` / `step()` calls to an `ExternalEnvAdapter`. All dimensions are set at runtime — no defaults.

| Space | Type | Shape |
|---|---|---|
| `observation_space` | `Box(−∞, +∞)` | `(observation_dim,)` — set by `NegotiateEnvironment` |
| `action_space` | `Discrete` | `action_dim` — set by `NegotiateEnvironment` |

`max_steps` controls episode truncation, also set by `NegotiateEnvironment`.

### `ExternalEnvAdapter` (`infra/external_env_adapter.py`)
Adapter interface between the gym and the simulation backend.

| Implementation | Used when |
|---|---|
| `FakeExternalEnvAdapter` | Unit tests and local development without a simulation process |
| `GrpcExternalEnvAdapter` | Production training against a live simulation host (:50062) |

### `ModelStore` (`infra/model_store.py`)
Handles model persistence:
- `save_model(model, run_id)` — saves to `{models_dir}/{run_id}/final.zip`.
- `save_checkpoint(model, run_id, step)` — saves to `{checkpoint_dir}/{run_id}/step_{step}.zip`.
- `load_model(run_id, algorithm)` — loads and returns the model.

### `trainer_servicer.py` (Transport)
Implements `PolicyTrainerServiceServicer`:

| RPC | Handler |
|---|---|
| `NegotiateEnvironment` | Validates and stores env spec (obs_dim, action_dim, max_steps) |
| `StartTrainingPPO` | Starts PPO run via `TrainingOrchestrator` |
| `StartTrainingA2C` | Starts A2C run |
| `StartTrainingDQN` | Starts DQN run |
| `GetTrainingStatus` | Returns progress from run registry |
| `Act` | Loads model and runs `model.predict(observation)` |

**Important:** `NegotiateEnvironment` must be called before any `StartTraining*` RPC. Training will be rejected if environment dimensions have not been negotiated.

## Setup

### 1. Create Virtual Environment
```powershell
python -m venv .venv
.venv\Scripts\Activate.ps1
```

### 2. Install Dependencies
```powershell
pip install -r requirements.txt
```

### 3. Generate gRPC Code (if proto changed)
```powershell
.\scripts\generate_all_grpc.ps1
```
Or manually:
```powershell
python -m grpc_tools.protoc -I./proto --python_out=./generated --grpc_python_out=./generated proto/policy_trainer.proto
python -m grpc_tools.protoc -I./proto --python_out=./generated --grpc_python_out=./generated proto/simulation.proto
```

## Running

```powershell
python server.py        # starts gRPC server on :50051
```

### Environment Variables
| Variable | Default | Description |
|---|---|---|
| `GRPC_HOST` | `0.0.0.0` | Bind address |
| `GRPC_PORT` | `50051` | Listen port |
| `GRPC_MAX_WORKERS` | `10` | gRPC server thread pool size |
| `MODELS_DIR` | `./trained_models` | Final model storage |
| `CHECKPOINT_DIR` | `./checkpoints` | Checkpoint storage |
| `LOG_LEVEL` | `INFO` | Logging verbosity |
| `SIMULATION_GRPC_HOST` | `localhost:50062` | Simulation host address for gym calls |

Environment dimensions (`observation_dim`, `action_dim`, `max_steps`) are **not** configured via env vars — they are negotiated per-experiment via the `NegotiateEnvironment` RPC.

## Testing

```powershell
pytest -v                                      # all tests
pytest tests/test_algorithms.py -v             # algorithm factory
pytest tests/test_env_wrapper.py -v            # gym environment
pytest tests/test_grpc_training_smoke.py -v    # end-to-end smoke
pytest tests/test_health_check.py -v           # health check protocol
pytest tests/test_negotiate_environment.py -v  # environment negotiation
pytest tests/test_inference_algorithm.py -v    # algorithm inference
```

## API Usage

### 1. Negotiate Environment (required before training)
```python
import grpc
from generated import policy_trainer_pb2, policy_trainer_pb2_grpc

channel = grpc.insecure_channel('localhost:50051')
stub = policy_trainer_pb2_grpc.PolicyTrainerServiceStub(channel)

spec = policy_trainer_pb2.EnvironmentSpec(
    observation_dim=126,
    action_dim=5,
    sight_range=5,
    max_steps=400
)
negotiate_resp = stub.NegotiateEnvironment(
    policy_trainer_pb2.NegotiateEnvironmentRequest(
        experiment_id="run_001",
        spec=spec
    )
)
assert negotiate_resp.accepted
```

### 2. Start Training
```python
response = stub.StartTrainingPPO(policy_trainer_pb2.TrainingRequest(
    experiment_id="run_001",
    total_timesteps=100_000,
    seed=42,
    hyperparameters={
        "learning_rate": "3e-4",
        "n_steps": "2048",
        "batch_size": "64",
        "n_epochs": "10",
        "gamma": "0.99",
        "gae_lambda": "0.95",
        "clip_range": "0.2",
        "ent_coef": "0.0",
    },
    model_output_path="./trained_models/run_001.zip"
))
run_id = response.run_id
```

All hyperparameters must be provided by the caller — there are no defaults.

### 3. Poll Status
```python
status = stub.GetTrainingStatus(policy_trainer_pb2.StatusRequest(run_id=run_id))
print(f"Steps done: {status.timesteps_done} | Done: {status.is_done}")
```

### 4. Inference
```python
act = stub.Act(policy_trainer_pb2.ActRequest(
    run_id=run_id,
    observation=[0.1, 0.2, 0.3, 0.4],
    algorithm_type="ppo"
))
print(f"Action: {act.action}, Success: {act.success}")
```

## Required Hyperparameters

All hyperparameters must be supplied by the caller. Training will fail with `ValueError` if any are missing.

| Algorithm | Required keys |
|---|---|
| PPO | `learning_rate`, `n_steps`, `batch_size`, `n_epochs`, `gamma`, `gae_lambda`, `clip_range`, `ent_coef` |
| A2C | `learning_rate`, `n_steps`, `gamma`, `gae_lambda`, `ent_coef`, `vf_coef` |
| DQN | `learning_rate`, `buffer_size`, `learning_starts`, `batch_size`, `gamma`, `train_freq`, `target_update_interval` |

## Proto Files

```
proto/
├── policy_trainer.proto  Caller → Python: negotiate env, start training, get status, act
└── simulation.proto      Python → Simulation: gym reset / step / close
```

The generated stubs in `generated/` are auto-generated and should not be edited manually.

## Adding a New Algorithm
1. Add a value to `AlgorithmType` in `core/dto.py`.
2. Add the SB3 import, a branch in `build_model()`, and required keys in `_REQUIRED_HYPERPARAMS` in `core/algorithms.py`.
3. Add a `StartTrainingXxx` RPC to `proto/policy_trainer.proto`.
4. Regenerate stubs and add a handler in `transport/trainer_servicer.py`.

## Health Checks

The server implements the standard [gRPC Health Checking Protocol](https://github.com/grpc/grpc/blob/master/doc/health-checking.md).

```powershell
python healthcheck.py                    # basic check
python healthcheck.py --verbose          # verbose output
python healthcheck.py --timeout 10       # custom timeout
```

## Build Scripts

```powershell
pwsh -File Makefile.ps1                  # show available commands
pwsh -File Makefile.ps1 generate         # regenerate gRPC stubs (both protos)
pwsh -File Makefile.ps1 test             # run tests
pwsh -File Makefile.ps1 serve            # start server
pwsh -File Makefile.ps1 clean            # clean build artifacts
```

## Troubleshooting

| Problem | Fix |
|---|---|
| `ModuleNotFoundError: No module named 'generated'` | Run protoc / `Makefile.ps1 generate`; ensure `generated/__init__.py` exists |
| `gRPC Connection refused :50051` | Start `python server.py` first |
| `gRPC Connection refused :50062` | Start the simulation host first |
| `NegotiateEnvironment must be called before StartTraining` | Call `NegotiateEnvironment` with valid spec before training |
| `Missing required hyperparameters` | Provide all required hyperparameters for the algorithm |
| Training too slow | Reduce `total_timesteps`; check `max_steps` per episode |
