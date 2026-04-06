// All API contract types have been moved to AuxiliumLab.AiSandbox.SharedContracts.
// These global aliases maintain backward compatibility for existing code that
// references the type names without a fully-qualified namespace.

global using ModelType   = AuxiliumLab.AiSandbox.SharedContracts.ModelType;
global using AiPolicy    = AuxiliumLab.AiSandbox.SharedContracts.AiPolicy;

// Training
global using PpoHyperparametersDto     = AuxiliumLab.AiSandbox.SharedContracts.PpoHyperparametersDto;
global using TrainingSandboxSettingsDto = AuxiliumLab.AiSandbox.SharedContracts.TrainingSandboxSettingsDto;
global using RewardSettingsDto          = AuxiliumLab.AiSandbox.SharedContracts.RewardSettingsDto;
global using StartPpoTrainingCommand    = AuxiliumLab.AiSandbox.SharedContracts.StartPpoTrainingCommand;
global using StartGenericTrainingCommand = AuxiliumLab.AiSandbox.SharedContracts.StartGenericTrainingCommand;
global using TrainingJobStartedDto      = AuxiliumLab.AiSandbox.SharedContracts.TrainingJobStartedDto;
global using TrainingJobState           = AuxiliumLab.AiSandbox.SharedContracts.TrainingJobState;
global using TrainingJobStatusDto       = AuxiliumLab.AiSandbox.SharedContracts.TrainingJobStatusDto;
global using TrainedModelInfoDto        = AuxiliumLab.AiSandbox.SharedContracts.TrainedModelInfoDto;
global using TrainingPreconditionsDto   = AuxiliumLab.AiSandbox.SharedContracts.TrainingPreconditionsDto;
global using TraineeAgentType           = AuxiliumLab.AiSandbox.SharedContracts.TraineeAgentType;
global using AgentAiConfigDto           = AuxiliumLab.AiSandbox.SharedContracts.AgentAiConfigDto;

// Simulation
global using SimulationKind              = AuxiliumLab.AiSandbox.SharedContracts.SimulationKind;
global using SimulationSandboxOverrideDto = AuxiliumLab.AiSandbox.SharedContracts.SimulationSandboxOverrideDto;
global using StartSingleSimulationCommand = AuxiliumLab.AiSandbox.SharedContracts.StartSingleSimulationCommand;
global using StartMassSimulationCommand   = AuxiliumLab.AiSandbox.SharedContracts.StartMassSimulationCommand;
global using IncrementalSweeperDto        = AuxiliumLab.AiSandbox.SharedContracts.IncrementalSweeperDto;
global using SimulationJobStartedDto      = AuxiliumLab.AiSandbox.SharedContracts.SimulationJobStartedDto;
global using SimulationJobStatusDto       = AuxiliumLab.AiSandbox.SharedContracts.SimulationJobStatusDto;
global using SandboxStatus                = AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects.SandboxStatus;
global using SandboxDefaultsDto           = AuxiliumLab.AiSandbox.SharedContracts.SandboxDefaultsDto;

// AggregationRun
global using AggregationStepDto               = AuxiliumLab.AiSandbox.SharedContracts.AggregationStepDto;
global using AggregationIncrementalSweeperDto = AuxiliumLab.AiSandbox.SharedContracts.AggregationIncrementalSweeperDto;
global using StartAggregationCommand          = AuxiliumLab.AiSandbox.SharedContracts.StartAggregationCommand;
global using AggregationJobStartedDto         = AuxiliumLab.AiSandbox.SharedContracts.AggregationJobStartedDto;
global using AggregationJobState              = AuxiliumLab.AiSandbox.SharedContracts.AggregationJobState;
global using AggregationJobStatusDto          = AuxiliumLab.AiSandbox.SharedContracts.AggregationJobStatusDto;

// Statistics
global using CompletedSimulationRunDto  = AuxiliumLab.AiSandbox.SharedContracts.CompletedSimulationRunDto;
global using CompletedAggregationRunDto = AuxiliumLab.AiSandbox.SharedContracts.CompletedAggregationRunDto;
global using AggregationStepResultDto   = AuxiliumLab.AiSandbox.SharedContracts.AggregationStepResultDto;

// Map entities
global using EEffect        = AuxiliumLab.AiSandbox.SharedContracts.EEffect;
global using AgentEffectDto  = AuxiliumLab.AiSandbox.SharedContracts.AgentEffectDto;
