
namespace AuxiliumLab.Frontend.Features.Training.Pages;

public partial class PpoTrainingPage
{
    private StartPpoTrainingCommand _cmd = new();
    private List<TrainedModelInfoDto> _trainedModels = [];

    // ── Opponent AI grid rows ────────────────────────────────────────────────

    internal class OpponentAiRow
    {
        public string    Group        { get; init; } = string.Empty;
        public string    Name         { get; init; } = string.Empty;
        public DateTime? CreatedDate  { get; init; }
        public ModelType ModelType    { get; init; }
        public string?   ExperimentId { get; init; }
        public string?   AgentType    { get; init; }
    }

    internal List<OpponentAiRow> _opponentRows = [];
    private OpponentAiRow? _selectedOpponentRow;

    private bool _loading;
    private TrainingJobStartedDto? _started;
    private string? _error;

    protected override async Task OnInitializedAsync()
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
                WinReward   = 50.0f,
                LossReward  = -50.0f
            }
        };

        try
        {
            _trainedModels = await TrainingApi.GetTrainedModelsAsync() ?? [];
        }
        catch
        {
            _trainedModels = [];
        }

        BuildOpponentRows();
    }

    private void BuildOpponentRows()
    {
        var oppositeAgentType = _cmd.TraineeAgent == TraineeAgentType.Hero ? "ENEMY" : "HERO";

        var rows = new List<OpponentAiRow>
        {
            new()
            {
                Group     = "Non AI",
                Name      = "Random AI",
                ModelType = ModelType.Random
            }
        };

        foreach (var model in _trainedModels.OrderByDescending(m => m.TrainedAt))
        {
            if (!string.Equals(model.AgentType, oppositeAgentType, StringComparison.OrdinalIgnoreCase))
                continue;

            var group = model.Algorithm.ToUpperInvariant() switch
            {
                "PPO" => "PPO",
                "A2C" => "A2C",
                "DQN" => "DQN",
                _     => model.Algorithm
            };
            rows.Add(new OpponentAiRow
            {
                Group        = group,
                Name         = model.ExperimentId,
                CreatedDate  = model.TrainedAt,
                ModelType    = Enum.TryParse<ModelType>(model.Algorithm, true, out var mt) ? mt : ModelType.PPO,
                ExperimentId = model.ExperimentId,
                AgentType    = model.AgentType
            });
        }

        _opponentRows = rows;
        _selectedOpponentRow = rows[0]; // Random AI by default
    }

    private void OnTraineeAgentChanged(TraineeAgentType value)
    {
        _cmd.TraineeAgent = value;
        BuildOpponentRows();
    }

    private void OnOpponentRowSelected(OpponentAiRow? row)
    {
        _selectedOpponentRow = row;
        if (row is null) return;

        _cmd.OpponentAi = row.ModelType == ModelType.Random
            ? new AgentAiConfigDto { ModelType = ModelType.Random }
            : new AgentAiConfigDto
            {
                ModelType    = row.ModelType,
                ExperimentId = row.ExperimentId,
                AgentType    = Enum.TryParse<TraineeAgentType>(row.AgentType, true, out var at) ? at : TraineeAgentType.Hero
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
