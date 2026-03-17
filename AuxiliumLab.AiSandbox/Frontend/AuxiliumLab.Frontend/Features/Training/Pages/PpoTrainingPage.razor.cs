using AuxiliumLab.Frontend.Features.Training.Dto;

namespace AuxiliumLab.Frontend.Features.Training.Pages;

public partial class PpoTrainingPage
{
    private StartPpoTrainingCommand _cmd = new()
    {
        Hyperparameters = new PpoHyperparametersDto
        {
            TotalTimesteps = 100_000,
            LearningRate   = 0.0003,
            NSteps         = 2048,
            BatchSize      = 64,
            NEpochs        = 10,
            Gamma          = 0.99,
            GaeLambda      = 0.95,
            ClipRange      = 0.2,
            EntCoef        = 0.0,
            NEnvs          = 4
        },
        SandboxSettings = new TrainingSandboxSettingsDto(),
        RewardSettings  = new RewardSettingsDto
        {
            StepPenalty = -0.001f,
            WinReward   = 1.0f,
            LossReward  = -1.0f
        }
    };

    private bool _loading;
    private bool _sandboxExpanded;
    private TrainingJobStartedDto? _started;
    private string? _error;

    private async Task StartTraining()
    {
        _loading = true;
        _error   = null;
        _started = null;

        try
        {
            _started = await TrainingApi.StartPpoTrainingAsync(_cmd);
            Notifications.Notify($"Training started: {_started?.ExperimentId}");
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }
}
