from google.protobuf.internal import containers as _containers
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class TrainingStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    STARTED: _ClassVar[TrainingStatus]
    FAILED: _ClassVar[TrainingStatus]
STARTED: TrainingStatus
FAILED: TrainingStatus

class EnvironmentSpec(_message.Message):
    __slots__ = ("observation_dim", "action_dim", "sight_range", "observation_feature_names")
    OBSERVATION_DIM_FIELD_NUMBER: _ClassVar[int]
    ACTION_DIM_FIELD_NUMBER: _ClassVar[int]
    SIGHT_RANGE_FIELD_NUMBER: _ClassVar[int]
    OBSERVATION_FEATURE_NAMES_FIELD_NUMBER: _ClassVar[int]
    observation_dim: int
    action_dim: int
    sight_range: int
    observation_feature_names: _containers.RepeatedScalarFieldContainer[str]
    def __init__(self, observation_dim: _Optional[int] = ..., action_dim: _Optional[int] = ..., sight_range: _Optional[int] = ..., observation_feature_names: _Optional[_Iterable[str]] = ...) -> None: ...

class NegotiateEnvironmentRequest(_message.Message):
    __slots__ = ("experiment_id", "spec")
    EXPERIMENT_ID_FIELD_NUMBER: _ClassVar[int]
    SPEC_FIELD_NUMBER: _ClassVar[int]
    experiment_id: str
    spec: EnvironmentSpec
    def __init__(self, experiment_id: _Optional[str] = ..., spec: _Optional[_Union[EnvironmentSpec, _Mapping]] = ...) -> None: ...

class NegotiateEnvironmentResponse(_message.Message):
    __slots__ = ("accepted", "message", "echoed_spec")
    ACCEPTED_FIELD_NUMBER: _ClassVar[int]
    MESSAGE_FIELD_NUMBER: _ClassVar[int]
    ECHOED_SPEC_FIELD_NUMBER: _ClassVar[int]
    accepted: bool
    message: str
    echoed_spec: EnvironmentSpec
    def __init__(self, accepted: bool = ..., message: _Optional[str] = ..., echoed_spec: _Optional[_Union[EnvironmentSpec, _Mapping]] = ...) -> None: ...

class TrainingRequest(_message.Message):
    __slots__ = ("experiment_id", "total_timesteps", "seed", "hyperparameters", "model_output_path")
    class HyperparametersEntry(_message.Message):
        __slots__ = ("key", "value")
        KEY_FIELD_NUMBER: _ClassVar[int]
        VALUE_FIELD_NUMBER: _ClassVar[int]
        key: str
        value: str
        def __init__(self, key: _Optional[str] = ..., value: _Optional[str] = ...) -> None: ...
    EXPERIMENT_ID_FIELD_NUMBER: _ClassVar[int]
    TOTAL_TIMESTEPS_FIELD_NUMBER: _ClassVar[int]
    SEED_FIELD_NUMBER: _ClassVar[int]
    HYPERPARAMETERS_FIELD_NUMBER: _ClassVar[int]
    MODEL_OUTPUT_PATH_FIELD_NUMBER: _ClassVar[int]
    experiment_id: str
    total_timesteps: int
    seed: int
    hyperparameters: _containers.ScalarMap[str, str]
    model_output_path: str
    def __init__(self, experiment_id: _Optional[str] = ..., total_timesteps: _Optional[int] = ..., seed: _Optional[int] = ..., hyperparameters: _Optional[_Mapping[str, str]] = ..., model_output_path: _Optional[str] = ...) -> None: ...

class TrainingResponse(_message.Message):
    __slots__ = ("status", "message", "run_id")
    STATUS_FIELD_NUMBER: _ClassVar[int]
    MESSAGE_FIELD_NUMBER: _ClassVar[int]
    RUN_ID_FIELD_NUMBER: _ClassVar[int]
    status: TrainingStatus
    message: str
    run_id: str
    def __init__(self, status: _Optional[_Union[TrainingStatus, str]] = ..., message: _Optional[str] = ..., run_id: _Optional[str] = ...) -> None: ...

class StatusRequest(_message.Message):
    __slots__ = ("run_id",)
    RUN_ID_FIELD_NUMBER: _ClassVar[int]
    run_id: str
    def __init__(self, run_id: _Optional[str] = ...) -> None: ...

class StatusResponse(_message.Message):
    __slots__ = ("timesteps_done", "is_done", "last_checkpoint_path", "error_message", "total_timesteps", "num_envs")
    TIMESTEPS_DONE_FIELD_NUMBER: _ClassVar[int]
    IS_DONE_FIELD_NUMBER: _ClassVar[int]
    LAST_CHECKPOINT_PATH_FIELD_NUMBER: _ClassVar[int]
    ERROR_MESSAGE_FIELD_NUMBER: _ClassVar[int]
    TOTAL_TIMESTEPS_FIELD_NUMBER: _ClassVar[int]
    NUM_ENVS_FIELD_NUMBER: _ClassVar[int]
    timesteps_done: int
    is_done: bool
    last_checkpoint_path: str
    error_message: str
    total_timesteps: int
    num_envs: int
    def __init__(self, timesteps_done: _Optional[int] = ..., is_done: bool = ..., last_checkpoint_path: _Optional[str] = ..., error_message: _Optional[str] = ..., total_timesteps: _Optional[int] = ..., num_envs: _Optional[int] = ...) -> None: ...

class ActRequest(_message.Message):
    __slots__ = ("run_id", "observation", "algorithm_type")
    RUN_ID_FIELD_NUMBER: _ClassVar[int]
    OBSERVATION_FIELD_NUMBER: _ClassVar[int]
    ALGORITHM_TYPE_FIELD_NUMBER: _ClassVar[int]
    run_id: str
    observation: _containers.RepeatedScalarFieldContainer[float]
    algorithm_type: str
    def __init__(self, run_id: _Optional[str] = ..., observation: _Optional[_Iterable[float]] = ..., algorithm_type: _Optional[str] = ...) -> None: ...

class ActResponse(_message.Message):
    __slots__ = ("action", "success", "error_message")
    ACTION_FIELD_NUMBER: _ClassVar[int]
    SUCCESS_FIELD_NUMBER: _ClassVar[int]
    ERROR_MESSAGE_FIELD_NUMBER: _ClassVar[int]
    action: int
    success: bool
    error_message: str
    def __init__(self, action: _Optional[int] = ..., success: bool = ..., error_message: _Optional[str] = ...) -> None: ...
