"""gRPC servicer implementation for the Policy Trainer service."""
import logging
from typing import Optional, Callable
import numpy as np

# Import generated gRPC code
from generated import policy_trainer_pb2, policy_trainer_pb2_grpc

from ..core.dto import TrainingConfig, AlgorithmType, RunStatus
from ..core.training import TrainingOrchestrator

logger = logging.getLogger(__name__)


class PolicyTrainerServicer(policy_trainer_pb2_grpc.PolicyTrainerServiceServicer):
    """Implementation of the PolicyTrainerService."""

    def __init__(self, orchestrator: TrainingOrchestrator):
        """
        Initialize the servicer.
        
        Args:
            orchestrator: Training orchestrator (owns adapter creation)
        """
        self.orchestrator = orchestrator
        # Per-experiment specs stored by NegotiateEnvironment, keyed by experiment_id.
        self._experiment_specs: dict = {}

    def NegotiateEnvironment(
        self,
        request: policy_trainer_pb2.NegotiateEnvironmentRequest,
        context
    ) -> policy_trainer_pb2.NegotiateEnvironmentResponse:
        """Validate and store the environment spec sent by the caller before training starts.

        Performs only generic validation (positive dimensions). Domain-specific
        observation formulas are the caller's responsibility.
        """
        experiment_id = request.experiment_id
        spec = request.spec

        # Generic validation — no domain-specific formula checks.
        if spec.observation_dim <= 0:
            msg = f"observation_dim must be positive, got {spec.observation_dim}"
            logger.error(msg)
            return policy_trainer_pb2.NegotiateEnvironmentResponse(
                accepted=False, message=msg, echoed_spec=spec)

        if spec.action_dim <= 0:
            msg = f"action_dim must be positive, got {spec.action_dim}"
            logger.error(msg)
            return policy_trainer_pb2.NegotiateEnvironmentResponse(
                accepted=False, message=msg, echoed_spec=spec)

        if spec.max_steps <= 0:
            msg = f"max_steps must be positive, got {spec.max_steps}"
            logger.error(msg)
            return policy_trainer_pb2.NegotiateEnvironmentResponse(
                accepted=False, message=msg, echoed_spec=spec)

        # Store the spec and update the orchestrator's env_config for this experiment.
        self._experiment_specs[experiment_id] = spec
        self.orchestrator.env_config.observation_dim = spec.observation_dim
        self.orchestrator.env_config.action_dim = spec.action_dim
        self.orchestrator.env_config.max_steps = spec.max_steps

        msg = (
            f"Environment spec accepted for experiment '{experiment_id}': "
            f"obs_dim={spec.observation_dim}, action_dim={spec.action_dim}, "
            f"max_steps={spec.max_steps}."
        )
        logger.info(msg)

        return policy_trainer_pb2.NegotiateEnvironmentResponse(
            accepted=True,
            message=msg,
            echoed_spec=spec,
        )
    
    def StartTrainingPPO(
        self,
        request: policy_trainer_pb2.TrainingRequest,
        context
    ) -> policy_trainer_pb2.TrainingResponse:
        """Start training a PPO model."""
        return self._start_training(request, AlgorithmType.PPO)
    
    def StartTrainingA2C(
        self,
        request: policy_trainer_pb2.TrainingRequest,
        context
    ) -> policy_trainer_pb2.TrainingResponse:
        """Start training an A2C model."""
        return self._start_training(request, AlgorithmType.A2C)
    
    def StartTrainingDQN(
        self,
        request: policy_trainer_pb2.TrainingRequest,
        context
    ) -> policy_trainer_pb2.TrainingResponse:
        """Start training a DQN model."""
        return self._start_training(request, AlgorithmType.DQN)
    
    def _start_training(
        self,
        request: policy_trainer_pb2.TrainingRequest,
        algorithm: AlgorithmType
    ) -> policy_trainer_pb2.TrainingResponse:
        """Common training start logic."""
        experiment_id = request.experiment_id
        algo_name = algorithm.value.upper()

        try:
            # ── Validate request ─────────────────────────────────────────
            if not experiment_id:
                return policy_trainer_pb2.TrainingResponse(
                    status=policy_trainer_pb2.FAILED,
                    message=(
                        f"[{algo_name}] experiment_id is required. "
                        "The .NET caller must set experiment_id on the TrainingRequest."
                    ),
                    run_id=""
                )
            
            if request.total_timesteps <= 0:
                return policy_trainer_pb2.TrainingResponse(
                    status=policy_trainer_pb2.FAILED,
                    message=(
                        f"[{algo_name}] total_timesteps must be positive, "
                        f"got {request.total_timesteps} for experiment '{experiment_id}'."
                    ),
                    run_id=""
                )

            # ── Resolve environment dimensions ───────────────────────────
            # Per-experiment spec takes priority (set by NegotiateEnvironment).
            # Falls back to shared env_config for backward compat / testing.
            spec = self._experiment_specs.get(experiment_id)
            if spec:
                obs_dim = spec.observation_dim
                act_dim = spec.action_dim
                max_steps = spec.max_steps
            else:
                ec = self.orchestrator.env_config
                if ec.observation_dim is None or ec.action_dim is None or ec.max_steps is None:
                    return policy_trainer_pb2.TrainingResponse(
                        status=policy_trainer_pb2.FAILED,
                        message=(
                            f"[{algo_name}] NegotiateEnvironment must be called before "
                            f"StartTraining for experiment '{experiment_id}'. "
                            f"No environment spec found and env_config is unset."
                        ),
                        run_id=""
                    )
                obs_dim = ec.observation_dim
                act_dim = ec.action_dim
                max_steps = ec.max_steps

            # ── Build training config ────────────────────────────────────
            config = TrainingConfig(
                algorithm=algorithm,
                experiment_id=experiment_id,
                total_timesteps=request.total_timesteps,
                seed=request.seed,
                hyperparameters=dict(request.hyperparameters),
                model_output_path=request.model_output_path,
                observation_dim=obs_dim,
                action_dim=act_dim,
                max_steps=max_steps,
            )
            
            # Start training (orchestrator creates adapters from its factory)
            run_id = self.orchestrator.start_training(config)
            
            logger.info(
                f"Training started: run_id={run_id}, algorithm={algo_name}, "
                f"experiment={experiment_id}, timesteps={request.total_timesteps}, "
                f"obs_dim={obs_dim}, act_dim={act_dim}, max_steps={max_steps}"
            )
            
            return policy_trainer_pb2.TrainingResponse(
                status=policy_trainer_pb2.STARTED,
                message=f"Training started successfully for {algo_name}",
                run_id=run_id
            )
            
        except Exception as e:
            logger.error(
                f"[{algo_name}] Failed to start training for experiment "
                f"'{experiment_id}': {e}",
                exc_info=True
            )
            return policy_trainer_pb2.TrainingResponse(
                status=policy_trainer_pb2.FAILED,
                message=(
                    f"[{algo_name}] Failed to start training for experiment "
                    f"'{experiment_id}': {e}"
                ),
                run_id=""
            )
    
    def GetTrainingStatus(
        self,
        request: policy_trainer_pb2.StatusRequest,
        context
    ) -> policy_trainer_pb2.StatusResponse:
        """Get the status of a training run."""
        run_info = self.orchestrator.get_run_info(request.run_id)
        
        if not run_info:
            return policy_trainer_pb2.StatusResponse(
                timesteps_done=0,
                is_done=False,
                last_checkpoint_path="",
                error_message=f"Run {request.run_id} not found"
            )
        
        is_done = run_info.status in [RunStatus.COMPLETED, RunStatus.FAILED]
        
        return policy_trainer_pb2.StatusResponse(
            timesteps_done=run_info.timesteps_done,
            is_done=is_done,
            last_checkpoint_path=run_info.last_checkpoint_path or "",
            error_message=run_info.error_message or "",
            total_timesteps=run_info.total_timesteps,
            num_envs=run_info.num_envs
        )
    
    def Act(
        self,
        request: policy_trainer_pb2.ActRequest,
        context
    ) -> policy_trainer_pb2.ActResponse:
        """Perform inference with a trained model."""
        try:
            # Convert observation to numpy array
            observation = np.array(request.observation, dtype=np.float32)

            # Forward the optional algorithm_type hint so the orchestrator can
            # load by-path models without guessing from the filename.
            algorithm_hint = request.algorithm_type or ""
            
            # Get prediction
            action = self.orchestrator.predict(request.run_id, observation, algorithm_hint)
            
            if action is None:
                logger.warning(
                    f"Act failed: model not available for run_id={request.run_id!r}, "
                    f"algorithm_type={algorithm_hint!r}"
                )
                return policy_trainer_pb2.ActResponse(
                    action=0,
                    success=False,
                    error_message=f"Model not available for run {request.run_id}"
                )
            
            return policy_trainer_pb2.ActResponse(
                action=action,
                success=True,
                error_message=""
            )
            
        except Exception as e:
            logger.error(f"Failed to perform inference: {e}", exc_info=True)
            return policy_trainer_pb2.ActResponse(
                action=0,
                success=False,
                error_message=str(e)
            )
