"""Unit tests for the NegotiateEnvironment gRPC handler."""
import pytest
from unittest.mock import MagicMock

from generated import policy_trainer_pb2
from auxilium_rl.infra.config import EnvConfig
from auxilium_rl.core.training import TrainingOrchestrator
from auxilium_rl.transport.trainer_servicer import PolicyTrainerServicer


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_spec(
    observation_dim: int = 10,
    action_dim: int = 4,
    sight_range: int = 3,
    max_steps: int = 500,
) -> policy_trainer_pb2.EnvironmentSpec:
    """Build an EnvironmentSpec with the given dimensions."""
    return policy_trainer_pb2.EnvironmentSpec(
        observation_dim=observation_dim,
        action_dim=action_dim,
        sight_range=sight_range,
        max_steps=max_steps,
    )


def _make_request(
    experiment_id: str = "exp_test",
    observation_dim: int = 10,
    action_dim: int = 4,
    sight_range: int = 3,
    max_steps: int = 500,
) -> policy_trainer_pb2.NegotiateEnvironmentRequest:
    return policy_trainer_pb2.NegotiateEnvironmentRequest(
        experiment_id=experiment_id,
        spec=_make_spec(
            observation_dim=observation_dim,
            action_dim=action_dim,
            sight_range=sight_range,
            max_steps=max_steps,
        ),
    )


@pytest.fixture
def servicer() -> PolicyTrainerServicer:
    """Create a servicer backed by a bare EnvConfig (all fields None)."""
    env_config = EnvConfig()
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

    def test_max_steps_zero_rejected(self, servicer: PolicyTrainerServicer):
        """max_steps == 0 must be rejected (no implicit defaults)."""
        response = servicer.NegotiateEnvironment(
            _make_request(max_steps=0), context=None
        )

        assert not response.accepted
        assert "max_steps must be positive" in response.message

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
            _make_request(observation_dim=54, action_dim=6, max_steps=600), context=None
        )

        assert response.accepted
        assert servicer.orchestrator.env_config.observation_dim == 54
        assert servicer.orchestrator.env_config.action_dim == 6
        assert servicer.orchestrator.env_config.max_steps == 600


class TestNegotiateEnvironmentValidation:
    """Verify generic validation rejects invalid specs."""

    def test_zero_observation_dim_rejected(self, servicer: PolicyTrainerServicer):
        response = servicer.NegotiateEnvironment(
            _make_request(observation_dim=0), context=None
        )
        assert not response.accepted
        assert "observation_dim must be positive" in response.message

    def test_negative_observation_dim_rejected(self, servicer: PolicyTrainerServicer):
        response = servicer.NegotiateEnvironment(
            _make_request(observation_dim=-1), context=None
        )
        assert not response.accepted

    def test_zero_action_dim_rejected(self, servicer: PolicyTrainerServicer):
        response = servicer.NegotiateEnvironment(
            _make_request(action_dim=0), context=None
        )
        assert not response.accepted
        assert "action_dim must be positive" in response.message

    def test_negative_action_dim_rejected(self, servicer: PolicyTrainerServicer):
        response = servicer.NegotiateEnvironment(
            _make_request(action_dim=-5), context=None
        )
        assert not response.accepted

    def test_zero_max_steps_rejected(self, servicer: PolicyTrainerServicer):
        response = servicer.NegotiateEnvironment(
            _make_request(max_steps=0), context=None
        )
        assert not response.accepted
        assert "max_steps must be positive" in response.message

    def test_negative_max_steps_rejected(self, servicer: PolicyTrainerServicer):
        response = servicer.NegotiateEnvironment(
            _make_request(max_steps=-10), context=None
        )
        assert not response.accepted
