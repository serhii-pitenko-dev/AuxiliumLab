
namespace AuxiliumLab.Frontend.Features.Training.Pages;

public partial class PpoTrainingPage
{
    private StartPpoTrainingCommand _cmd = new();

    private bool _loading;
    private bool _sandboxExpanded;
    private TrainingJobStartedDto? _started;
    private string? _error;

    protected override void OnInitialized()
    {
        var sb = SandboxConfig.Value;

        _cmd = new StartPpoTrainingCommand
        {
            Hyperparameters = new PpoHyperparametersDto
            {
                TotalTimesteps = 100_000,
                LearningRate   = 0.0003,
                NSteps         = 256,
                BatchSize      = 64,
                NEpochs        = 5,
                Gamma          = 0.90,
                GaeLambda      = 0.95,
                ClipRange      = 0.2,
                EntCoef        = 0.1,
                Seed           = 42,
                NEnvs          = 4
            },
            SandboxSettings = new TrainingSandboxSettingsDto
            {
                MaxTurns       = sb.MaxTurns.Current,
                MapWidth       = sb.MapSettings.Size.Width.Current,
                MapHeight      = sb.MapSettings.Size.Height.Current,
                BlocksPercent  = sb.MapSettings.ElementsPercentages.BlocksPercent.Current,
                EnemiesPercent = sb.MapSettings.ElementsPercentages.PercentOfEnemies.Current,
                HeroSpeed      = sb.Hero.Speed.Current,
                HeroSightRange = sb.Hero.SightRange.Current,
                HeroStamina    = sb.Hero.Stamina.Current,
                EnemySpeed       = sb.Enemy.Speed.Current,
                EnemySightRange  = sb.Enemy.SightRange.Current,
                EnemyStamina     = sb.Enemy.Stamina.Current
            },
            RewardSettings = new RewardSettingsDto
            {
                StepPenalty = -0.1f,
                WinReward   = 10.0f,
                LossReward  = -10.0f
            }
        };
    }

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
