"""Unit tests for DTO type conversion and validation."""
import pytest

from auxilium_rl.core.dto import AlgorithmType, TrainingConfig


class TestGetHyperparamsTyped:
    """Tests for TrainingConfig.get_hyperparams_typed()."""

    def _config(self, hyperparameters: dict) -> TrainingConfig:
        return TrainingConfig(
            algorithm=AlgorithmType.DQN,
            experiment_id="test",
            total_timesteps=100,
            seed=0,
            hyperparameters=hyperparameters,
        )

    def test_train_freq_is_int(self):
        """train_freq must be converted to int, not float."""
        cfg = self._config({"train_freq": "4"})
        result = cfg.get_hyperparams_typed()
        assert result["train_freq"] == 4
        assert isinstance(result["train_freq"], int)

    def test_target_update_interval_is_int(self):
        """target_update_interval must be converted to int, not float."""
        cfg = self._config({"target_update_interval": "1000"})
        result = cfg.get_hyperparams_typed()
        assert result["target_update_interval"] == 1000
        assert isinstance(result["target_update_interval"], int)

    def test_learning_rate_is_float(self):
        """learning_rate must be converted to float."""
        cfg = self._config({"learning_rate": "3e-4"})
        result = cfg.get_hyperparams_typed()
        assert result["learning_rate"] == pytest.approx(3e-4)
        assert isinstance(result["learning_rate"], float)

    def test_invalid_float_raises(self):
        """Non-numeric float param must raise a clear ValueError."""
        cfg = self._config({"learning_rate": "not_a_number"})
        with pytest.raises(ValueError, match="must be a float"):
            cfg.get_hyperparams_typed()

    def test_invalid_int_raises(self):
        """Non-numeric int param must raise a clear ValueError."""
        cfg = self._config({"batch_size": "abc"})
        with pytest.raises(ValueError, match="must be an integer"):
            cfg.get_hyperparams_typed()

    def test_gym_ids_skipped(self):
        """gym_ids is routing metadata and must not appear in typed params."""
        cfg = self._config({"gym_ids": "id1;id2", "learning_rate": "1e-3"})
        result = cfg.get_hyperparams_typed()
        assert "gym_ids" not in result
        assert "learning_rate" in result

    def test_n_envs_skipped(self):
        """n_envs is routing metadata and must not appear in typed params."""
        cfg = self._config({"n_envs": "4", "learning_rate": "1e-3"})
        result = cfg.get_hyperparams_typed()
        assert "n_envs" not in result

    def test_unknown_param_inferred_as_int(self):
        """Unknown integer-like params should be inferred as int."""
        cfg = self._config({"custom_param": "42"})
        result = cfg.get_hyperparams_typed()
        assert result["custom_param"] == 42
        assert isinstance(result["custom_param"], int)

    def test_unknown_param_inferred_as_float(self):
        """Unknown float-like params should be inferred as float."""
        cfg = self._config({"custom_param": "3.14"})
        result = cfg.get_hyperparams_typed()
        assert result["custom_param"] == pytest.approx(3.14)
        assert isinstance(result["custom_param"], float)

    def test_unknown_param_kept_as_string(self):
        """Unknown non-numeric params stay as string."""
        cfg = self._config({"custom_param": "some_value"})
        result = cfg.get_hyperparams_typed()
        assert result["custom_param"] == "some_value"

    def test_error_message_includes_algorithm(self):
        """Error messages must include the algorithm name for AI agent debugging."""
        cfg = self._config({"learning_rate": "bad"})
        with pytest.raises(ValueError, match="DQN"):
            cfg.get_hyperparams_typed()
