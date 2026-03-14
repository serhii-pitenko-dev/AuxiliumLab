using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.Entities;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Maps;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetAffectedCells;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetMapLayout;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Lost;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.GlobalMessagesContract.Events.Win;
using AuxiliumLab.AiSandbox.ConsolePresentation.Settings;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace AuxiliumLab.AiSandbox.ConsolePresentation;

public class ConsoleRunner : IConsoleRunner
{
    private readonly IMessageBroker _messageBroker;
    private readonly IMapQueriesHandleService _mapQueries;
    private readonly ColorScheme _consoleColorScheme;
    private readonly int _actionTimeout;
    private Dictionary<ObjectType, string> _cellData = [];
    private Guid _playgroundId;
    private int _mapWidth;
    private int _mapHeight;
    private List<string> _eventMessages = [];
    public event Action<Guid>? ReadyForRendering;
    private MapLayoutResponse _fullMapLayout;
    private readonly IFileDataManager<StandardPlaygroundState> _playgroundStateFileRepository;
    private StandardPlaygroundState _standardPlaygroundState;

    // â”€â”€ Fixed layout row anchors (set once in OnGameStarted) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Layout:
    //   Row 0           blank
    //   Row 1â€“4         header info (4 lines written by RenderInitialGameInfo)
    //   Row 5           blank + turn counter
    //   Row 6           map top border      â† _mapStartRow
    //   Row 6+1 â€¦       map rows
    //   Row 6+H+1       map bottom border
    //   Row 6+H+2       blank separator
    //   Row 6+H+3 â€¦     scrolling log zone  â† _logStartRow
    private const int MapStartRow  = 6;
    private const int HeaderRows   = 6;     // rows 0-5 (blank + 4 info + blank/turn)
    private const int LogLineCount = 20;    // fixed height of the log zone

    private int _logStartRow;               // computed after map size is known

    public ConsoleRunner(
        IAiActions aiActions,
        IMessageBroker messageBroker,
        IMapQueriesHandleService mapQueries,
        IOptions<ConsoleSettings> consoleSettings,
        IFileDataManager<StandardPlaygroundState> playgroundStateFileRepository)
    {
        _messageBroker = messageBroker;
        _mapQueries = mapQueries;
        _consoleColorScheme = consoleSettings.Value.ColorScheme;
        _actionTimeout = consoleSettings.Value.ActionTimeout;
        _fullMapLayout = new MapLayoutResponse(-1, new MapCell[0, 0]);
        _playgroundStateFileRepository = playgroundStateFileRepository;
    }

    public void Run()
    {
        InitializeConsole();

        _messageBroker.Subscribe<GameStartedEvent>(OnGameStarted);
        _messageBroker.Subscribe<OnBaseAgentActionEvent>(OnAgentActionEvent);
        _messageBroker.Subscribe<TurnExecutedEvent>(OnTurnEnded);
        _messageBroker.Subscribe<HeroLostEvent>(OnGameLost);
        _messageBroker.Subscribe<HeroWonEvent>(OnGameWon);
    }

    private void InitializeConsole()
    {
        Console.CursorVisible = false;
        InitializeElementsRendering();
        Console.Clear();
    }

    private void OnGameStarted(GameStartedEvent message)
    {
        _playgroundId = message.PlaygroundId;
        _standardPlaygroundState = _playgroundStateFileRepository.LoadObjectAsync(_playgroundId).Result;

        // â”€â”€ Header block (rows 0-5) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        Console.SetCursorPosition(0, 0);
        RenderInitialGameInfo();                // writes rows 1-4
        WriteTurnRow(0);       // writes row 5

        // â”€â”€ Map zone â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        _fullMapLayout = _mapQueries.MapLayoutQuery.GetFromMemory(_playgroundId);
        _mapWidth  = _fullMapLayout.Cells.GetLength(0);
        _mapHeight = _fullMapLayout.Cells.GetLength(1);

        // log zone starts immediately after: top border + H map rows + bottom border + 1 blank
        _logStartRow = MapStartRow + 1 + _mapHeight + 1 + 1;

        Console.SetCursorPosition(0, MapStartRow);
        RenderFullMap(_fullMapLayout);
    }

    private void RerenderCells(HashSet<Coordinates> coordinates)
    {
        foreach (var coord in coordinates)
        {
            RerenderSingleCell(coord);
        }
    }

    private void RerenderSingleCell(Coordinates coordinates)
    {
        int x = coordinates.X;
        int y = coordinates.Y;

        MapCell cell = _fullMapLayout.Cells[x, y];

        // +1 for left border column, +1 for map top border row
        int consoleX = x + 2;
        int consoleY = MapStartRow + 1 + (_mapHeight - 1 - y);

        Console.SetCursorPosition(consoleX, consoleY);
        AnsiConsole.Markup(GetCellSymbol(cell));
    }

    private void OnTurnEnded(TurnExecutedEvent message)
    {
        _eventMessages.Add($"=== Turn {message.TurnNumber} completed ===");
        WriteTurnRow(message.TurnNumber);
        RenderLogZone();
    }

    private void OnAgentActionEvent(OnBaseAgentActionEvent message)
    {
        string eventMessage = ConvertEventToString(message);
        _eventMessages.Add(eventMessage);
        RenderLogZone();

        if (message is OnAgentMoveActionEvent moveEvent)
            HandleAgentMoveEvent(moveEvent);

        Thread.Sleep(_actionTimeout);
    }

    private void HandleAgentMoveEvent(OnAgentMoveActionEvent moveEvent)
    {
        var affectedCellsToRerender = new HashSet<Coordinates>();

        // Step 1: Remove old AgentEffect entries for this agent from all cells
        for (int x = 0; x < _mapWidth; x++)
        {
            for (int y = 0; y < _mapHeight; y++)
            {
                MapCell cell = _fullMapLayout.Cells[x, y];

                // Check if this cell has effects from the moving agent
                var effectsWithoutAgent = cell.Effects
                    .Where(effect => effect.AgentId != moveEvent.AgentId)
                    .ToArray();

                // If effects changed, update the cell
                if (effectsWithoutAgent.Length != cell.Effects.Length)
                {
                    _fullMapLayout.Cells[x, y] = cell with { Effects = effectsWithoutAgent };
                    affectedCellsToRerender.Add(new Coordinates(x, y));
                }
            }
        }

        // Step 2: Get new affected cells for this agent
        AffectedCellsResponse affectedCellsResponse =
            _mapQueries.AffectedCellsQuery.GetFromMemory(_playgroundId, moveEvent.AgentId);

        // Step 3: Apply new AgentEffect entries to the map
        foreach (var newCell in affectedCellsResponse.Cells)
        {
            int x = newCell.Coordinates.X;
            int y = newCell.Coordinates.Y;

            // Get the current cell from the full map layout
            MapCell currentCell = _fullMapLayout.Cells[x, y];

            // Merge effects: keep existing effects from other agents and add new effects
            var mergedEffects = currentCell.Effects
                .Where(effect => effect.AgentId != moveEvent.AgentId) // Remove old effects for this agent (safety)
                .Concat(newCell.Effects) // Add new effects
                .ToArray();

            // Update the cell in the full map layout
            _fullMapLayout.Cells[x, y] = currentCell with { Effects = mergedEffects };
            affectedCellsToRerender.Add(newCell.Coordinates);
        }

        // Step 4: Apply move result to local map state.
        // Uses ConsoleMapState.ApplyAgentMove which handles the IsSuccess guard:
        // successful â†’ clear "from", stamp "to"; failed â†’ only queue "from" for
        // re-render so the agent icon is never erased from its real position.
        ConsoleMapState.ApplyAgentMove(
            _fullMapLayout.Cells,
            moveEvent.From,
            moveEvent.To,
            moveEvent.AgentId,
            moveEvent.IsSuccess,
            affectedCellsToRerender);

        // Step 5: Re-render all affected cells
        RerenderCells(affectedCellsToRerender);
    }

    private string ConvertEventToString(OnBaseAgentActionEvent actionEvent)
    {
        string eventMessage = actionEvent switch
        {
            OnAgentMoveActionEvent moveEvent =>
                moveEvent.IsSuccess
                    ? $"Agent {moveEvent.AgentId:N} moved from ({moveEvent.From.X}, {moveEvent.From.Y}) to ({moveEvent.To.X}, {moveEvent.To.Y})"
                    : $"Agent {moveEvent.AgentId:N} FAILED to move from ({moveEvent.From.X}, {moveEvent.From.Y}) - invalid move",
            OnAgentToggleActionEvent toggleActionEvent =>
                    $"Agent {toggleActionEvent.AgentId:N} {(toggleActionEvent.IsActivated ? "activated" : "deactivated")} action: {toggleActionEvent.AgentAction}",
                    
            _ => $"Unknown event: {actionEvent.GetType().Name}"
        };


        string runStatus = actionEvent.AgentSnapshot.IsRun ? "Running" : "Walking";
        eventMessage += $"\n    â†’ Type: {actionEvent.AgentSnapshot.Type}, Speed: {actionEvent.AgentSnapshot.Speed}, Sight: {actionEvent.AgentSnapshot.SightRange}, {runStatus}, Stamina: {actionEvent.AgentSnapshot.Stamina}, Max Stamina: {actionEvent.AgentSnapshot.MaxStamina}, Turn Order: {actionEvent.AgentSnapshot.OrderInTurnQueue}";


        return eventMessage;
    }

    private void RenderInitialGameInfo()
    {
        // Rows 1-4: static header â€” never scrolls, always at these absolute rows.
        Console.SetCursorPosition(0, 1);
        WriteSysInfoLine($"Map ID  : {_playgroundId}");
        WriteSysInfoLine($"Size    : {_standardPlaygroundState.Map.Width} Ã— {_standardPlaygroundState.Map.Height}" +
                         $"  |  Area: {_standardPlaygroundState.Map.Width * _standardPlaygroundState.Map.Height}" +
                         $"  |  Blocks: {_standardPlaygroundState.Blocks.Count / (double)(_standardPlaygroundState.Map.Width * _standardPlaygroundState.Map.Height):P0}" +
                         $"  |  Enemies: {_standardPlaygroundState.Enemies.Count / (double)(_standardPlaygroundState.Map.Width * _standardPlaygroundState.Map.Height):P0}");
        WriteSysInfoLine($"Elements: blocks {_standardPlaygroundState.Blocks.Count}, enemies {_standardPlaygroundState.Enemies.Count}");
        WriteSysInfoLine(new string('â”€', Math.Max(1, Console.WindowWidth - 1)));
    }

    private void WriteTurnRow(int turnNumber)
    {
        // Row 5 â€” turn counter, always overwritten in place.
        Console.SetCursorPosition(0, 5);
        WriteSysInfoLine($" Turn: {turnNumber}");
    }

    private void RenderFullMap(MapLayoutResponse mapRenderData)
    {
        int width  = mapRenderData.Cells.GetLength(0);
        int height = mapRenderData.Cells.GetLength(1);

        // Cursor must already be at MapStartRow before this is called.
        // Top border
        AnsiConsole.MarkupLine($"[{_consoleColorScheme.BorderColor} on {_consoleColorScheme.MapBackGroundColor}] {new string('â–ˆ', width + 2)}[/]");

        // Map rows (Cartesian Y: top â†’ bottom)
        for (int cartesianY = height - 1; cartesianY >= 0; cartesianY--)
        {
            string row = string.Empty;
            for (int x = 0; x < width; x++)
                row += GetCellSymbol(mapRenderData.Cells[x, cartesianY]);

            string leftBorder = (cartesianY < 10) ? cartesianY.ToString() : "â–ˆ";
            AnsiConsole.MarkupLine(
                $"[{_consoleColorScheme.BorderColor} on {_consoleColorScheme.MapBackGroundColor}] {leftBorder}[/]" +
                $"{row}" +
                $"[{_consoleColorScheme.BorderColor} on {_consoleColorScheme.MapBackGroundColor}]â–ˆ[/]");
        }

        // Bottom border with X-axis numbers
        string bottomBorder = " â–ˆ";
        for (int x = 0; x < width; x++)
            bottomBorder += (x < 10) ? x.ToString() : "â–ˆ";
        bottomBorder += "â–ˆ";
        AnsiConsole.MarkupLine($"[{_consoleColorScheme.BorderColor} on {_consoleColorScheme.MapBackGroundColor}]{bottomBorder}[/]");
    }

    private string GetCellSymbol(MapCell cell)
    {
        // First check if there's an actual agent/object at this cell
        if (cell.ObjectType != ObjectType.Empty)
        {
            return _cellData[cell.ObjectType];
        }

        // Priority order for rendering effects:
        // 1. Hero Path (highest priority)
        // 2. Hero Vision
        // 3. Enemy Path
        // 4. Enemy Vision (lowest priority)

        bool hasHeroPath = false;
        bool hasHeroVision = false;
        bool hasEnemyPath = false;
        bool hasEnemyVision = false;

        foreach (var agentEffect in cell.Effects)
        {
            if (agentEffect.AgentType == ObjectType.Hero)
            {
                if (agentEffect.Effects.Contains(EEffect.Path))
                    hasHeroPath = true;
                if (agentEffect.Effects.Contains(EEffect.Vision))
                    hasHeroVision = true;
            }
            else if (agentEffect.AgentType == ObjectType.Enemy)
            {
                if (agentEffect.Effects.Contains(EEffect.Path))
                    hasEnemyPath = true;
                if (agentEffect.Effects.Contains(EEffect.Vision))
                    hasEnemyVision = true;
            }
        }

        // Render based on priority
        if (hasHeroPath)
            return $"[#000000 on {_consoleColorScheme.HeroPathColor}]Â·[/]";
        if (hasHeroVision)
            return $"[#000000 on {_consoleColorScheme.HeroVisionColor}]Â·[/]";
        if (hasEnemyPath)
            return $"[#000000 on {_consoleColorScheme.EnemyPathColor}]Â·[/]";
        if (hasEnemyVision)
            return $"[#000000 on {_consoleColorScheme.EnemyVisionColor}]Â·[/]";

        return _cellData[ObjectType.Empty];
    }

    private void RenderLogZone()
    {
        if (_logStartRow == 0) return; // map not yet initialized

        Console.SetCursorPosition(0, _logStartRow);

        // Take the last LogLineCount entries and overwrite the fixed log zone in place.
        int start = Math.Max(0, _eventMessages.Count - LogLineCount);
        int windowWidth = Math.Max(1, Console.WindowWidth - 1);

        for (int i = 0; i < LogLineCount; i++)
        {
            int msgIdx = start + i;
            if (msgIdx < _eventMessages.Count)
            {
                // Pad/truncate to window width so the previous longer line is fully erased.
                string line = _eventMessages[msgIdx];
                if (line.Length > windowWidth) line = line[..windowWidth];
                WriteSysInfoLine(line.PadRight(windowWidth));
            }
            else
            {
                // Empty row â€” erase any leftover text.
                WriteSysInfoLine(new string(' ', windowWidth));
            }
        }

        // Park cursor at a safe position below the log zone so it doesn't flicker on the map.
        Console.SetCursorPosition(0, _logStartRow + LogLineCount);
    }

    private void WriteSysInfoLine(string message)
    {
        AnsiConsole.MarkupLine($"[{_consoleColorScheme.BorderColor} on {_consoleColorScheme.GlobalBackGroundColor}]{message}[/]");
    }

    private void InitializeElementsRendering()
    {
        _cellData = new Dictionary<ObjectType, string>
        {
            { ObjectType.Empty, $"[#000000 on {_consoleColorScheme.MapBackGroundColor}]Â·[/]" },
            { ObjectType.Block, $"[{_consoleColorScheme.BlockColor} on {_consoleColorScheme.MapBackGroundColor}]â–ˆ[/]" },
            { ObjectType.BorderBlock, $"[{_consoleColorScheme.BorderBlockColor} on {_consoleColorScheme.MapBackGroundColor}]â–ˆ[/]" },
            { ObjectType.Hero, $"[{_consoleColorScheme.HeroColor} on {_consoleColorScheme.MapBackGroundColor}]â–ˆ[/]" },
            { ObjectType.Enemy, $"[{_consoleColorScheme.EnemyColor} on {_consoleColorScheme.MapBackGroundColor}]X[/]" },
            { ObjectType.Exit, $"[Black on Green]E[/]" }
        };
    }

    private void OnGameWon(HeroWonEvent gameWonEventMessage)
    {
        _eventMessages.Add("!!! HERO WIN !!!");
        RenderLogZone();
    }

    private void OnGameLost(HeroLostEvent gameLostEventMessage)
    {
        _eventMessages.Add("!!! HERO LOST !!!");
        RenderLogZone();
    }

    private void OnTurnLimitReached()
    {
        _eventMessages.Add("!!! TURN LIMIT REACHED !!!");
        RenderLogZone();
    }
}
