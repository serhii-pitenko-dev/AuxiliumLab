"""Unit tests for the NegotiateEnvironment gRPC handler – max_steps synchronisation."""
import pytest
from unittest.mock import MagicMock

from generated import policy_trainer_pb2
from auxilium_rl.infra.config import EnvConfig
from auxilium_rl.core.training import TrainingOrchestrator
from auxilium_rl.transport.trainer_servicer import PolicyTrainerServicer


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_spec(sight_range: int = 5, max_steps: int = 0) -> policy_trainer_pb2.EnvironmentSpec:
    """Build a valid EnvironmentSpec with the correct obs_dim formula."""
    grid = 2 * sight_range + 1
    obs_dim = 5 + grid * grid  # 5 scalar features + grid²
    return policy_trainer_pb2.EnvironmentSpec(
        observation_dim=obs_dim,
        action_dim=5,
        sight_range=sight_range,
        max_steps=max_steps,
    )


def _make_request(
    experiment_id: str = "exp_test",
    sight_range: int = 5,
    max_steps: int = 0,
) -> policy_trainer_pb2.NegotiateEnvironmentRequest:
    return policy_trainer_pb2.NegotiateEnvironmentRequest(
        experiment_id=experiment_id,
        spec=_make_spec(sight_range=sight_range, max_steps=max_steps),
    )


@pytest.fixture
def servicer() -> PolicyTrainerServicer:
    """Create a servicer backed by a real EnvConfig (default max_steps=500)."""
    env_config = EnvConfig(observation_dim=126, action_dim=5, max_steps=500)
    orchestrator = MagicMock(spec=TrainingOrchestrator)
    orchestrator.env_config = env_config
    return PolicyTrainerServicer(orchestrator=orchestrator)


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------


class TestNegotiateEnvironmentMaxSteps:
    """Verify that NegotiateEnvironment propagates max_steps to env_config."""

    def test_max_steps_applied_when_positive(self, servicer: PolicyTrainerServicer):
        """A positive max_steps in the spec must override the env_config default."""
        response = servicer.NegotiateEnvironment(
            _make_request(max_steps=1200), context=None
        )

        assert response.accepted
        assert servicer.orchestrator.env_config.max_steps == 1200

    def test_max_steps_default_preserved_when_zero(self, servicer: PolicyTrainerServicer):
        """max_steps == 0 (proto default) must keep the existing env_config value."""
        response = servicer.NegotiateEnvironment(
            _make_request(max_steps=0), context=None
        )

        assert response.accepted
        assert servicer.orchestrator.env_config.max_steps == 500  # unchanged default

    def test_max_steps_echoed_in_response(self, servicer: PolicyTrainerServicer):
        """The echoed spec must contain the max_steps value from the request."""
        response = servicer.NegotiateEnvironment(
            _make_request(max_steps=750), context=None
        )

        assert response.echoed_spec.max_steps == 750

    def test_max_steps_logged_in_message(self, servicer: PolicyTrainerServicer):
        """The acceptance message must mention the negotiated max_steps."""
        response = servicer.NegotiateEnvironment(
            _make_request(max_steps=300), context=None
        )

        assert "max_steps=300" in response.message

    def test_different_experiments_update_config(self, servicer: PolicyTrainerServicer):
        """Successive negotiations must update env_config to the latest max_steps."""
        servicer.NegotiateEnvironment(
            _make_request(experiment_id="exp_1", max_steps=100), context=None
        )
        assert servicer.orchestrator.env_config.max_steps == 100

        servicer.NegotiateEnvironment(
            _make_request(experiment_id="exp_2", max_steps=2000), context=None
        )
        assert servicer.orchestrator.env_config.max_steps == 2000

    def test_obs_dim_and_action_dim_still_applied(self, servicer: PolicyTrainerServicer):
        """max_steps logic must not break existing obs_dim / action_dim propagation."""
        response = servicer.NegotiateEnvironment(
            _make_request(sight_range=3, max_steps=600), context=None
        )

        assert response.accepted
        grid = 2 * 3 + 1
        expected_obs = 5 + grid * grid  # 54
        assert servicer.orchestrator.env_config.observation_dim == expected_obs
        assert servicer.orchestrator.env_config.action_dim == 5
        assert servicer.orchestrator.env_config.max_steps == 600
