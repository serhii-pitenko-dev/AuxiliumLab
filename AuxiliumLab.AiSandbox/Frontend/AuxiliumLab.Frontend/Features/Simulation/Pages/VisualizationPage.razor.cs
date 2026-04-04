
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using AuxiliumLab.AiSandbox.SharedContracts;

namespace AuxiliumLab.Frontend.Features.Simulation.Pages;

public partial class VisualizationPage : IAsyncDisposable
{
    private const int CellSize   = 16;
    private const int AxisMargin = 18;
    private const int MaxLogs    = 200;

    // ── Row model for the data grid ──────────────────────────────────────────

    internal class SimulationSourceRow
    {
        public string                    Group        { get; init; } = string.Empty;
        public string                    Name         { get; init; } = string.Empty;
        public DateTime?                 CreatedDate  { get; init; }
        public SimulationKind            Kind         { get; init; }
        public ModelType                 Algorithm    { get; init; }
        public string?                   ExperimentId { get; init; }
        public TrainingPreconditionsDto? Preconditions { get; init; }
        public string?                   AgentType    { get; init; }
    }

    // ── State ────────────────────────────────────────────────────────────────

    internal bool _simulationActive;
    private bool  _loadingModels;

    internal List<SimulationSourceRow> _rows = [];
    internal List<SimulationSourceRow> _heroRows = [];
    internal List<SimulationSourceRow> _enemyRows = [];
    private SimulationSourceRow? _selectedHeroRow;
    private SimulationSourceRow? _selectedEnemyRow;

    private readonly StartSingleSimulationCommand _cmd = new()
    {
        Kind      = SimulationKind.RandomAI,
        Algorithm = ModelType.PPO
    };

    private SimulationSandboxOverrideDto _override = new();

    private bool    _starting;
    private bool    _running;
    private bool    _paused;
    private Guid    _jobId;
    private int     _gridWidth;
    private int     _gridHeight;
    private int     _maxTurns;
    private int     _currentTurn;
    private string? _outcome;
    private string? _reason;

    private Dictionary<string, SimulationCellDto> _cells          = [];
    private Dictionary<string, (Coordinates Position, ObjectType Type)> _agentPositions = [];
    private Dictionary<string, AgentSnapshotDto> _agents          = [];
    private readonly List<string> _logs = [];

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        HubClient.OnSimulationStarted  += HandleStarted;
        HubClient.OnAgentMoved         += HandleAgentMoved;
        HubClient.OnAgentToggled       += HandleAgentToggled;
        HubClient.OnTurnCompleted      += HandleTurnCompleted;
        HubClient.OnSimulationEnded    += HandleEnded;
        HubClient.OnDebugMessage       += HandleDebugMessage;

        _override = BuildDefaultOverride();
        _cmd.SandboxSettings = _override;
        _cmd.ActionDelayMs = _override.ActionDelayMs;

        await LoadModelsAndBuildRowsAsync();
    }

    internal SimulationSandboxOverrideDto BuildDefaultOverride()
    {
        var sb = SandboxConfig.Value;
        return new SimulationSandboxOverrideDto
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
        };
    }

    private async Task LoadModelsAndBuildRowsAsync()
    {
        _loadingModels = true;
        var rows = new List<SimulationSourceRow>();

        var randomRow = new SimulationSourceRow
        {
            Group = "Non AI",
            Name  = "Random AI",
            Kind  = SimulationKind.RandomAI
        };
        rows.Add(randomRow);

        try
        {
            var models = await TrainingApi.GetTrainedModelsAsync();
            foreach (var model in models.OrderByDescending(m => m.TrainedAt))
            {
                var group = model.Algorithm.ToUpperInvariant() switch
                {
                    "PPO" => "PPO",
                    "A2C" => "A2C",
                    "DQN" => "DQN",
                    _     => model.Algorithm
                };
                rows.Add(new SimulationSourceRow
                {
                    Group         = group,
                    Name          = model.ExperimentId,
                    CreatedDate   = model.TrainedAt,
                    Kind          = SimulationKind.TrainedAI,
                    Algorithm     = Enum.TryParse<ModelType>(model.Algorithm, true, out var mt) ? mt : ModelType.PPO,
                    ExperimentId  = model.ExperimentId,
                    Preconditions = model.Preconditions,
                    AgentType     = model.AgentType
                });
            }
        }
        catch (Exception ex)
        {
            Notifications.Notify($"Error loading models: {ex.Message}");
        }

        _rows = rows;
        BuildFilteredRows();
        _selectedHeroRow = _heroRows.First(r => r.Kind == SimulationKind.RandomAI);
        _selectedEnemyRow = _enemyRows.First(r => r.Kind == SimulationKind.RandomAI);
        _loadingModels = false;
    }

    private void BuildFilteredRows()
    {
        _heroRows = _rows.Where(r => r.Kind == SimulationKind.RandomAI
                                     || string.Equals(r.AgentType, "HERO", StringComparison.OrdinalIgnoreCase))
                         .ToList();
        _enemyRows = _rows.Where(r => r.Kind == SimulationKind.RandomAI
                                      || string.Equals(r.AgentType, "ENEMY", StringComparison.OrdinalIgnoreCase))
                          .ToList();
    }

    // ── Row selection ────────────────────────────────────────────────────────

    private void OnHeroRowSelected(SimulationSourceRow? row)
    {
        _selectedHeroRow = row;
        if (row is null) return;

        _cmd.HeroAi = BuildAiConfigFromRow(row);
        ApplyPreconditionsToOverride();
    }

    private void OnEnemyRowSelected(SimulationSourceRow? row)
    {
        _selectedEnemyRow = row;
        if (row is null) return;

        _cmd.EnemyAi = BuildAiConfigFromRow(row);
        ApplyPreconditionsToOverride();
    }

    private static AgentAiConfigDto BuildAiConfigFromRow(SimulationSourceRow row) => row.Kind switch
    {
        SimulationKind.TrainedAI => new AgentAiConfigDto
        {
            ModelType    = row.Algorithm,
            ExperimentId = row.ExperimentId,
            AgentType    = Enum.TryParse<TraineeAgentType>(row.AgentType, true, out var at) ? at : TraineeAgentType.Hero
        },
        _ => new AgentAiConfigDto { ModelType = ModelType.Random }
    };

    private void ApplyPreconditionsToOverride()
    {
        // Pick preconditions from the first trained model found (hero takes priority)
        var precond = _selectedHeroRow?.Preconditions ?? _selectedEnemyRow?.Preconditions;
        var ov = BuildDefaultOverride();
        if (precond is not null)
        {
            ov.MaxTurns       = precond.MaxTurns;
            ov.MapWidth       = precond.MapWidth;
            ov.MapHeight      = precond.MapHeight;
            ov.BlocksPercent  = precond.BlocksPercent;
            ov.EnemiesPercent = precond.EnemiesPercent;
            ov.HeroSpeed      = precond.HeroSpeed;
            ov.HeroSightRange = precond.HeroSightRange;
            ov.HeroStamina    = precond.HeroStamina;
            ov.EnemySpeed       = precond.EnemySpeed;
            ov.EnemySightRange  = precond.EnemySightRange;
            ov.EnemyStamina     = precond.EnemyStamina;
        }
        _override = ov;
        _cmd.SandboxSettings = _override;
    }

    private void OnOverrideChanged(SimulationSandboxOverrideDto dto)
    {
        _cmd.SandboxSettings = dto;
        _cmd.ActionDelayMs = dto.ActionDelayMs;
    }

    // ── Simulation controls ─────────────────────────────────────────────────

    private async Task StartAsync()
    {
        _starting       = true;
        _cells          = [];
        _agentPositions = [];
        _agents         = [];
        _logs.Clear();
        _outcome     = null;
        _reason      = null;
        _currentTurn = 0;
        _gridWidth   = 0;
        _gridHeight  = 0;

        try
        {
            var result = await SimulationApi.StartSingleSimulationAsync(_cmd);
            if (result is null) { AddLog("Failed to start simulation."); return; }

            _jobId   = result.JobId;
            _running = true;
            _paused  = false;
            _simulationActive = true;
            AddLog($"Started job {_jobId}");

            await HubClient.ConnectAsync(_jobId.ToString());
        }
        catch (Exception ex)
        {
            AddLog($"Error: {ex.Message}");
            Notifications.Notify($"Error starting simulation: {ex.Message}");
        }
        finally
        {
            _starting = false;
        }
    }

    private async Task BackToConfigAsync()
    {
        if (_running)
        {
            await SimulationApi.StopSimulationAsync(_jobId);
            _running = false;
        }

        if (_jobId != default)
        {
            await HubClient.DisconnectAsync();
            _jobId = default;
        }

        _simulationActive = false;
    }

    private async Task PauseResumeAsync()
    {
        if (_paused)
        {
            await SimulationApi.ResumeSimulationAsync(_jobId);
            _paused = false;
            AddRawLog("---Resumed---");
        }
        else
        {
            await SimulationApi.PauseSimulationAsync(_jobId);
            _paused = true;
            AddRawLog("---Paused---");
        }
    }

    private async Task StopAsync()
    {
        await SimulationApi.StopSimulationAsync(_jobId);
        _running = false;
        AddRawLog("---Stop Requested---");
        Notifications.Notify("Simulation stop requested");
    }

    private async Task ReRunAsync()
    {
        if (_jobId != default)
        {
            await HubClient.DisconnectAsync();
            _jobId = default;
        }

        await StartAsync();
    }

    // ── Hub event handlers ────────────────────────────────────────────────────

    private void HandleDebugMessage(string msg)
    {
        AddLog(msg);
        InvokeAsync(StateHasChanged);
    }

    private void HandleStarted(SimulationStartedDto e)
    {
        _gridWidth  = e.Width;
        _gridHeight = e.Height;
        _maxTurns   = e.MaxTurns;

        _cells = e.Cells.ToDictionary(c => $"{c.Position.X},{c.Position.Y}");

        foreach (var a in e.Agents)
        {
            _agentPositions[a.AgentId] = (a.Position, a.AgentType);
            _agents[a.AgentId] = a.Snapshot;
        }

        AddLog($"Grid {e.Width}x{e.Height}, max turns={e.MaxTurns}, agents={e.Agents.Length}");
        foreach (var a in e.Agents)
            AddLog($"  {a.AgentType} @ ({a.Position.X},{a.Position.Y})");
        InvokeAsync(StateHasChanged);
    }

    private void HandleAgentMoved(AgentMovedDto e)
    {
        try
        {
            if (e.IsSuccess)
                _agentPositions[e.AgentId] = (e.To, e.AgentType);
            _agents[e.AgentId] = e.Agent;

            foreach (var cell in e.UpdatedCells)
                _cells[$"{cell.Position.X},{cell.Position.Y}"] = cell;

            var action = e.Agent?.IsRun == true ? "Run" : "Move";
            var status = e.IsSuccess ? "" : " FAILED";
            AddRawLog($"{e.AgentType} #{e.AgentId[..Math.Min(6, e.AgentId.Length)]} {action}: ({e.From.X},{e.From.Y}) → ({e.To.X},{e.To.Y}){status}");
            InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            AddLog($"HandleAgentMoved EXCEPTION: {ex.Message}");
        }
    }

    private void HandleAgentToggled(AgentToggledDto e)
    {
        _agents[e.AgentId] = e.Agent;
        var state = e.IsActivated ? "Activated" : "Deactivated";
        AddRawLog($"{e.AgentType} #{e.AgentId[..Math.Min(6, e.AgentId.Length)]} {e.Action} ability: {state}");
        InvokeAsync(StateHasChanged);
    }

    private void HandleTurnCompleted(TurnCompletedDto e)
    {
        _currentTurn = e.TurnNumber;
        foreach (var cell in e.UpdatedCells)
            _cells[$"{cell.Position.X},{cell.Position.Y}"] = cell;
        AddRawLog($"--------Turn {e.TurnNumber}--------");
        InvokeAsync(StateHasChanged);
    }

    private void HandleEnded(SimulationEndedDto e)
    {
        _outcome = e.Outcome;
        _reason  = e.Reason;
        _running = false;
        AddRawLog($"========{e.Outcome} at turn {e.FinalTurn}========");
        Notifications.Notify($"Simulation ended: {e.Outcome}");
        InvokeAsync(StateHasChanged);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AddLog(string msg)
    {
        _logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
        if (_logs.Count > MaxLogs) _logs.RemoveAt(_logs.Count - 1);
    }

    private void AddRawLog(string msg)
    {
        _logs.Insert(0, msg);
        if (_logs.Count > MaxLogs) _logs.RemoveAt(_logs.Count - 1);
    }

    internal static string CellFill(ObjectType objectType) => objectType switch
    {
        ObjectType.Block       => "#616161",
        ObjectType.BorderBlock => "#424242",
        ObjectType.Exit        => "#43a047",
        _                      => "#fafafa"
    };

    internal static string AgentFill(ObjectType agentType) => agentType switch
    {
        ObjectType.Hero  => "#1e88e5",
        ObjectType.Enemy => "#e53935",
        _                => "#9c27b0"
    };

    /// <summary>
    /// Returns an SVG fill colour for the highest-priority effect overlay on a cell.
    /// Prioritization (highest first): Hero path → Enemy path → Hero sight → Enemy sight.
    /// Returns <c>null</c> for block/border cells (solid obstacles suppress overlays).
    /// </summary>
    internal static string? GetEffectFill(SimulationCellDto cell)
    {
        if (cell.ObjectType is ObjectType.Block or ObjectType.BorderBlock) return null;
        if (cell.Effects.Length == 0) return null;

        if (HasEffect(cell, ObjectType.Hero,  EEffect.Path))   return "rgba(30,136,229,0.60)";
        if (HasEffect(cell, ObjectType.Enemy, EEffect.Path))   return "rgba(229,57,53,0.60)";
        if (HasEffect(cell, ObjectType.Hero,  EEffect.Vision)) return "rgba(30,136,229,0.20)";
        if (HasEffect(cell, ObjectType.Enemy, EEffect.Vision)) return "rgba(229,57,53,0.20)";

        return null;
    }

    /// <summary>Returns the CSS class name for the highest-priority effect on the cell.</summary>
    internal static string GetEffectClass(SimulationCellDto cell)
    {
        if (HasEffect(cell, ObjectType.Hero,  EEffect.Path))   return "hero-path";
        if (HasEffect(cell, ObjectType.Enemy, EEffect.Path))   return "enemy-path";
        if (HasEffect(cell, ObjectType.Hero,  EEffect.Vision)) return "hero-vision";
        if (HasEffect(cell, ObjectType.Enemy, EEffect.Vision)) return "enemy-vision";
        return string.Empty;
    }

    /// <summary>Checks whether the cell has a specific effect from a given agent type.</summary>
    internal static bool HasEffect(SimulationCellDto cell, ObjectType agentType, EEffect effect)
        => cell.Effects.Any(ae => ae.AgentType == agentType && ae.Effects.Contains(effect));

    public async ValueTask DisposeAsync()
    {
        HubClient.OnSimulationStarted  -= HandleStarted;
        HubClient.OnAgentMoved         -= HandleAgentMoved;
        HubClient.OnAgentToggled       -= HandleAgentToggled;
        HubClient.OnTurnCompleted      -= HandleTurnCompleted;
        HubClient.OnSimulationEnded    -= HandleEnded;
        HubClient.OnDebugMessage       -= HandleDebugMessage;

        if (_jobId != default)
            await HubClient.DisconnectAsync();
    }
}
