using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.AggregationRun;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground.CreatePlayground;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Training;
using AuxiliumLab.AiSandbox.ApplicationServices.Executors;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.AggregationRun;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetAffectedCells;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Map.GetMapLayout;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation.Maps;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Statistic;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Training;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto.Performance;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.TestPreconditionSet;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.Mappers;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;
using AuxiliumLab.AiSandbox.Domain.Statistics.Result;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.Statistics.StatisticDataManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Configuration;

public static class ApplicationServicesCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // ── Simulation Map Commands ──────────────────────────────────────────
        services.AddScoped<ICreatePlaygroundCommandHandler, CreatePlaygroundCommandHandler>();
        services.AddScoped<IPlaygroundCommandsHandleService, PlaygroundCommandsHandleService>();

        // ── Simulation Map Queries ───────────────────────────────────────────
        services.AddScoped<IMapQueriesHandleService, MapQueriesHandleService>();
        services.AddScoped<IMapLayout, GetMapLayoutHandle>();
        services.AddScoped<IAffectedCells, GetAffectedCellsHandle>();

        // ── File data managers ───────────────────────────────────────────────
        services.AddSingleton<IFileDataManager<MapLayoutResponse>, FileDataManager<MapLayoutResponse>>();
        services.AddSingleton<IFileDataManager<StandardPlaygroundState>, FileDataManager<StandardPlaygroundState>>();
        services.AddSingleton<IFileDataManager<RawDataLog>, FileDataManager<RawDataLog>>();
        services.AddSingleton<IFileDataManager<GeneralBatchRunInformation>, FileDataManager<GeneralBatchRunInformation>>();

        #if PERFORMANCE_ANALYSIS
            #if PERFORMANCE_DETAILED_ANALYSIS
                services.AddSingleton<IFileDataManager<TurnExecutionPerformance>, FileDataManager<TurnExecutionPerformance>>();
            #endif
                services.AddSingleton<IFileDataManager<SandboxExecutionPerformance>, FileDataManager<SandboxExecutionPerformance>>();
        #else
                services.AddSingleton<IFileDataManager<TurnExecutionPerformance>, NullFileDataManager<TurnExecutionPerformance>>();
                services.AddSingleton<IFileDataManager<SandboxExecutionPerformance>, NullFileDataManager<SandboxExecutionPerformance>>();
        #endif

        // ── Statistic file manager ───────────────────────────────────────────
        services.AddSingleton<IStatisticFileDataManager>(sp =>
        {
            var fileConfig   = sp.GetRequiredService<IOptions<FileSourceConfiguration>>();
            string statsRoot = Path.Combine(
                fileConfig.Value.FileStorage.BasePath,
                fileConfig.Value.FileStorage.SavedSimulations);
            return new StatisticFileDataManager(statsRoot);
        });

        // ── Domain utilities ─────────────────────────────────────────────────
        services.AddSingleton<IStandardPlaygroundMapper, StandardPlaygroundMapper>();
        services.AddTransient<IExecutorFactory, ExecutorFactory>();
        services.AddTransient<IExecutorForPresentation>(
            sp => sp.GetRequiredService<IExecutorFactory>().CreateExecutorForPresentation());
        // Note: DI-resolved IExecutorForPresentation uses default ActionDelayMs=0 and PauseGate=null.
        // For customized values, use IExecutorFactory.CreateExecutorForPresentation(actionDelayMs, pauseGate).
        services.AddTransient<IStandardExecutor>(
            sp => sp.GetRequiredService<IExecutorFactory>().CreateStandardExecutor());
        services.AddSingleton<ITestPreconditionData, TestPreconditionData>();

        // ── Feature command services (singleton: own background-job state) ────
        services.AddSingleton<TrainingCommandService>();
        services.AddSingleton<ITrainingCommands>(sp => sp.GetRequiredService<TrainingCommandService>());

        services.AddSingleton<SimulationCommandService>();
        services.AddSingleton<ISimulationCommands>(sp => sp.GetRequiredService<SimulationCommandService>());

        services.AddSingleton<AggregationRunCommandService>();
        services.AddSingleton<IAggregationRunCommands>(sp => sp.GetRequiredService<AggregationRunCommandService>());

        // ── Feature query services ───────────────────────────────────────────
        services.AddSingleton<ITrainingQueries, TrainingQueryService>();
        services.AddSingleton<ISimulationQueries, SimulationQueryService>();
        services.AddSingleton<IAggregationRunQueries, AggregationRunQueryService>();
        services.AddSingleton<IStatisticQueries, StatisticQueryService>();

        return services;
    }
}

