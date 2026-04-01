"""Unit tests for algorithm inference logic in TrainingOrchestrator."""
import os
import pytest
from unittest.mock import MagicMock, patch
import numpy as np

from auxilium_rl.core.dto import AlgorithmType
from auxilium_rl.core.training import TrainingOrchestrator


class TestInferAlgorithm:
    """Tests for TrainingOrchestrator._infer_algorithm static method."""

    # ── 1. Explicit hint (highest priority) ──────────────────────────────

    @pytest.mark.parametrize("hint,expected", [
        ("ppo", AlgorithmType.PPO),
        ("PPO", AlgorithmType.PPO),
        ("a2c", AlgorithmType.A2C),
        ("A2C", AlgorithmType.A2C),
        ("dqn", AlgorithmType.DQN),
        ("DQN", AlgorithmType.DQN),
    ])
    def test_explicit_hint_resolves(self, hint, expected):
        result = TrainingOrchestrator._infer_algorithm("/any/path/model.zip", hint)
        assert result == expected

    def test_explicit_hint_overrides_directory(self):
        """Hint takes priority over directory name."""
        result = TrainingOrchestrator._infer_algorithm("/models/A2C/model.zip", "ppo")
        assert result == AlgorithmType.PPO

    def test_explicit_hint_overrides_filename(self):
        """Hint takes priority over filename prefix."""
        result = TrainingOrchestrator._infer_algorithm("/models/dqn_model.zip", "ppo")
        assert result == AlgorithmType.PPO

    def test_unknown_hint_falls_back(self):
        """Unknown hint falls through to path inference."""
        result = TrainingOrchestrator._infer_algorithm("/models/PPO/model.zip", "unknown_algo")
        assert result == AlgorithmType.PPO

    # ── 2. Directory path inference (second priority) ────────────────────

    @pytest.mark.parametrize("path,expected", [
        ("/trained_models/PPO/experiment_1/model.zip", AlgorithmType.PPO),
        ("/trained_models/ppo/experiment_1/model.zip", AlgorithmType.PPO),
        ("/data/A2C/model.zip", AlgorithmType.A2C),
        ("/data/DQN/run_42/model.zip", AlgorithmType.DQN),
        # Windows-style paths
        ("D:\\models\\PPO\\model.zip", AlgorithmType.PPO),
        ("D:\\models\\a2c\\run\\model.zip", AlgorithmType.A2C),
    ])
    def test_directory_path_resolves(self, path, expected):
        result = TrainingOrchestrator._infer_algorithm(path, "")
        assert result == expected

    def test_directory_takes_priority_over_filename(self):
        """Dir segment wins over a misleading filename prefix."""
        result = TrainingOrchestrator._infer_algorithm("/models/PPO/a2c_model.zip", "")
        assert result == AlgorithmType.PPO

    # ── 3. Filename prefix inference (lowest priority / legacy) ──────────

    @pytest.mark.parametrize("path,expected", [
        ("/models/ppo_model.zip", AlgorithmType.PPO),
        ("/models/a2c_experiment.zip", AlgorithmType.A2C),
        ("/models/dqn_run42.zip", AlgorithmType.DQN),
    ])
    def test_filename_prefix_resolves(self, path, expected):
        result = TrainingOrchestrator._infer_algorithm(path, "")
        assert result == expected

    # ── 4. Failure case ──────────────────────────────────────────────────

    def test_unknown_path_returns_none(self):
        """When nothing matches, return None."""
        result = TrainingOrchestrator._infer_algorithm("/models/model.zip", "")
        assert result is None

    def test_empty_hint_and_generic_path_returns_none(self):
        """model.zip in a generic directory with no hint → None (the original bug)."""
        result = TrainingOrchestrator._infer_algorithm(
            "/trained_models/some_experiment/model.zip", ""
        )
        assert result is None


class TestLoadModelByPath:
    """Tests for _load_model_by_path with mocked filesystem and model loading."""

    @pytest.fixture
    def orchestrator(self):
        model_store = MagicMock()
        env_config = MagicMock()
        env_config.observation_dim = 10
        env_config.action_dim = 5
        env_config.max_steps = 100
        return TrainingOrchestrator(model_store, env_config)

    def test_returns_none_for_missing_file(self, orchestrator):
        result = orchestrator._load_model_by_path("/nonexistent/path/model.zip")
        assert result is None

    @patch("os.path.exists", return_value=True)
    def test_caches_loaded_model(self, mock_exists, orchestrator):
        fake_model = MagicMock()
        orchestrator.model_store.load_model.return_value = fake_model

        m1 = orchestrator._load_model_by_path("/models/PPO/model.zip")
        m2 = orchestrator._load_model_by_path("/models/PPO/model.zip")

        assert m1 is fake_model
        assert m2 is fake_model
        # load_model should only be called once thanks to caching
        orchestrator.model_store.load_model.assert_called_once()

    @patch("os.path.exists", return_value=True)
    def test_hint_resolves_generic_filename(self, mock_exists, orchestrator):
        """The original bug: model.zip can be loaded when algorithm_hint is provided."""
        fake_model = MagicMock()
        orchestrator.model_store.load_model.return_value = fake_model

        result = orchestrator._load_model_by_path("/models/experiment/model.zip", "ppo")
        assert result is fake_model

    @patch("os.path.exists", return_value=True)
    def test_returns_none_when_algorithm_unknown(self, mock_exists, orchestrator):
        """model.zip without hint or recognisable path → None."""
        result = orchestrator._load_model_by_path("/models/experiment/model.zip")
        assert result is None


class TestPredict:
    """Tests for the predict method with algorithm_hint forwarding."""

    @pytest.fixture
    def orchestrator(self):
        model_store = MagicMock()
        env_config = MagicMock()
        env_config.observation_dim = 10
        env_config.action_dim = 5
        env_config.max_steps = 100
        return TrainingOrchestrator(model_store, env_config)

    @patch("os.path.exists", return_value=True)
    def test_predict_passes_algorithm_hint(self, mock_exists, orchestrator):
        fake_model = MagicMock()
        fake_model.predict.return_value = (np.array(3), None)
        orchestrator.model_store.load_model.return_value = fake_model

        obs = np.zeros(10)
        action = orchestrator.predict("/models/experiment/model.zip", obs, algorithm_hint="ppo")

        assert action == 3
        fake_model.predict.assert_called_once()

    def test_predict_returns_none_when_model_not_found(self, orchestrator):
        obs = np.zeros(10)
        action = orchestrator.predict("/nonexistent/model.zip", obs, algorithm_hint="ppo")
        assert action is None
