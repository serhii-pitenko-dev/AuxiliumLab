"""Configuration management for the RL training service."""
import os
from dataclasses import dataclass
from typing import Optional


@dataclass
class ServiceConfig:
    """Configuration for the gRPC service."""
    
    host: str = "0.0.0.0"
    port: int = 50051
    max_workers: int = 10
    checkpoint_dir: str = "./checkpoints"
    models_dir: str = "./trained_models"
    log_level: str = "INFO"
    # Address of the .NET SimulationService gRPC server (Python calls this during training).
    # Override with SIMULATION_GRPC_HOST env var, e.g. "host.docker.internal:50062" in Docker.
    simulation_grpc_host: str = "localhost:50062"
    
    @classmethod
    def from_env(cls) -> "ServiceConfig":
        """Load configuration from environment variables."""
        return cls(
            host=os.getenv("GRPC_HOST", "0.0.0.0"),
            port=int(os.getenv("GRPC_PORT", "50051")),
            max_workers=int(os.getenv("GRPC_MAX_WORKERS", "10")),
            checkpoint_dir=os.getenv("CHECKPOINT_DIR", "./checkpoints"),
            models_dir=os.getenv("MODELS_DIR", "./trained_models"),
            log_level=os.getenv("LOG_LEVEL", "INFO"),
            simulation_grpc_host=os.getenv("SIMULATION_GRPC_HOST", "localhost:50062"),
        )


@dataclass
class EnvConfig:
    """Configuration for the Gymnasium environment.

    All fields are set at runtime by NegotiateEnvironment before each training
    run.  There are no defaults — the caller must negotiate before training.
    """

    observation_dim: Optional[int] = None
    action_dim: Optional[int] = None
    max_steps: Optional[int] = None

    @classmethod
    def from_env(cls) -> "EnvConfig":
        """Load environment configuration from environment variables (if set)."""
        obs = os.getenv("OBS_DIM")
        act = os.getenv("ACTION_DIM")
        steps = os.getenv("MAX_STEPS")
        return cls(
            observation_dim=int(obs) if obs else None,
            action_dim=int(act) if act else None,
            max_steps=int(steps) if steps else None,
        )
