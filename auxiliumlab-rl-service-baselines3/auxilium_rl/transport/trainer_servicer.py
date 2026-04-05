"""gRPC servicer implementation for the Policy Trainer service."""
import logging
from typing import Optional, Callable
import numpy as np

# Import generated gRPC code
from generated import policy_trainer_pb2, policy_trainer_pb2_grpc

from ..core.dto import TrainingConfig, AlgorithmType, RunStatus
from ..core.training import TrainingOrchestrator
from ..infra.external_env_adapter import ExternalEnvAdapter

logger = logging.getLogger(__name__)


class PolicyTrainerServicer(policy_trainer_pb2_grpc.PolicyTrainerServiceServicer):
    """Implementation of the PolicyTrainerService."""

    def __init__(
        self,
        orchestrator: TrainingOrchestrator,
        adapter_factory: Optional[Callable[[str], ExternalEnvAdapter]] = None
    ):
        """
        Initialize the servicer.
        
        Args:
            orchestrator: Training orchestrator
            adapter_factory: Optional factory function (gym_id: str) -> ExternalEnvAdapter.
                             Receives the gym UUID string from the .NET side so gRPC
                             requests are routed to the correct Sb3Actions instance.
        """
        self.orchestrator = orchestrator
        self.adapter_factory = adapter_factory
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
        try:
            # Validate request
            if not request.experiment_id:
                return policy_trainer_pb2.TrainingResponse(
                    status=policy_trainer_pb2.FAILED,
                    message="experiment_id is required",
                    run_id=""
                )
            
            if request.total_timesteps <= 0:
                return policy_trainer_pb2.TrainingResponse(
                    status=policy_trainer_pb2.FAILED,
                    message="total_timesteps must be positive",
                    run_id=""
                )

            # Ensure NegotiateEnvironment was called before training.
            ec = self.orchestrator.env_config
            if ec.observation_dim is None or ec.action_dim is None or ec.max_steps is None:
                return policy_trainer_pb2.TrainingResponse(
                    status=policy_trainer_pb2.FAILED,
                    message="NegotiateEnvironment must be called before StartTraining",
                    run_id=""
                )

            # Create training config
            config = TrainingConfig(
                algorithm=algorithm,
                experiment_id=request.experiment_id,
                total_timesteps=request.total_timesteps,
                seed=request.seed,
                hyperparameters=dict(request.hyperparameters),
                model_output_path=request.model_output_path
            )
            
            # Create adapter if factory is provided.
            # Extract the first gym_id from hyperparameters and pass it to the factory
            # so gRPC Reset/Step/Close calls carry the correct GymId recognised by .NET.
            adapter = None
            if self.adapter_factory:
                gym_ids_str = config.hyperparameters.get("gym_ids", "")
                gym_ids = [g for g in gym_ids_str.split(";") if g]
                gym_id = gym_ids[0] if gym_ids else ""
                if not gym_id:
                    logger.warning(
                        "No gym_ids found in hyperparameters. "
                        "gRPC calls will use an empty gym_id and .NET will ignore them. "
                        "Ensure the .NET orchestrator sends 'gym_ids' in hyperparameters."
                    )
                adapter = self.adapter_factory(gym_id)
                logger.info(f"Created adapter for gym_id={gym_id}")
            
            # Start training
            run_id = self.orchestrator.start_training(config, adapter)
            
            logger.info(
                f"Training started: run_id={run_id}, algorithm={algorithm.value}, "
                f"experiment={request.experiment_id}, timesteps={request.total_timesteps}"
            )
            
            return policy_trainer_pb2.TrainingResponse(
                status=policy_trainer_pb2.STARTED,
                message=f"Training started successfully for {algorithm.value.upper()}",
                run_id=run_id
            )
            
        except Exception as e:
            logger.error(f"Failed to start training: {e}", exc_info=True)
            return policy_trainer_pb2.TrainingResponse(
                status=policy_trainer_pb2.FAILED,
                message=f"Failed to start training: {str(e)}",
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
