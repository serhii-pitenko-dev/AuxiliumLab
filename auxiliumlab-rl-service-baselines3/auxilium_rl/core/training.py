"""Training orchestration, callbacks, and run management."""
import logging
import os
import threading
from typing import Callable, Dict, Optional
import uuid
from pathlib import Path

from stable_baselines3.common.callbacks import BaseCallback, CallbackList
from stable_baselines3.common.vec_env import DummyVecEnv
from stable_baselines3.common.base_class import BaseAlgorithm
import numpy as np

from .dto import TrainingConfig, RunInfo, RunStatus
from .algorithms import build_model, get_model_class
from .env import ExternalSimEnv
from ..infra.model_store import ModelStore
from ..infra.external_env_adapter import ExternalEnvAdapter, FakeExternalEnvAdapter
from ..infra.config import EnvConfig

logger = logging.getLogger(__name__)


class CheckpointCallback(BaseCallback):
    """Callback for saving periodic checkpoints during training."""
    
    def __init__(
        self,
        model_store: ModelStore,
        run_id: str,
        checkpoint_freq: int = 10000,
        run_registry: Optional[Dict[str, RunInfo]] = None,
        registry_lock: Optional[threading.Lock] = None,
        verbose: int = 0
    ):
        """
        Initialize the checkpoint callback.
        
        Args:
            model_store: Model store for saving checkpoints
            run_id: Unique run identifier
            checkpoint_freq: Save checkpoint every N timesteps
            run_registry: Registry to update progress
            registry_lock: Shared lock protecting run_registry writes
            verbose: Verbosity level
        """
        super().__init__(verbose)
        self.model_store = model_store
        self.run_id = run_id
        self.checkpoint_freq = checkpoint_freq
        self.run_registry = run_registry
        self._lock = registry_lock
    
    def _on_step(self) -> bool:
        """Called at each training step."""
        if self.n_calls % self.checkpoint_freq == 0:
            checkpoint_path = self.model_store.save_checkpoint(
                self.model,
                self.run_id,
                self.num_timesteps
            )
            logger.info(f"[{self.run_id}] Checkpoint saved at timestep {self.num_timesteps}")
            
            # Update run registry
            if self.run_registry and self._lock and self.run_id in self.run_registry:
                with self._lock:
                    self.run_registry[self.run_id].last_checkpoint_path = checkpoint_path
                    self.run_registry[self.run_id].timesteps_done = self.num_timesteps
        
        # Update progress in registry
        if self.run_registry and self._lock and self.run_id in self.run_registry:
            with self._lock:
                self.run_registry[self.run_id].timesteps_done = self.num_timesteps
        
        return True  # Continue training


class TrainingOrchestrator:
    """Manages training runs and provides thread-safe access to run status."""
    
    def __init__(
        self,
        model_store: ModelStore,
        env_config: EnvConfig,
        checkpoint_freq: int = 10000,
        simulation_grpc_host: str = "localhost:50062",
        adapter_factory: Optional[Callable[[str], ExternalEnvAdapter]] = None,
    ):
        """
        Initialize the training orchestrator.
        
        Args:
            model_store: Model store for saving models and checkpoints
            env_config: Environment configuration (fallback when per-experiment spec is unavailable)
            checkpoint_freq: Checkpoint frequency in timesteps
            simulation_grpc_host: Address of the .NET SimulationService gRPC server
            adapter_factory: Factory (gym_id) -> ExternalEnvAdapter for creating adapters.
                             When None, FakeExternalEnvAdapter is used (testing).
        """
        self.model_store = model_store
        self.env_config = env_config
        self.checkpoint_freq = checkpoint_freq
        self.simulation_grpc_host = simulation_grpc_host
        self.adapter_factory = adapter_factory
        
        # Thread-safe run registry
        self.run_registry: Dict[str, RunInfo] = {}
        self.registry_lock = threading.Lock()
        # Cache for models loaded by file path (inference mode)
        self._path_model_cache: Dict[str, BaseAlgorithm] = {}
    
    def start_training(self, config: TrainingConfig) -> str:
        """
        Start a training run asynchronously.
        
        Environment dimensions (observation_dim, action_dim, max_steps) must be
        set on the TrainingConfig before calling this method.
        
        Args:
            config: Training configuration (includes env dimensions)
            
        Returns:
            Unique run ID
            
        Raises:
            ValueError: If environment dimensions are not set on config
        """
        if config.observation_dim <= 0 or config.action_dim <= 0 or config.max_steps <= 0:
            raise ValueError(
                f"Environment dimensions must be positive on TrainingConfig "
                f"(obs_dim={config.observation_dim}, act_dim={config.action_dim}, "
                f"max_steps={config.max_steps}). "
                f"Ensure NegotiateEnvironment was called before StartTraining."
            )

        run_id = str(uuid.uuid4())
        
        # Create run info
        gym_ids_raw = config.hyperparameters.get("gym_ids", "")
        num_envs = len([g for g in gym_ids_raw.split(";") if g]) or 1
        run_info = RunInfo(
            run_id=run_id,
            config=config,
            status=RunStatus.PENDING,
            total_timesteps=config.total_timesteps,
            num_envs=num_envs
        )
        
        # Register the run
        with self.registry_lock:
            self.run_registry[run_id] = run_info
        
        # Start training in background thread
        thread = threading.Thread(
            target=self._train_worker,
            args=(run_id, config),
            daemon=True
        )
        thread.start()
        
        logger.info(f"Started training run {run_id} for {config.algorithm.value.upper()}")
        return run_id
    
    def _train_worker(
        self,
        run_id: str,
        config: TrainingConfig,
    ) -> None:
        """Background worker for training."""
        env = None
        try:
            # Update status to running
            with self.registry_lock:
                self.run_registry[run_id].status = RunStatus.RUNNING
            
            # Read dimensions from the immutable config snapshot — not from
            # the shared env_config which could be overwritten by a concurrent
            # NegotiateEnvironment call.
            obs_dim = config.observation_dim
            act_dim = config.action_dim
            max_steps = config.max_steps

            gym_ids_str = config.hyperparameters.get("gym_ids", "")
            gym_ids = [g for g in gym_ids_str.split(";") if g]

            if len(gym_ids) > 1:
                # Multiple gyms — use the vectorised wrapper.
                # Each env gets its own adapter from the factory.
                def _make_env(gid: str):
                    def _init():
                        a = (self.adapter_factory(gid)
                             if self.adapter_factory
                             else FakeExternalEnvAdapter(observation_dim=obs_dim, action_dim=act_dim))
                        return ExternalSimEnv(
                            adapter=a,
                            observation_dim=obs_dim,
                            action_dim=act_dim,
                            max_steps=max_steps
                        )
                    return _init
                env = DummyVecEnv([_make_env(gid) for gid in gym_ids])
                logger.info(f"[{run_id}] Created DummyVecEnv with {len(gym_ids)} gym(s)")
            else:
                # Single gym or no gym (testing).
                if gym_ids and self.adapter_factory:
                    adapter = self.adapter_factory(gym_ids[0])
                    logger.info(f"[{run_id}] Created adapter for gym_id={gym_ids[0]}")
                else:
                    adapter = FakeExternalEnvAdapter(
                        observation_dim=obs_dim,
                        action_dim=act_dim
                    )
                    if not gym_ids:
                        logger.info(f"[{run_id}] No gym_ids found — using FakeExternalEnvAdapter")
                env = ExternalSimEnv(
                    adapter=adapter,
                    observation_dim=obs_dim,
                    action_dim=act_dim,
                    max_steps=max_steps
                )
            
            # Build model
            hyperparams = config.get_hyperparams_typed()
            model = build_model(
                algorithm=config.algorithm,
                env=env,
                hyperparameters=hyperparams,
                seed=config.seed,
                verbose=1
            )
            
            # Create callbacks
            checkpoint_callback = CheckpointCallback(
                model_store=self.model_store,
                run_id=run_id,
                checkpoint_freq=self.checkpoint_freq,
                run_registry=self.run_registry,
                registry_lock=self.registry_lock,
            )
            
            # Train
            logger.info(f"[{run_id}] Starting training for {config.total_timesteps} timesteps")
            model.learn(
                total_timesteps=config.total_timesteps,
                callback=checkpoint_callback,
                progress_bar=False
            )
            
            # Save final model — if .NET supplied a specific output path use it,
            # otherwise fall back to the default ./trained_models/ directory.
            if config.model_output_path:
                final_path = config.model_output_path
                Path(final_path).parent.mkdir(parents=True, exist_ok=True)
                model.save(final_path)
                logger.info(f"[{run_id}] Saved model to {final_path}")
            else:
                final_path = self.model_store.save_model(model, run_id, config.experiment_id)
            
            # Update status to completed
            with self.registry_lock:
                self.run_registry[run_id].status = RunStatus.COMPLETED
                self.run_registry[run_id].final_model_path = final_path
                self.run_registry[run_id].timesteps_done = config.total_timesteps
                self.run_registry[run_id].model_in_memory = model  # Keep for inference
            
            logger.info(f"[{run_id}] Training completed successfully")
            
        except Exception as e:
            logger.error(f"[{run_id}] Training failed: {e}", exc_info=True)
            with self.registry_lock:
                self.run_registry[run_id].status = RunStatus.FAILED
                self.run_registry[run_id].error_message = str(e)
        finally:
            # Always close the environment so .NET gym executors are released.
            if env is not None:
                try:
                    env.close()
                except Exception as close_err:
                    logger.warning(f"[{run_id}] Error closing env: {close_err}")
    
    def get_run_info(self, run_id: str) -> Optional[RunInfo]:
        """
        Get information about a training run.
        
        Args:
            run_id: Unique run identifier
            
        Returns:
            Run information, or None if not found
        """
        with self.registry_lock:
            return self.run_registry.get(run_id)
    
    def predict(self, run_id: str, observation: np.ndarray, algorithm_hint: str = "") -> Optional[int]:
        """
        Perform inference with a trained model.

        Supports two modes:
        - Training mode: ``run_id`` is a UUID registered during start_training().
        - Inference mode: ``run_id`` is the absolute file path to a saved model
          (sent by .NET's InferenceActions during MassTrainedAISimulation).

        Args:
            run_id: Either a UUID from start_training() or an absolute file path.
            observation: The observation vector.
            algorithm_hint: Optional algorithm type string (e.g. "ppo", "a2c", "dqn")
                            sent by .NET via the ``algorithm_type`` field. Used
                            when loading a model by file path to avoid unreliable
                            filename-based inference.
        """
        # ── Training-mode lookup ─────────────────────────────────────────────
        with self.registry_lock:
            run_info = self.run_registry.get(run_id)
            if run_info:
                if run_info.model_in_memory:
                    action, _ = run_info.model_in_memory.predict(observation, deterministic=True)
                    return int(action)
                if run_info.final_model_path:
                    model_class = get_model_class(run_info.config.algorithm)
                    model = self.model_store.load_model(run_info.final_model_path, model_class)
                    action, _ = model.predict(observation, deterministic=True)
                    return int(action)

        # ── Inference-mode fallback: run_id is a file path ───────────────────
        model = self._load_model_by_path(run_id, algorithm_hint)
        if model is not None:
            action, _ = model.predict(observation, deterministic=True)
            return int(action)

        return None

    def _load_model_by_path(self, model_path: str, algorithm_hint: str = "") -> Optional[BaseAlgorithm]:
        """Load (and cache) a model given its file path.

        Algorithm detection strategy (in priority order):
        1. ``algorithm_hint`` from the gRPC ``ActRequest.algorithm_type`` field.
        2. Parent directory names in the path (e.g. ``/PPO/``).
        3. Filename prefix (legacy fallback, e.g. ``ppo_model.zip``).
        """
        cached = self._path_model_cache.get(model_path)
        if cached is not None:
            return cached

        # Try path as-is first, then with .zip, then without .zip suffix.
        if os.path.exists(model_path):
            actual_path = model_path
        elif os.path.exists(model_path + '.zip'):
            actual_path = model_path + '.zip'
        elif model_path.lower().endswith('.zip') and os.path.exists(model_path[:-4]):
            actual_path = model_path[:-4]
        else:
            logger.warning(f"Model file not found for inference: {model_path}")
            return None

        algo_type = self._infer_algorithm(actual_path, algorithm_hint)
        if algo_type is None:
            return None

        try:
            model_class = get_model_class(algo_type)
            model = self.model_store.load_model(actual_path, model_class)
            self._path_model_cache[model_path] = model
            logger.info(f"Loaded inference model from {actual_path} (algorithm={algo_type.value})")
            return model
        except Exception as e:
            logger.error(f"Failed to load inference model from {actual_path}: {e}")
            return None

    @staticmethod
    def _infer_algorithm(model_path: str, algorithm_hint: str = "") -> Optional["AlgorithmType"]:
        """Infer the SB3 algorithm type for a model file.

        Strategy (in priority order):
        1. Explicit hint from the caller (e.g. ``ActRequest.algorithm_type``).
        2. Parent directory name in the path (e.g. ``…/PPO/experiment/model.zip``).
        3. Filename prefix (e.g. ``ppo_experiment.zip``).
        """
        from .dto import AlgorithmType

        _ALGO_MAP = {
            "ppo": AlgorithmType.PPO,
            "a2c": AlgorithmType.A2C,
            "dqn": AlgorithmType.DQN,
        }

        # 1. Explicit hint
        if algorithm_hint:
            algo = _ALGO_MAP.get(algorithm_hint.lower())
            if algo:
                logger.debug(f"Algorithm resolved from hint: {algorithm_hint}")
                return algo
            logger.warning(f"Unknown algorithm_hint '{algorithm_hint}'; falling back to path inference")

        # 2. Parent directory names (walk up the path looking for PPO / A2C / DQN)
        path_lower = model_path.replace("\\", "/").lower()
        for token, algo in _ALGO_MAP.items():
            if f"/{token}/" in path_lower:
                logger.debug(f"Algorithm resolved from directory path: {token}")
                return algo

        # 3. Filename prefix (legacy)
        fname = os.path.basename(model_path).lower()
        for token, algo in _ALGO_MAP.items():
            if fname.startswith(token):
                logger.debug(f"Algorithm resolved from filename prefix: {token}")
                return algo

        logger.warning(
            f"Cannot infer algorithm type from model path: {model_path}. "
            f"Ensure the path contains a /PPO/, /A2C/, or /DQN/ directory segment, "
            f"or pass algorithm_type in the ActRequest."
        )
        return None
