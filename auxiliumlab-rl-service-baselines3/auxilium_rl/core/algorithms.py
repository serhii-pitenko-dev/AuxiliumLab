"""RL algorithm factory and model builders."""
import logging
from typing import Dict, Any, Optional
from stable_baselines3 import PPO, A2C, DQN
from stable_baselines3.common.base_class import BaseAlgorithm
import gymnasium as gym

from .dto import AlgorithmType

logger = logging.getLogger(__name__)

# Required hyperparameters per algorithm — training will fail if any are missing.
_REQUIRED_HYPERPARAMS: Dict[AlgorithmType, set] = {
    AlgorithmType.PPO: {"learning_rate", "n_steps", "batch_size", "n_epochs", "gamma", "gae_lambda", "clip_range", "ent_coef"},
    AlgorithmType.A2C: {"learning_rate", "n_steps", "gamma", "gae_lambda", "ent_coef", "vf_coef"},
    AlgorithmType.DQN: {"learning_rate", "buffer_size", "learning_starts", "batch_size", "gamma", "train_freq", "target_update_interval"},
}


def build_model(
    algorithm: AlgorithmType,
    env: gym.Env,
    hyperparameters: Optional[Dict[str, Any]] = None,
    seed: int = 0,
    verbose: int = 1
) -> BaseAlgorithm:
    """
    Build a Stable Baselines3 model.

    All hyperparameters must be provided by the caller (.NET side).
    Raises ValueError if any required hyperparameter is missing.

    Args:
        algorithm: Type of algorithm to build
        env: Gymnasium environment
        hyperparameters: Hyperparameters — must contain all required keys
        seed: Random seed
        verbose: Verbosity level

    Returns:
        Initialized SB3 model
    """
    if hyperparameters is None:
        raise ValueError(
            f"hyperparameters must be provided for {algorithm.value.upper()}. "
            f"Required keys: {_REQUIRED_HYPERPARAMS.get(algorithm, set())}"
        )

    params = dict(hyperparameters)

    # n_envs is sent by the .NET orchestrator to let Python know how many parallel
    # gym environments are running on the .NET side. It is NOT a SB3 model
    # constructor argument — parallel envs are managed by .NET (one Sb3Actions per
    # executor), so we just log it and remove it before passing params to the model.
    n_envs = params.pop("n_envs", None)
    if n_envs is not None:
        logger.info(f"Parallel .NET gym environments: {n_envs} (managed by .NET side)")

    # gym_ids is a semicolon-separated list of UUID strings identifying the .NET gym
    # instances. It is routing metadata — not an SB3 constructor argument.
    params.pop("gym_ids", None)

    # Validate required hyperparameters after stripping non-SB3 keys.
    required = _REQUIRED_HYPERPARAMS.get(algorithm, set())
    missing = required - params.keys()
    if missing:
        raise ValueError(
            f"Missing required hyperparameters for {algorithm.value.upper()}: {sorted(missing)}. "
            f"The .NET caller must provide all of: {sorted(required)}"
        )

    logger.info(f"Building {algorithm.value.upper()} model with params: {params}")

    if algorithm == AlgorithmType.PPO:
        return PPO(
            policy="MlpPolicy",
            env=env,
            seed=seed,
            verbose=verbose,
            **params
        )
    elif algorithm == AlgorithmType.A2C:
        return A2C(
            policy="MlpPolicy",
            env=env,
            seed=seed,
            verbose=verbose,
            **params
        )
    elif algorithm == AlgorithmType.DQN:
        return DQN(
            policy="MlpPolicy",
            env=env,
            seed=seed,
            verbose=verbose,
            **params
        )
    else:
        raise ValueError(f"Unsupported algorithm: {algorithm}")


def get_model_class(algorithm: AlgorithmType) -> type:
    """
    Get the SB3 model class for an algorithm.
    
    Args:
        algorithm: Algorithm type
        
    Returns:
        Model class
    """
    if algorithm == AlgorithmType.PPO:
        return PPO
    elif algorithm == AlgorithmType.A2C:
        return A2C
    elif algorithm == AlgorithmType.DQN:
        return DQN
    else:
        raise ValueError(f"Unsupported algorithm: {algorithm}")
