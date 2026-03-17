using AuxiliumLab.Frontend.Features.Simulation.Dto;

namespace AuxiliumLab.Frontend.Features.Simulation.Pages;

public partial class VisualizationPage : IAsyncDisposable
{
    private const int CellSize = 16;
    private const int MaxLogs  = 200;

    private readonly StartSingleSimulationCommand _cmd = new()
    {
        Kind      = SimulationKind.RandomAI,
        Algorithm = ModelType.PPO
    };

    private SimulationSandboxOverrideDto _override = new();

    private bool   _starting;
    private bool   _running;
    private bool   _paused;
    private Guid   _jobId;
    private int    _gridWidth;
    private int    _gridHeight;
    private int    _maxTurns;
    private int    _currentTurn;
    private string? _outcome;
    private string? _reason;

    private Dictionary<string, SimulationCellDto> _cells          = [];
    private Dictionary<string, (int X, int Y, string Type)> _agentPositions = [];
    private Dictionary<string, AgentSnapshotDto> _agents          = [];
    private readonly List<string> _logs = [];

    protected override async Task OnInitializedAsync()
    {
        HubClient.OnSimulationStarted  += HandleStarted;
        HubClient.OnAgentMoved         += HandleAgentMoved;
        HubClient.OnAgentToggled       += HandleAgentToggled;
        HubClient.OnTurnCompleted      += HandleTurnCompleted;
        HubClient.OnSimulationEnded    += HandleEnded;
        HubClient.OnDebugMessage       += msg => { AddLog(msg); InvokeAsync(StateHasChanged); };

        var sb = SandboxConfig.Value;
        _override = new SimulationSandboxOverrideDto
        {
            MaxTurns       = sb.MaxTurns.Current,
            MapWidth       = sb.MapSettings.Size.Width.Current,
            MapHeight      = sb.MapSettings.Size.Height.Current,
            BlocksPercent  = sb.MapSettings.ElementsPercentages.BlocksPercent.Current,
            EnemiesPercent = sb.MapSettings.ElementsPercentages.PercentOfEnemies.Current,
            HeroSpeed      = sb.Hero.Speed.Current,
            HeroSightRange = sb.Hero.SightRange.Current,
            HeroStamina    = sb.Hero.Stamina.Current,
            EnemySpeed     = sb.Enemy.Speed.Current
        };
        _cmd.SandboxSettings = _override;
    }

    private void OnOverrideChanged(SimulationSandboxOverrideDto dto)
    {
        _cmd.SandboxSettings = dto;
    }

    private async Task StartAsync()
    {
        _starting  = true;
        _cells     = [];
        _agentPositions = [];
        _agents    = [];
        _logs.Clear();
        _outcome   = null;
        _reason    = null;
        _currentTurn = 0;

        try
        {
            var result = await SimulationApi.StartSingleSimulationAsync(_cmd);
            if (result is null) { AddLog("Failed to start simulation."); return; }

            _jobId   = result.JobId;
            _running = true;
            _paused  = false;
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

    private async Task PauseResumeAsync()
    {
        if (_paused)
        {
            await SimulationApi.ResumeSimulationAsync(_jobId);
            _paused = false;
            AddLog("Resumed");
        }
        else
        {
            await SimulationApi.PauseSimulationAsync(_jobId);
            _paused = true;
            AddLog("Paused");
        }
    }

    private async Task StopAsync()
    {
        await SimulationApi.StopSimulationAsync(_jobId);
        _running = false;
        AddLog("Stop requested");
        Notifications.Notify("Simulation stop requested");
    }

    // ── Hub event handlers ────────────────────────────────────────────────────

    private void HandleStarted(SimulationStartedDto e)
    {
        _gridWidth  = e.Width;
        _gridHeight = e.Height;
        _maxTurns   = e.MaxTurns;

        _cells = e.Cells.ToDictionary(c => $"{c.X},{c.Y}");

        foreach (var a in e.Agents)
        {
            _agentPositions[a.AgentId] = (a.X, a.Y, a.AgentType);
            _agents[a.AgentId] = a.Snapshot;
        }

        AddLog($"Grid {e.Width}x{e.Height}, max turns={e.MaxTurns}, agents={e.Agents.Length} [{string.Join(", ", e.Agents.Select(a => $"{a.AgentType}@({a.X},{a.Y})"))  }]");
        InvokeAsync(StateHasChanged);
    }

    private void HandleAgentMoved(AgentMovedDto e)
    {
        try
        {
            _agentPositions[e.AgentId] = (e.ToX, e.ToY, e.AgentType);
            _agents[e.AgentId] = e.Agent;
            AddLog($"AgentMoved T{_currentTurn}: {e.AgentType} {e.AgentId[..Math.Min(6, e.AgentId.Length)]} → ({e.ToX},{e.ToY})");
            InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            AddLog($"HandleAgentMoved EXCEPTION: {ex.Message}");
        }
    }

    private void HandleAgentToggled(AgentToggledDto e)
    {
        AddLog($"{e.AgentType} {e.AgentId[..Math.Min(6, e.AgentId.Length)]} {e.Action} ({(e.IsActivated ? "on" : "off")})");
        InvokeAsync(StateHasChanged);
    }

    private void HandleTurnCompleted(TurnCompletedDto e)
    {
        _currentTurn = e.TurnNumber;
        foreach (var cell in e.UpdatedCells)
            _cells[$"{cell.X},{cell.Y}"] = cell;
        AddLog($"TurnCompleted: turn={e.TurnNumber} cells={e.UpdatedCells.Length}");
        InvokeAsync(StateHasChanged);
    }

    private void HandleEnded(SimulationEndedDto e)
    {
        _outcome = e.Outcome;
        _reason  = e.Reason;
        _running = false;
        AddLog($"Ended: {e.Outcome} at turn {e.FinalTurn}");
        Notifications.Notify($"Simulation ended: {e.Outcome}");
        InvokeAsync(StateHasChanged);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AddLog(string msg)
    {
        _logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
        if (_logs.Count > MaxLogs) _logs.RemoveAt(_logs.Count - 1);
    }

    private static string CellFill(string objectType) => objectType switch
    {
        "Block"       => "#616161",
        "BorderBlock" => "#424242",
        "Exit"        => "#43a047",
        _             => "#fafafa"
    };

    private static string AgentFill(string agentType) => agentType switch
    {
        "Hero"  => "#1e88e5",
        "Enemy" => "#e53935",
        _       => "#9c27b0"
    };

    public async ValueTask DisposeAsync()
    {
        HubClient.OnSimulationStarted  -= HandleStarted;
        HubClient.OnAgentMoved         -= HandleAgentMoved;
        HubClient.OnAgentToggled       -= HandleAgentToggled;
        HubClient.OnTurnCompleted      -= HandleTurnCompleted;
        HubClient.OnSimulationEnded    -= HandleEnded;
        HubClient.OnDebugMessage       -= msg => { AddLog(msg); InvokeAsync(StateHasChanged); };

        if (_jobId != default)
            await HubClient.DisconnectAsync();
    }
}
