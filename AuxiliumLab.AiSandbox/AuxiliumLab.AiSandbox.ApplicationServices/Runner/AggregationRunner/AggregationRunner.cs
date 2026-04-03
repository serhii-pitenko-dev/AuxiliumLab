using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Training.Dto;
using AuxiliumLab.AiSandbox.ApplicationServices.Executors;
using AuxiliumLab.AiSandbox.ApplicationServices.Trainer;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Domain.Statistics.Result;
using MassRunnerClass = AuxiliumLab.AiSandbox.ApplicationServices.Runner.MassRunner.MassRunner;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects.StartupSettings;
using AuxiliumLab.AiSandbox.Statistics.Preconditions;
using AuxiliumLab.AiSandbox.Statistics.StatisticDataManager;
using Microsoft.Extensions.DependencyInjection;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Runner.AggregationRunner;

/// <summary>
/// Describes a single job within an aggregation run.
/// </summary>
/// <param name="Name">Human-readable label shown in the report (e.g. "Random AI", "PPO - AI").</param>
/// <param name="Mode">The <see cref="ExecutionMode"/> that governs how this step runs.</param>
public record AggregationStepConfiguration(string Name, ExecutionMode Mode);

/// <summary>
/// Runs a configurable sequence of jobs (<see cref="ExecutionMode.Training"/>,
/// <see cref="ExecutionMode.MassRandomAISimulation"/>, <see cref="ExecutionMode.MassTrainedAISimulation"/>)
/// one after another, then produces a combined CSV report comparing all run types.
/// </summary>
public class AggregationRunner
{
    private readonly IServiceProvider          _serviceProvider;
    private readonly Sb3AlgorithmTypeProvider  _algorithmTypeProvider;
    private readonly IPolicyTrainerClient      _policyTrainerClient;
    private readonly GymBrokerRegistry         _gymBrokerRegistry;
    private readonly IStatisticFileDataManager _statisticFileManager;
    private readonly SandBoxConfiguration      _sandboxConfig;
    private readonly string                    _algorithmsFolderPath;

    public AggregationRunner(
        IServiceProvider          serviceProvider,
        Sb3AlgorithmTypeProvider  algorithmTypeProvider,
        IPolicyTrainerClient      policyTrainerClient,
        GymBrokerRegistry         gymBrokerRegistry,
        IStatisticFileDataManager statisticFileManager,
        SandBoxConfiguration      sandboxConfig,
        string algorithmsFolderPath)
    {
        _serviceProvider       = serviceProvider;
        _algorithmTypeProvider = algorithmTypeProvider;
        _policyTrainerClient   = policyTrainerClient;
        _gymBrokerRegistry     = gymBrokerRegistry;
        _statisticFileManager  = statisticFileManager;
        _sandboxConfig         = sandboxConfig;
        _algorithmsFolderPath  = algorithmsFolderPath;
    }

    /// <summary>
    /// Executes all steps sequentially and saves the aggregation report CSV.
    /// </summary>
    /// <param name="steps">Ordered list of steps to execute.</param>
    /// <param name="standardSimCount">Baseline simulation count for mass run steps.</param>
    /// <param name="incrementalProperties">Incremental sweep settings for mass run steps.</param>
    /// <param name="algorithmType">RL algorithm used for Training and MassTrained steps.</param>
    /// <param name="policyType">Policy type (MLP etc.) used for inference.</param>
    /// <param name="trainingOverrides">Optional hyperparameter overrides forwarded to the Training step.</param>
    /// <param name="cancellationToken">Cancellation support for the training step.</param>
    public async Task RunAggregationAsync(
        IReadOnlyList<AggregationStepConfiguration> steps,
        int standardSimCount,
        SimulationIncrementalPropertiesSettings incrementalProperties,
        ModelType algorithmType,
        AiPolicy policyType,
        StartPpoTrainingCommand trainingOverrides,
        CancellationToken cancellationToken = default)
    {
        var startDate   = DateTime.Now;
        var stepResults = new List<AggregationStepResult>();

        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                   AGGREGATION RUN STARTED                    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine($"Steps: {string.Join(" → ", steps.Select(s => s.Name))}");
        Console.WriteLine();

        TrainingRunInfo? lastTrainingInfo = null;

        foreach (var step in steps)
        {
            Console.WriteLine($"▶  Step: {step.Name}  ({step.Mode})");
            Console.WriteLine(new string('─', 64));

            switch (step.Mode)
            {
                // ── Training ────────────────────────────────────────────────────
                case ExecutionMode.Training:
                {
                    var trainingRunner = new TrainingRunner(
                        _serviceProvider,
                        _algorithmTypeProvider,
                        _policyTrainerClient,
                        _gymBrokerRegistry);

                    lastTrainingInfo = await trainingRunner.RunTrainingAsync(algorithmType, trainingOverrides, sandboxConfig: _sandboxConfig, cancellationToken: cancellationToken);

                    stepResults.Add(new AggregationStepResult(
                        step.Name,
                        step.Mode.ToString(),
                        lastTrainingInfo,
                        MassRunResult: null));
                    break;
                }

                // ── Mass Random AI ───────────────────────────────────────────────
                case ExecutionMode.MassRandomAISimulation:
                {
                    var result = await RunMassStepAsync(
                        step,
                        standardSimCount,
                        incrementalProperties);

                    stepResults.Add(new AggregationStepResult(
                        step.Name,
                        step.Mode.ToString(),
                        TrainingInfo: null,
                        MassRunResult: result));
                    break;
                }

                // ── Mass Trained AI ──────────────────────────────────────────────
                case ExecutionMode.MassTrainedAISimulation:
                {
                    // Auto-discover the latest trained model for this algorithm.
                    string algorithmFolder = Path.Combine(_algorithmsFolderPath, algorithmType.ToString());
                    string modelPath = Directory.Exists(algorithmFolder)
                        ? Directory.GetFiles(algorithmFolder, "*", SearchOption.AllDirectories)
                            .OrderByDescending(File.GetLastWriteTime)
                            .FirstOrDefault(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) ?? string.Empty
                        : string.Empty;

                    if (string.IsNullOrEmpty(modelPath))
                        Console.WriteLine($"  [WARNING] No trained model found in '{algorithmFolder}'. " +
                                          "MassTrained step will fall back to Random AI.");

                    var aiConfig = new AiConfiguration
                    {
                        ModelType  = algorithmType,
                        Version    = "1.0",
                        PolicyType = policyType
                    };

                    // Build the executor creator when a model path is available;
                    // fall back to RandomActions when no trained model exists.
                    // For aggregation, we default to training the Hero agent.
                    Func<IExecutorFactory, IStandardExecutor>? createInferenceExecutor =
                        string.IsNullOrEmpty(modelPath)
                            ? null
                            : factory =>
                            {
                                var heroSpec = new AgentAiSpec
                                {
                                    ModelType = algorithmType,
                                    ModelPath = modelPath,
                                    AiConfig  = aiConfig
                                };
                                return factory.CreateStandardExecutor(
                                    _sandboxConfig, heroSpec, AgentAiSpec.Random());
                            };

                    var result = await RunMassStepAsync(
                        step,
                        standardSimCount,
                        incrementalProperties,
                        createExecutorOverride: createInferenceExecutor);

                    stepResults.Add(new AggregationStepResult(
                        step.Name,
                        step.Mode.ToString(),
                        TrainingInfo: null,
                        MassRunResult: result));
                    break;
                }

                default:
                    Console.WriteLine($"  [SKIPPED] Mode '{step.Mode}' is not supported in aggregation runs.");
                    break;
            }

            Console.WriteLine($"✔  Step '{step.Name}' completed.");
            Console.WriteLine();
        }

        // Save the aggregation report.
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("AGGREGATION RUN - Saving combined report...");
        string reportPath = await _statisticFileManager.SaveAggregationReportAsync(stepResults, startDate);
        Console.WriteLine($"Report saved to: {reportPath}");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a scope, builds a <see cref="MassRunner.MassRunner"/>, and runs it,
    /// returning the captured results.
    /// </summary>
    private async Task<MassRunCapturedResult> RunMassStepAsync(
        AggregationStepConfiguration step,
        int standardSimCount,
        SimulationIncrementalPropertiesSettings incrementalProperties,
        Func<IExecutorFactory, IStandardExecutor>? createExecutorOverride = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var executorFactory  = scope.ServiceProvider.GetRequiredService<IExecutorFactory>();
        var batchFileManager = scope.ServiceProvider.GetRequiredService<IFileDataManager<GeneralBatchRunInformation>>();

        Func<IStandardExecutor> createExecutor = createExecutorOverride is not null
            ? () => createExecutorOverride(executorFactory)
            : () => executorFactory.CreateStandardExecutor(_sandboxConfig, AgentAiSpec.Random(), AgentAiSpec.Random());

        var simulationSettings = new SimulationStartupSettings
        {
            PolicyType              = string.Empty,
            ExecutionMode           = step.Mode.ToString(),
            StandardSimulationCount = standardSimCount,
            IncrementalProperties   = new SimulationIncrementalPropertiesSettings
            {
                SimulationCount = incrementalProperties.SimulationCount,
                Properties      = [..incrementalProperties.Properties],
            },
        };

        var massRunner = new MassRunnerClass(batchFileManager, _statisticFileManager, _sandboxConfig);
        return await massRunner.RunManyAsync(createExecutor, standardSimCount, startupSettings: simulationSettings);
    }
}
