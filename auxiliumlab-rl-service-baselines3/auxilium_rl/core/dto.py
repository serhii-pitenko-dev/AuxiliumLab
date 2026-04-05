"""Data Transfer Objects for internal use."""
from dataclasses import dataclass, field
from typing import Any, Dict, Optional
from enum import Enum


class AlgorithmType(Enum):
    """Supported RL algorithms."""
    PPO = "ppo"
    A2C = "a2c"
    DQN = "dqn"


class RunStatus(Enum):
    """Training run status."""
    PENDING = "pending"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"


@dataclass
class TrainingConfig:
    """Configuration for a training run."""
    algorithm: AlgorithmType
    experiment_id: str
    total_timesteps: int
    seed: int
    hyperparameters: Dict[str, str] = field(default_factory=dict)
    model_output_path: str = ""
    # Environment dimensions — set by NegotiateEnvironment, immutable per run.
    observation_dim: int = 0
    action_dim: int = 0
    max_steps: int = 0
    
    def get_hyperparams_typed(self) -> Dict[str, Any]:
        """Convert string hyperparameters to appropriate types.

        Raises:
            ValueError: If a numeric hyperparameter cannot be parsed.
        """
        typed_params = {}
        
        float_params = {"learning_rate", "gamma", "gae_lambda", "ent_coef", "vf_coef", "clip_range"}
        int_params = {
            "n_steps", "batch_size", "n_epochs", "buffer_size",
            "learning_starts", "train_freq", "target_update_interval",
        }
        # Keys that are routing metadata, not SB3 constructor params.
        skip_params = {"gym_ids", "n_envs"}
        
        for key, value in self.hyperparameters.items():
            if key in skip_params:
                continue
            if key in float_params:
                try:
                    typed_params[key] = float(value)
                except (ValueError, TypeError) as exc:
                    raise ValueError(
                        f"Hyperparameter '{key}' for {self.algorithm.value.upper()} "
                        f"must be a float, got '{value}'. Fix the value on the .NET caller side."
                    ) from exc
            elif key in int_params:
                try:
                    typed_params[key] = int(value)
                except (ValueError, TypeError) as exc:
                    raise ValueError(
                        f"Hyperparameter '{key}' for {self.algorithm.value.upper()} "
                        f"must be an integer, got '{value}'. Fix the value on the .NET caller side."
                    ) from exc
            else:
                # Unknown param — try int, then float, then keep as string.
                try:
                    typed_params[key] = int(value)
                except (ValueError, TypeError):
                    try:
                        typed_params[key] = float(value)
                    except (ValueError, TypeError):
                        typed_params[key] = value
        
        return typed_params


@dataclass
class RunInfo:
    """Information about a training run."""
    run_id: str
    config: TrainingConfig
    status: RunStatus
    timesteps_done: int = 0
    total_timesteps: int = 0
    num_envs: int = 1
    last_checkpoint_path: Optional[str] = None
    final_model_path: Optional[str] = None
    error_message: Optional[str] = None
    model_in_memory: Optional[Any] = None  # For inference after training
