// Global type aliases so existing WebApi code continues to compile after API
// contract types were moved to AuxiliumLab.AiSandbox.SharedContracts.

global using ModelType   = AuxiliumLab.AiSandbox.SharedContracts.ModelType;
global using AiPolicy    = AuxiliumLab.AiSandbox.SharedContracts.AiPolicy;

// Training
global using StartPpoTrainingCommand    = AuxiliumLab.AiSandbox.SharedContracts.StartPpoTrainingCommand;
global using TrainingJobStartedDto      = AuxiliumLab.AiSandbox.SharedContracts.TrainingJobStartedDto;
global using TrainingJobState           = AuxiliumLab.AiSandbox.SharedContracts.TrainingJobState;
global using TrainingJobStatusDto       = AuxiliumLab.AiSandbox.SharedContracts.TrainingJobStatusDto;
global using TrainedModelInfoDto        = AuxiliumLab.AiSandbox.SharedContracts.TrainedModelInfoDto;
global using TrainingPreconditionsDto   = AuxiliumLab.AiSandbox.SharedContracts.TrainingPreconditionsDto;

// Simulation
global using SimulationKind               = AuxiliumLab.AiSandbox.SharedContracts.SimulationKind;
global using StartSingleSimulationCommand = AuxiliumLab.AiSandbox.SharedContracts.StartSingleSimulationCommand;
global using StartMassSimulationCommand   = AuxiliumLab.AiSandbox.SharedContracts.StartMassSimulationCommand;
global using SimulationJobStartedDto      = AuxiliumLab.AiSandbox.SharedContracts.SimulationJobStartedDto;
global using SimulationJobStatusDto       = AuxiliumLab.AiSandbox.SharedContracts.SimulationJobStatusDto;
global using SandboxStatus                = AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects.SandboxStatus;
global using SandboxDefaultsDto           = AuxiliumLab.AiSandbox.SharedContracts.SandboxDefaultsDto;

// AggregationRun
global using StartAggregationCommand  = AuxiliumLab.AiSandbox.SharedContracts.StartAggregationCommand;
global using AggregationJobStartedDto = AuxiliumLab.AiSandbox.SharedContracts.AggregationJobStartedDto;
global using AggregationJobState      = AuxiliumLab.AiSandbox.SharedContracts.AggregationJobState;
global using AggregationJobStatusDto  = AuxiliumLab.AiSandbox.SharedContracts.AggregationJobStatusDto;

// Statistics
global using CompletedSimulationRunDto  = AuxiliumLab.AiSandbox.SharedContracts.CompletedSimulationRunDto;
global using CompletedAggregationRunDto = AuxiliumLab.AiSandbox.SharedContracts.CompletedAggregationRunDto;
global using AggregationStepResultDto   = AuxiliumLab.AiSandbox.SharedContracts.AggregationStepResultDto;

// SignalR hub events
global using SimulationCellDto    = AuxiliumLab.AiSandbox.SharedContracts.SimulationCellDto;
global using AgentEffectDto       = AuxiliumLab.AiSandbox.SharedContracts.AgentEffectDto;
global using AgentSnapshotDto     = AuxiliumLab.AiSandbox.SharedContracts.AgentSnapshotDto;
global using InitialAgentDto      = AuxiliumLab.AiSandbox.SharedContracts.InitialAgentDto;
global using SimulationStartedDto = AuxiliumLab.AiSandbox.SharedContracts.SimulationStartedDto;
global using AgentMovedDto        = AuxiliumLab.AiSandbox.SharedContracts.AgentMovedDto;
global using AgentToggledDto      = AuxiliumLab.AiSandbox.SharedContracts.AgentToggledDto;
global using TurnCompletedDto     = AuxiliumLab.AiSandbox.SharedContracts.TurnCompletedDto;
global using SimulationEndedDto   = AuxiliumLab.AiSandbox.SharedContracts.SimulationEndedDto;
global using EEffect              = AuxiliumLab.AiSandbox.SharedContracts.EEffect;
global using ObjectType            = AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects.ObjectType;
