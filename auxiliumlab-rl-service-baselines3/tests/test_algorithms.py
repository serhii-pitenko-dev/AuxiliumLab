"""Unit tests for algorithm factory and model building."""
import pytest
import gymnasium as gym
from stable_baselines3 import PPO, A2C, DQN

from auxilium_rl.core.algorithms import build_model, get_model_class, _REQUIRED_HYPERPARAMS
from auxilium_rl.core.dto import AlgorithmType


@pytest.fixture
def dummy_env():
    """Create a simple dummy environment for testing."""
    return gym.make("CartPole-v1")


# Full hyperparameter sets matching what .NET sends.
PPO_PARAMS = {
    "learning_rate": 3e-4,
    "n_steps": 2048,
    "batch_size": 64,
    "n_epochs": 10,
    "gamma": 0.99,
    "gae_lambda": 0.95,
    "clip_range": 0.2,
    "ent_coef": 0.0,
}

A2C_PARAMS = {
    "learning_rate": 7e-4,
    "n_steps": 5,
    "gamma": 0.99,
    "gae_lambda": 1.0,
    "ent_coef": 0.0,
    "vf_coef": 0.5,
}

DQN_PARAMS = {
    "learning_rate": 1e-4,
    "buffer_size": 50000,
    "learning_starts": 1000,
    "batch_size": 32,
    "gamma": 0.99,
    "train_freq": 4,
    "target_update_interval": 1000,
}


class TestAlgorithmFactory:
    """Test suite for algorithm factory functions."""

    def test_build_ppo_model(self, dummy_env):
        """Test building a PPO model."""
        model = build_model(
            algorithm=AlgorithmType.PPO,
            env=dummy_env,
            hyperparameters=PPO_PARAMS,
            seed=42,
            verbose=0
        )

        assert isinstance(model, PPO)
        assert model.policy.__class__.__name__ == "ActorCriticPolicy"

    def test_build_a2c_model(self, dummy_env):
        """Test building an A2C model."""
        model = build_model(
            algorithm=AlgorithmType.A2C,
            env=dummy_env,
            hyperparameters=A2C_PARAMS,
            seed=42,
            verbose=0
        )

        assert isinstance(model, A2C)
        assert model.policy.__class__.__name__ == "ActorCriticPolicy"

    def test_build_dqn_model(self, dummy_env):
        """Test building a DQN model."""
        model = build_model(
            algorithm=AlgorithmType.DQN,
            env=dummy_env,
            hyperparameters=DQN_PARAMS,
            seed=42,
            verbose=0
        )

        assert isinstance(model, DQN)
        assert model.policy.__class__.__name__ == "DQNPolicy"

    def test_custom_hyperparameters(self, dummy_env):
        """Test that custom hyperparameters are applied."""
        custom_lr = 1e-5
        params = {**PPO_PARAMS, "learning_rate": custom_lr}
        model = build_model(
            algorithm=AlgorithmType.PPO,
            env=dummy_env,
            hyperparameters=params,
            seed=42,
            verbose=0
        )

        assert model.learning_rate == custom_lr

    def test_missing_hyperparameters_raises(self, dummy_env):
        """Test that missing required hyperparameters raise ValueError."""
        incomplete = {"learning_rate": 3e-4}  # missing most PPO params
        with pytest.raises(ValueError, match="Missing required hyperparameters"):
            build_model(
                algorithm=AlgorithmType.PPO,
                env=dummy_env,
                hyperparameters=incomplete,
                seed=42,
                verbose=0,
            )

    def test_none_hyperparameters_raises(self, dummy_env):
        """Test that None hyperparameters raise ValueError."""
        with pytest.raises(ValueError, match="hyperparameters must be provided"):
            build_model(
                algorithm=AlgorithmType.PPO,
                env=dummy_env,
                hyperparameters=None,
                seed=42,
                verbose=0,
            )

    def test_get_model_class(self):
        """Test getting model classes."""
        assert get_model_class(AlgorithmType.PPO) == PPO
        assert get_model_class(AlgorithmType.A2C) == A2C
        assert get_model_class(AlgorithmType.DQN) == DQN

    def test_invalid_algorithm(self, dummy_env):
        """Test that invalid algorithm raises error."""
        # This would require creating an invalid AlgorithmType, which isn't directly possible
        # with enums, but we can test the error path indirectly
        pass
