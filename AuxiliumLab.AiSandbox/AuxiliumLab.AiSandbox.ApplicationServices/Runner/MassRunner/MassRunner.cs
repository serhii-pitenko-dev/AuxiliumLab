using AuxiliumLab.AiSandbox.ApplicationServices.Executors;
using AuxiliumLab.AiSandbox.Domain.Playgrounds.Factories;
using AuxiliumLab.AiSandbox.Domain.Statistics.Result;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.Statistics.Converters;
using AuxiliumLab.AiSandbox.Statistics.Preconditions;
using AuxiliumLab.AiSandbox.Statistics.StatisticDataManager;
using Microsoft.Extensions.Options;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Runner.MassRunner;

/// <summary>
/// Handles parallel batch runs and incremental sweep runs.
/// </summary>
public class MassRunner
{
    private readonly IStatisticFileDataManager _statisticFileManager;
    private readonly SandBoxConfiguration _configuration;

    public MassRunner(
        IFileDataManager<GeneralBatchRunInformation> batchResultFileManager,
        IStatisticFileDataManager statisticFileManager,
        IOptions<SandBoxConfiguration> configuration)
    {
        _statisticFileManager   = statisticFileManager;
        _configuration          = configuration.Value;
    }

    /// <summary>
    /// Runs <paramref name="count"/> standard simulations in parallel and, for every entry in
    /// <paramref name="incrementalProperties"/>, an additional sweep of simulations – one per step
    /// value of the corresponding <see cref="IncrementalRange"/> – all at their standard values
    /// except for the swept property.
    /// <para>
    /// Total runs = <paramref name="count"/> + Σ ( (Max − Min) / Step ) for each swept property.
    /// </para>
    /// </summary>
    /// <param name="executor">The executor used to run each simulation.</param>
    /// <param name="count">Number of standard (baseline) runs.</param>
    /// <param name="configuration">
    ///   Configuration to use. Falls back to the injected default when <see langword="null"/>.
    /// </param>
    /// <param name="incrementalProperties">
    ///   Property names from <see cref="IncrementalPropertyNames"/> whose ranges should be swept.
    ///   Pass <see langword="null"/> or an empty enumerable to skip incremental runs entirely.
    /// </param>
    public async Task<MassRunCapturedResult> RunManyAsync(
        IExecutorFactory executorFactory,
        int count,
        SandBoxConfiguration? configuration = null,
        SimulationStartupSettings? startupSettings = null)
    {
        var startTime = DateTime.Now;
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine($"MASS RUNNER - Starting batch execution at {startTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        // Pre-warm the thread pool to avoid injection delay; ensures cores are available immediately.
        int minWorkers = Environment.ProcessorCount * 2;
        ThreadPool.SetMinThreads(minWorkers, minWorkers);

        configuration ??= _configuration;
        var propertiesToSweep      = startupSettings?.IncrementalProperties.Properties ?? [];
        int incrementalSimCount    = startupSettings?.IncrementalProperties.SimulationCount ?? 1;
        var batchRunId = Guid.NewGuid();
        bool areaEnabled = configuration.MapSettings.Size.IncrementalArea?.IsEnabled == true;

        LogBatchHeader(batchRunId, count, propertiesToSweep, incrementalSimCount);

        string csvFileName = $"{batchRunId}.csv";
        await SavePreconditionsCsvAsync(configuration, startupSettings, csvFileName);
        await SaveBatchRunInfoAsync(configuration, batchRunId);

        var standardBatch    = await RunStandardPhaseAsync(executorFactory, configuration, batchRunId, count);
        var sweepSummaries   = await RunIncrementalSweepPhaseAsync(executorFactory, configuration, batchRunId, propertiesToSweep, areaEnabled, incrementalSimCount);
        var areaSweepSummary = areaEnabled
            ? await RunAreaSweepPhaseAsync(executorFactory, configuration, batchRunId, sweepSummaries.Count + 1)
            : null;

        // Aggregate totals across all batches for footer output
        var allBatches = new List<BatchSummary> { standardBatch };
        allBatches.AddRange(sweepSummaries.SelectMany(s => s.Batches));
        if (areaSweepSummary is not null)
            allBatches.AddRange(areaSweepSummary.Batches);

        int totalWins      = allBatches.Sum(b => b.Wins);
        int totalCompleted = allBatches.Sum(b => b.TotalRuns);
        long totalTurns    = allBatches.Sum(b => (long)Math.Round(b.AverageTurns * b.TotalRuns));

        var massRunSummary = new MassRunSummary(
            1 + sweepSummaries.Count + (areaSweepSummary is not null ? 1 : 0),
            DateTime.Now - startTime,
            string.Join("; ", sweepSummaries.Select(s => s.Property)));

        await AppendStandardRunCsvAsync(csvFileName, standardBatch);
        foreach (var sweep in sweepSummaries)
            await AppendIncrementalRunSummaryCsvAsync(csvFileName, sweep);
        if (areaSweepSummary is not null)
            await AppendIncrementalRunSummaryCsvAsync(csvFileName, areaSweepSummary);
        await AppendMassRunSummaryCsvAsync(csvFileName, massRunSummary);

        LogBatchFooter(startTime, totalCompleted, totalWins, totalTurns);

        return new MassRunCapturedResult(standardBatch, sweepSummaries, areaSweepSummary);
    }

    // ── Step 1: console header ─────────────────────────────────────────────────

    private static void LogBatchHeader(Guid batchRunId, int count, List<string> propertiesToSweep, int incrementalSimulationCount)
    {
        Console.WriteLine($"Batch Run ID: {batchRunId}");
        Console.WriteLine($"Standard parallel runs: {count}");
        Console.WriteLine($"Max parallelism: {Environment.ProcessorCount} threads");
        if (propertiesToSweep.Any())
        {
            Console.WriteLine($"Incremental properties to sweep: {string.Join(", ", propertiesToSweep)}");
            Console.WriteLine($"Incremental simulation count per step: {incrementalSimulationCount}");
        }
        Console.WriteLine();
    }

    // ── Step 2: save preconditions CSV ────────────────────────────────────────

    private async Task SavePreconditionsCsvAsync(
        SandBoxConfiguration configuration,
        SimulationStartupSettings? startupSettings,
        string csvFileName)
    {
        if (startupSettings is not null)
        {
            await _statisticFileManager.ConvertToCsvAndAppendAsync(csvFileName, TableConverter.ToCsv(startupSettings));
            await _statisticFileManager.ConvertToCsvAndAppendAsync(csvFileName, Environment.NewLine);
        }

        await _statisticFileManager.ConvertToCsvAndAppendAsync(csvFileName, TableConverter.ToCsv(BuildSandBoxSettings(configuration)));
        await _statisticFileManager.ConvertToCsvAndAppendAsync(csvFileName, Environment.NewLine);
    }

    // ── Step 3: persist batch run info ────────────────────────────────────────

    private async Task SaveBatchRunInfoAsync(SandBoxConfiguration configuration, Guid batchRunId)
    {
        int area = (int)configuration.MapSettings.Size.Height * (int)configuration.MapSettings.Size.Width;
        var batchRunInfo = new GeneralBatchRunInformation(
            BlocksCount:  PlaygroundFactory.PercentCalculation(area, configuration.MapSettings.ElementsPercentages.BlocksPercent),
            EnemiesCount: PlaygroundFactory.PercentCalculation(area, configuration.MapSettings.ElementsPercentages.PercentOfEnemies),
            Area:         area,
            MapWidth:     configuration.MapSettings.Size.Width,
            MapHeight:    configuration.MapSettings.Size.Height,
            MapArea:      area);

        Console.WriteLine("Configuration:");
        Console.WriteLine($"  Map Size: {configuration.MapSettings.Size.Width} × {configuration.MapSettings.Size.Height} (Area: {area})");
        Console.WriteLine($"  Blocks: {batchRunInfo.BlocksCount}, Enemies: {batchRunInfo.EnemiesCount}");
        Console.WriteLine($"  Max Turns: {configuration.MaxTurns.Current}");
        Console.WriteLine();
    }

    // ── Step 4: standard parallel runs ────────────────────────────────────────

    private async Task<BatchSummary> RunStandardPhaseAsync(
        IExecutorFactory executorFactory,
        SandBoxConfiguration configuration,
        Guid batchRunId,
        int count)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Starting {count} standard parallel runs...");
        var phaseStart = DateTime.Now;

        var (wins, completed, turns) = await RunSimulationsAsync(executorFactory, configuration, count);

        var execTime = DateTime.Now - phaseStart;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Completed {count} standard runs in {execTime.TotalSeconds:F2}s");
        Console.WriteLine($"  Progress: {completed} runs completed, {wins} wins");
        Console.WriteLine();

        return BuildBatchSummary(1, completed, wins, turns, configuration.MaxTurns.Current, execTime);
    }

    // ── Step 5: incremental named-property sweep ───────────────────────────────

    private async Task<IReadOnlyList<IncrementalRunSummary>> RunIncrementalSweepPhaseAsync(
        IExecutorFactory executorFactory,
        SandBoxConfiguration configuration,
        Guid batchRunId,
        List<string> propertiesToSweep,
        bool areaEnabled,
        int simulationCount)
    {
        if (!propertiesToSweep.Any())
            return [];

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Starting incremental sweep runs...");

        var summaries   = new List<IncrementalRunSummary>();
        int sweepNumber = 1;

        foreach (var propertyName in propertiesToSweep)
        {
            // Skip individual Width/Height sweeps when joint area sweep is active
            if (areaEnabled &&
                (propertyName == IncrementalPropertyNames.MapWidth ||
                 propertyName == IncrementalPropertyNames.MapHeight))
                continue;

            var range = GetRange(configuration, propertyName);
            if (range is null)
                continue;

            int stepCount  = range.StepCount;
            int totalTasks = stepCount * simulationCount;
            Console.WriteLine($"  Sweeping '{propertyName}': {range.Min} to {range.Max}, step {range.Step} ({stepCount} steps × {simulationCount} runs each = {totalTasks} total)");

            var propStart = DateTime.Now;

            // Pre-build per-step configs and Interlocked-safe counter arrays.
            var configs       = new SandBoxConfiguration[stepCount];
            var stepWins      = new int[stepCount];
            var stepCompleted = new int[stepCount];
            var stepTurns     = new long[stepCount];
            var stepEnds      = new DateTime[stepCount]; // set by the last sim of each step

            for (int idx = 0; idx < stepCount; idx++)
                configs[idx] = WithPropertyOverride(configuration, propertyName, range.Min + idx * range.Step);

            Console.WriteLine($"    [{DateTime.Now:HH:mm:ss}] Scheduling all {totalTasks} tasks as a flat queue ({stepCount} steps × {simulationCount} runs)...");

            // Flat queue: all stepCount × simulationCount tasks are submitted to the thread pool at once.
            // This keeps all cores busy regardless of per-step duration variance — when short steps
            // finish early, their threads immediately pick up pending long-step tasks from the queue.
            var allTasks = Enumerable.Range(0, stepCount).SelectMany(i =>
                Enumerable.Range(0, simulationCount).Select(_ => Task.Run(async () =>
                {
                    var result = await executorFactory.CreateStandardExecutor().RunAndCaptureAsync(configs[i]);
                    Interlocked.Add(ref stepTurns[i], result.TurnsCount);
                    if (result.WinReason.HasValue)
                        Interlocked.Increment(ref stepWins[i]);
                    // Record end time when the last simulation of this step finishes.
                    if (Interlocked.Increment(ref stepCompleted[i]) == simulationCount)
                        stepEnds[i] = DateTime.Now;
                })));

            await Task.WhenAll(allTasks);

            var batchSlots = new BatchSummary[stepCount];
            for (int i = 0; i < stepCount; i++)
            {
                var end = stepEnds[i] == default ? DateTime.Now : stepEnds[i];
                batchSlots[i] = BuildBatchSummary(
                    i + 1, stepCompleted[i], stepWins[i], stepTurns[i],
                    configs[i].MaxTurns.Current, end - propStart);
            }

            var batches      = batchSlots.ToList();
            var propExecTime = DateTime.Now - propStart;
            Console.WriteLine($"    [{DateTime.Now:HH:mm:ss}] Completed {totalTasks} runs for '{propertyName}' ({stepCount} steps) in {propExecTime.TotalSeconds:F1}s");

            summaries.Add(new IncrementalRunSummary(
                Guid.NewGuid(), sweepNumber, batches,
                $"Sweep '{propertyName}': {range.Min} to {range.Max}, step {range.Step} ({stepCount} steps × {simulationCount} runs)",
                propExecTime, propertyName, simulationCount,
                range.Min.ToString(), range.Step.ToString(), range.Max.ToString()));

            sweepNumber++;
        }

        return summaries;
    }

    // ── Step 6: joint area sweep (Width + Height together) ────────────────────

    private async Task<IncrementalRunSummary> RunAreaSweepPhaseAsync(
        IExecutorFactory executorFactory,
        SandBoxConfiguration configuration,
        Guid batchRunId,
        int startNumber)
    {
        var sz       = configuration.MapSettings.Size;
        int areaStep = configuration.MapSettings.Size.IncrementalArea!.Step;

        int biggerRange = Math.Max(sz.Width.Max - sz.Width.Min, sz.Height.Max - sz.Height.Min);
        int iterations  = areaStep > 0 ? biggerRange / areaStep : 0;

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Starting joint area sweep (Width + Height)...");
        Console.WriteLine($"  Width range: {sz.Width.Min} to {sz.Width.Max}");
        Console.WriteLine($"  Height range: {sz.Height.Min} to {sz.Height.Max}");
        Console.WriteLine($"  Step: {areaStep}, Iterations: {iterations}");

        var areaStart = DateTime.Now;
        var batches   = new List<BatchSummary>(iterations);

        for (int i = 0; i < iterations; i++)
        {
            int wValue    = Math.Min(sz.Width.Min  + i * areaStep, sz.Width.Max);
            int hValue    = Math.Min(sz.Height.Min + i * areaStep, sz.Height.Max);
            var stepStart = DateTime.Now;
            var result    = await executorFactory.CreateStandardExecutor().RunAndCaptureAsync(WithAreaOverride(configuration, wValue, hValue));
            int stepWins  = result.WinReason.HasValue ? 1 : 0;

            batches.Add(new BatchSummary(
                Guid.NewGuid(), i + 1, 1, stepWins, 1 - stepWins,
                result.TurnsCount,
                configuration.MaxTurns.Current,
                DateTime.Now - stepStart));
        }

        Console.WriteLine($"  Completed {iterations} area sweep runs");
        Console.WriteLine();

        return new IncrementalRunSummary(
            Guid.NewGuid(), startNumber, batches,
            $"Joint area sweep (Width {sz.Width.Min}→{sz.Width.Max}, Height {sz.Height.Min}→{sz.Height.Max}, step {areaStep})",
            DateTime.Now - areaStart, "Area (Width+Height)", 1,
            sz.Width.Min.ToString(), areaStep.ToString(), sz.Width.Max.ToString());
    }

    // ── Step 7: persist and CSV-export summaries ──────────────────────────────

    private async Task AppendStandardRunCsvAsync(string csvFileName, BatchSummary batch)
    {
        await _statisticFileManager.ConvertToCsvAndAppendAsync(csvFileName, TableConverter.ToCsv(batch));
        await _statisticFileManager.ConvertToCsvAndAppendAsync(csvFileName, Environment.NewLine);
    }

    private async Task AppendIncrementalRunSummaryCsvAsync(string csvFileName, IncrementalRunSummary summary)
    {
        await _statisticFileManager.ConvertToCsvAndAppendAsync(csvFileName, TableConverter.ToCsv(summary));
        await _statisticFileManager.ConvertToCsvAndAppendAsync(csvFileName, Environment.NewLine);
    }

    private async Task AppendMassRunSummaryCsvAsync(string csvFileName, MassRunSummary massRunSummary)
    {
        await _statisticFileManager.ConvertToCsvAndAppendAsync(csvFileName, TableConverter.ToCsv(massRunSummary));
    }

    // ── Step 8: console footer ─────────────────────────────────────────────────

    private static void LogBatchFooter(DateTime startTime, int totalCompleted, int totalWins, long totalTurns)
    {
        int losses      = totalCompleted - totalWins;
        double avgTurns = totalCompleted > 0 ? (double)totalTurns / totalCompleted : 0;
        var finishTime  = DateTime.Now;
        var duration    = finishTime - startTime;

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine($"MASS RUNNER - Batch execution completed at {finishTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine($"Total Duration: {duration.TotalSeconds:F2}s ({duration:hh\\:mm\\:ss})");
        Console.WriteLine($"Total Runs: {totalCompleted}");
        Console.WriteLine($"Wins: {totalWins} ({(totalCompleted > 0 ? (totalWins * 100.0 / totalCompleted) : 0):F1}%)");
        Console.WriteLine($"Losses: {losses} ({(totalCompleted > 0 ? (losses * 100.0 / totalCompleted) : 0):F1}%)");
        Console.WriteLine($"Average Turns: {avgTurns:F2}");
        Console.WriteLine($"Total Turns: {totalTurns:N0}");
        Console.WriteLine($"Average Duration per Run: {(totalCompleted > 0 ? duration.TotalSeconds / totalCompleted : 0):F3}s");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs <paramref name="count"/> simulations concurrently using <c>Task.Run</c> + <c>Task.WhenAll</c>
    /// (forces real thread-pool threads for CPU-bound work) and returns aggregate counters.
    /// </summary>
    private static async Task<(int Wins, int TotalRuns, long TotalTurns)> RunSimulationsAsync(
        IExecutorFactory factory, SandBoxConfiguration config, int count)
    {
        int wins = 0, completed = 0;
        long turns = 0;
        var tasks = Enumerable.Range(0, count).Select(_ => Task.Run(async () =>
        {
            var result = await factory.CreateStandardExecutor().RunAndCaptureAsync(config);
            Interlocked.Increment(ref completed);
            Interlocked.Add(ref turns, result.TurnsCount);
            if (result.WinReason.HasValue)
                Interlocked.Increment(ref wins);
        }));
        await Task.WhenAll(tasks);
        return (wins, completed, turns);
    }

    /// <summary>Constructs a <see cref="BatchSummary"/> from raw aggregated counters.</summary>
    private static BatchSummary BuildBatchSummary(
        int number, int totalRuns, int wins, long totalTurns, int maxTurns, TimeSpan execTime)
        => new BatchSummary(
            Guid.NewGuid(), number, totalRuns, wins, totalRuns - wins,
            totalRuns > 0 ? (double)totalTurns / totalRuns : 0,
            maxTurns, execTime);

    /// <summary>
    /// Maps the relevant fields of <paramref name="cfg"/> to a <see cref="SimulationSandBoxSettings"/>
    /// for CSV export, omitting TotalRuns, SaveToFileRegularity, TurnTimeout, FileSource and Type.
    /// </summary>
    private static SimulationSandBoxSettings BuildSandBoxSettings(SandBoxConfiguration cfg)
    {
        static RangeSettings Map(IncrementalRange r) => new(r.Min, r.Current, r.Max, r.Step);

        return new SimulationSandBoxSettings
        {
            MaxTurns              = Map(cfg.MaxTurns),
            MapWidth              = Map(cfg.MapSettings.Size.Width),
            MapHeight             = Map(cfg.MapSettings.Size.Height),
            IncrementalAreaEnabled = cfg.MapSettings.Size.IncrementalArea?.IsEnabled ?? false,
            IncrementalAreaStep    = cfg.MapSettings.Size.IncrementalArea?.Step ?? 0,
            BlocksPercent         = Map(cfg.MapSettings.ElementsPercentages.BlocksPercent),
            PercentOfEnemies      = Map(cfg.MapSettings.ElementsPercentages.PercentOfEnemies),
            HeroSpeed             = Map(cfg.Hero.Speed),
            HeroSightRange        = Map(cfg.Hero.SightRange),
            HeroStamina           = Map(cfg.Hero.Stamina),
            EnemySpeed            = Map(cfg.Enemy.Speed),
            EnemySightRange       = Map(cfg.Enemy.SightRange),
            EnemyStamina          = Map(cfg.Enemy.Stamina),
        };
    }

    /// <summary>
    /// Returns the <see cref="IncrementalRange"/> for the given property name, or <see langword="null"/>
    /// if the name is not recognised.
    /// </summary>
    private static IncrementalRange? GetRange(SandBoxConfiguration cfg, string propertyName)
        => propertyName switch
        {
            IncrementalPropertyNames.MaxTurns        => cfg.MaxTurns,
            IncrementalPropertyNames.MapWidth         => cfg.MapSettings.Size.Width,
            IncrementalPropertyNames.MapHeight        => cfg.MapSettings.Size.Height,
            IncrementalPropertyNames.BlocksPercent    => cfg.MapSettings.ElementsPercentages.BlocksPercent,
            IncrementalPropertyNames.PercentOfEnemies => cfg.MapSettings.ElementsPercentages.PercentOfEnemies,
            IncrementalPropertyNames.HeroSpeed        => cfg.Hero.Speed,
            IncrementalPropertyNames.HeroSightRange   => cfg.Hero.SightRange,
            IncrementalPropertyNames.HeroStamina      => cfg.Hero.Stamina,
            IncrementalPropertyNames.EnemySpeed       => cfg.Enemy.Speed,
            IncrementalPropertyNames.EnemySightRange  => cfg.Enemy.SightRange,
            IncrementalPropertyNames.EnemyStamina     => cfg.Enemy.Stamina,
            _                                         => null
        };

    /// <summary>
    /// Returns a shallow copy of <paramref name="config"/> where the named property's
    /// <see cref="IncrementalRange.Current"/> is replaced with <paramref name="value"/>.
    /// All other property references remain unchanged.
    /// </summary>
    private static SandBoxConfiguration WithPropertyOverride(SandBoxConfiguration config, string propertyName, int value)
    {
        // Shallow-copy the top-level class
        var updated = new SandBoxConfiguration
        {
            MaxTurns            = config.MaxTurns,
            TurnTimeout         = config.TurnTimeout,
            SaveToFileRegularity= config.SaveToFileRegularity,
            IsDebugMode         = config.IsDebugMode,
            MapSettings         = config.MapSettings,
            Hero                = config.Hero,
            Enemy               = config.Enemy,
        };

        switch (propertyName)
        {
            case IncrementalPropertyNames.MaxTurns:
                updated.MaxTurns = config.MaxTurns.WithCurrent(value);
                break;

            case IncrementalPropertyNames.MapWidth:
            {
                var ms = updated.MapSettings;
                var sz = ms.Size;
                sz.Width = sz.Width.WithCurrent(value);
                ms.Size = sz;
                updated.MapSettings = ms;
                break;
            }

            case IncrementalPropertyNames.MapHeight:
            {
                var ms = updated.MapSettings;
                var sz = ms.Size;
                sz.Height = sz.Height.WithCurrent(value);
                ms.Size = sz;
                updated.MapSettings = ms;
                break;
            }

            case IncrementalPropertyNames.BlocksPercent:
            {
                var ms = updated.MapSettings;
                var ep = ms.ElementsPercentages;
                ep.BlocksPercent = ep.BlocksPercent.WithCurrent(value);
                ms.ElementsPercentages = ep;
                updated.MapSettings = ms;
                break;
            }

            case IncrementalPropertyNames.PercentOfEnemies:
            {
                var ms = updated.MapSettings;
                var ep = ms.ElementsPercentages;
                ep.PercentOfEnemies = ep.PercentOfEnemies.WithCurrent(value);
                ms.ElementsPercentages = ep;
                updated.MapSettings = ms;
                break;
            }

            case IncrementalPropertyNames.HeroSpeed:
            {
                var hero = updated.Hero;
                hero.Speed = hero.Speed.WithCurrent(value);
                updated.Hero = hero;
                break;
            }

            case IncrementalPropertyNames.HeroSightRange:
            {
                var hero = updated.Hero;
                hero.SightRange = hero.SightRange.WithCurrent(value);
                updated.Hero = hero;
                break;
            }

            case IncrementalPropertyNames.HeroStamina:
            {
                var hero = updated.Hero;
                hero.Stamina = hero.Stamina.WithCurrent(value);
                updated.Hero = hero;
                break;
            }

            case IncrementalPropertyNames.EnemySpeed:
            {
                var enemy = updated.Enemy;
                enemy.Speed = enemy.Speed.WithCurrent(value);
                updated.Enemy = enemy;
                break;
            }

            case IncrementalPropertyNames.EnemySightRange:
            {
                var enemy = updated.Enemy;
                enemy.SightRange = enemy.SightRange.WithCurrent(value);
                updated.Enemy = enemy;
                break;
            }

            case IncrementalPropertyNames.EnemyStamina:
            {
                var enemy = updated.Enemy;
                enemy.Stamina = enemy.Stamina.WithCurrent(value);
                updated.Enemy = enemy;
                break;
            }
        }

        return updated;
    }

    /// <summary>
    /// Returns a shallow copy of <paramref name="config"/> where both
    /// <see cref="IncrementalRange.Current"/> of Width and Height are overridden simultaneously,
    /// preserving the <see cref="IncrementalArea"/> settings unchanged.
    /// </summary>
    private static SandBoxConfiguration WithAreaOverride(SandBoxConfiguration config, int width, int height)
    {
        var updated = new SandBoxConfiguration
        {
            MaxTurns             = config.MaxTurns,
            TurnTimeout          = config.TurnTimeout,
            SaveToFileRegularity = config.SaveToFileRegularity,
            IsDebugMode          = config.IsDebugMode,
            MapSettings          = config.MapSettings,
            Hero                 = config.Hero,
            Enemy                = config.Enemy,
        };

        var ms = updated.MapSettings;
        var sz = ms.Size;
        sz.Width  = sz.Width.WithCurrent(width);
        sz.Height = sz.Height.WithCurrent(height);
        ms.Size   = sz;
        updated.MapSettings = ms;

        return updated;
    }
}

