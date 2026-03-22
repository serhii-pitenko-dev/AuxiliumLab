
namespace AuxiliumLab.Frontend.Features.AggregationRun.Pages;

public partial class AggregationRunPage
{
    private StartAggregationCommand _cmd = new();

    private bool _loading;
    private AggregationJobStartedDto? _started;
    private string? _error;

    protected override void OnInitialized()
    {
        var sb = SandboxConfig.Value;

        _cmd = new StartAggregationCommand
        {
            StandardSimulationCount = 100,
            Algorithm               = ModelType.PPO,
            PolicyType              = AiPolicy.MLP,
            Steps =
            [
                new AggregationStepDto { Name = "Random AI", Mode = "MassRandomAISimulation" },
                new AggregationStepDto { Name = "PPO - AI",  Mode = "MassTrainedAISimulation" }
            ],
            TrainingOverrides = new StartPpoTrainingCommand
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
            }
        };
    }

    private void AddStep() => _cmd.Steps.Add(new AggregationStepDto());
    private void RemoveStep(int idx) => _cmd.Steps.RemoveAt(idx);

    private async Task StartAsync()
    {
        _loading = true;
        _error   = null;
        _started = null;

        try
        {
            _started = await AggregationApi.StartAggregationAsync(_cmd);
            Notifications.Notify($"Aggregation started: {_started?.JobId}");
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
