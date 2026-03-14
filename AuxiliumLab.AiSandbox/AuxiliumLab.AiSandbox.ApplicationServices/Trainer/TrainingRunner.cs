using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.PolicyTrainer;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Trainers;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Training.Dto;
using AuxiliumLab.AiSandbox.ApplicationServices.Executors;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Training.Dto;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.LogsDto.Performance;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.TestPreconditionSet;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.Mappers;
using AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.Sb3Contract.Commands;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.Domain.Statistics.Result;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Trainer;

public class TrainingRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TrainingSettings _trainingSettings;
    private readonly Sb3AlgorithmTypeProvider _algorithmTypeProvider;
    private readonly IPolicyTrainerClient _policyTrainerClient;
    private readonly GymBrokerRegistry _gymBrokerRegistry;

    public TrainingRunner(
        IServiceProvider serviceProvider,
        TrainingSettings trainingSettings,
        Sb3AlgorithmTypeProvider algorithmTypeProvider,
        IPolicyTrainerClient policyTrainerClient,
        GymBrokerRegistry gymBrokerRegistry)
    {
        _serviceProvider = serviceProvider;
        _trainingSettings = trainingSettings;
        _algorithmTypeProvider = algorithmTypeProvider;
        _policyTrainerClient = policyTrainerClient;
        _gymBrokerRegistry = gymBrokerRegistry;
    }

    /// <summary>
    /// Starts training with optional parameter overrides from a web request.
    /// Creates a folder structure: {BasePath}/{TrainedAlgorithms}/{Algorithm}/{ExperimentId}/
    /// containing the trained model and a preconditions.json file.
    /// </summary>
    public async Task<TrainingRunInfo> RunTrainingAsync(
        ModelType algorithmType,
        StartPpoTrainingCommand? overrides = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Find the settings for the selected algorithm (merged with overrides)
        string algorithmName = algorithmType.ToString().ToUpper();
        var baseAlgoSettings = _trainingSettings.Algorithms
            .FirstOrDefault(a => a.Algorithm.Equals(algorithmName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No training settings found for algorithm '{algorithmName}' in training-settings.json.");

        // Apply any web-request hyperparameter overrides
        var algoSettings = ApplyHyperparameterOverrides(baseAlgoSettings, overrides?.Hyperparameters);

        // Apply reward overrides
        var rewards = ApplyRewardOverrides(_trainingSettings.Rewards, overrides?.RewardSettings);

        // 2. Instantiate the correct Training class
        ITraining training = algorithmType switch
        {
            ModelType.PPO => new PpoTraining(isSameMachine: true, algoSettings),
            ModelType.A2C => new A2cTraining(isSameMachine: true, algoSettings),
            ModelType.DQN => new DqnTraining(isSameMachine: true, algoSettings),
            _ => throw new NotImplementedException($"Training for algorithm '{algorithmType}' is not implemented.")
        };

        // 3. Resolve shared singleton dependencies (must not contain per-gym mutable state)
        var playgroundRepo = _serviceProvider.GetRequiredService<IMemoryDataManager<StandardPlayground>>();
        var playgroundStateFileRepo = _serviceProvider.GetRequiredService<IFileDataManager<StandardPlaygroundState>>();
        var mapper = _serviceProvider.GetRequiredService<IStandardPlaygroundMapper>();
        var rawDataRepo = _serviceProvider.GetRequiredService<IFileDataManager<RawDataLog>>();
        var turnPerfRepo = _serviceProvider.GetRequiredService<IFileDataManager<TurnExecutionPerformance>>();
        var sbxPerfRepo = _serviceProvider.GetRequiredService<IFileDataManager<SandboxExecutionPerformance>>();
        var testPreconditionData = _serviceProvider.GetRequiredService<ITestPreconditionData>();
        var sandboxConfig = _serviceProvider.GetRequiredService<IOptions<SandBoxConfiguration>>();
        var fileSourceConfig = _serviceProvider.GetRequiredService<IOptions<FileSourceConfiguration>>();

        // Apply sandbox overrides if provided
        var effectiveSandboxConfig = ApplySandboxOverrides(sandboxConfig, overrides?.SandboxSettings);

        // 4. Create one executor + Sb3Actions pair per physical core (or the override count).
        int nEnvs = overrides?.Hyperparameters?.NEnvs.HasValue == true
            ? Math.Max(1, overrides.Hyperparameters.NEnvs.Value)
            : Math.Max(1, training.PhysicalCores);
        var executorTasks = new List<Task>();
        var gymIds = new List<Guid>();
        var gymCtsList    = new List<CancellationTokenSource>();
        var linkedCtsList = new List<CancellationTokenSource>();

        for (int i = 0; i < nEnvs; i++)
        {
            // Each executor requires a scoped IPlaygroundCommandsHandleService
            var scope = _serviceProvider.CreateScope();
            var playgroundCommands = scope.ServiceProvider
                .GetRequiredService<Commands.Simulation.Playground.IPlaygroundCommandsHandleService>();

            // Each gym gets its own isolated broker + rpc client so that:
            //   1. Events from one gym's executor cannot leak to another gym's Sb3Actions.
            //   2. There is no lock contention between gyms on a shared broker.
            var gymBroker = new MessageBroker();
            var gymRpcClient = new BrokerRpcClient(gymBroker);
            var gymAgentStateRepo = new MemoryDataManager<AgentStateForAIDecision>();

            // Create a dedicated Sb3Actions for this gym
            var sb3 = _algorithmTypeProvider.Create(
                algorithmType, gymBroker, gymAgentStateRepo,
                rewards.StepPenalty, rewards.WinReward, rewards.LossReward);

            // Track the gym's unique ID so it can be passed to Python
            gymIds.Add(sb3.GymId);

            // Register the per-gym broker so SimulationService can route Reset/Step/Close
            // to the correct Sb3Actions instance (instead of the shared singleton broker).
            _gymBrokerRegistry.Register(sb3.GymId, gymBroker);

            // Each episode gets a fresh executor so no per-episode state
            // (sandboxStatus, _playground, _agentsToAct, etc.) leaks between runs.
            // Sb3Actions stays alive for the full training session since it owns
            // the Python gRPC channel.
            StandardExecutor CreateEpisodeExecutor() => new StandardExecutor(
                playgroundCommands,
                playgroundRepo,
                sb3,
                effectiveSandboxConfig,
                playgroundStateFileRepo,
                gymAgentStateRepo,
                gymBroker,
                gymRpcClient,
                mapper,
                rawDataRepo,
                turnPerfRepo,
                sbxPerfRepo,
                testPreconditionData);

            // Set the episode callback so Sb3Actions can restart episodes
            sb3.SetEpisodeCallback(() => CreateEpisodeExecutor().RunAsync());

            // Cancel when Python closes this gym's connection (signals training complete). 
            var gymCloseCts = new CancellationTokenSource();
            var linkedCts   = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, gymCloseCts.Token);
            gymCtsList.Add(gymCloseCts);
            linkedCtsList.Add(linkedCts);

            var capturedGymId = sb3.GymId;
            gymBroker.Subscribe<RequestSimulationCloseCommand>(cmd =>
            {
                if (cmd.GymId == capturedGymId)
                    gymCloseCts.Cancel();
            });

            // Keep running until Python closes this gym (training complete) or app is stopped.
            var execTask = Task.Run(async () =>
            {
                // The first episode is started by Python calling Reset(gymId).
                // Subsequent episodes are also started via the episode callback.
                // TrainingRunner exits when Python closes all gyms after training finishes.
                try
                {
                    await Task.Delay(Timeout.Infinite, linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }, CancellationToken.None);

            executorTasks.Add(execTask);
        }

        // 5. Negotiate environment contract with Python before starting training.
        //    This replaces the old silent coupling where obs_dim was hard-coded
        //    on both sides. Any mismatch is now a hard error here.
        string experimentId = training.BuildExperimentId();
        var spec = EnvironmentSpecBuilder.Build(effectiveSandboxConfig.Value, experimentId);
        var negotiationCt = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
        NegotiateEnvironmentResponse negotiation;
        try
        {
            negotiation = await _policyTrainerClient.NegotiateEnvironmentAsync(
                new NegotiateEnvironmentRequest { ExperimentId = experimentId, Spec = spec },
                negotiationCt);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"[Training] NegotiateEnvironment RPC failed for experiment '{experimentId}': {ex.Message}", ex);
        }

        if (!negotiation.Accepted)
            throw new InvalidOperationException(
                $"[Training] Python RL service rejected environment spec for experiment '{experimentId}': "
                + negotiation.Message);

        EnvironmentSpecBuilder.AssertEchoMatches(spec, negotiation.EchoedSpec, experimentId);

        Console.WriteLine(
            $"[Training] Environment spec negotiated: obs_dim={spec.ObservationDim}, "
            + $"action_dim={spec.ActionDim}, sight_range={spec.SightRange}.");

        // 6. Start training on the Python side
        Console.WriteLine($"[Training] Starting {algorithmType} training with {nEnvs} gym(s)...");
        Console.WriteLine($"[Training] Experiment: {experimentId}");
        Console.WriteLine($"[Training] Gym IDs: {string.Join(", ", gymIds.Select(g => g.ToString("N")[..8]))}");
        await training.Run(
            _policyTrainerClient,
            gymIds,
            fileSourceConfig.Value.FileStorage.BasePath,
            fileSourceConfig.Value.FileStorage.TrainedAlgorithms);

        // 7. Wait until all gyms close (Python training complete) or app is stopped.
        try
        {
            await Task.WhenAll(executorTasks).ConfigureAwait(false);
            Console.WriteLine("[Training] All gyms closed — training complete.");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[Training] Training cancelled by application stop.");
        }
        finally
        {
            // Clean up broker registrations so stale gym IDs don't linger
            foreach (var id in gymIds)
                _gymBrokerRegistry.Unregister(id);

            // Dispose per-gym cancellation tokens
            foreach (var cts in linkedCtsList) cts.Dispose();
            foreach (var cts in gymCtsList)    cts.Dispose();
        }

        // Return training metadata so callers (e.g. AggregationRunner) can include it in reports.
        var parameterDict = algoSettings.Parameters
            .ToDictionary(p => p.Name, p => p.Value);

        // Save preconditions.json alongside the trained model folder
        await SavePreconditionsAsync(
            algorithmType,
            experimentId,
            parameterDict,
            effectiveSandboxConfig.Value,
            rewards,
            fileSourceConfig.Value);

        return new TrainingRunInfo(algorithmName, experimentId, parameterDict);
    }

    // ── Helper Methods ──────────────────────────────────────────────────────────

    /// <summary>Applies web-request hyperparameter overrides on top of settings-file defaults.</summary>
    private static TrainingAlgorithmSettings ApplyHyperparameterOverrides(
        TrainingAlgorithmSettings baseSettings,
        PpoHyperparametersDto? overrides)
    {
        if (overrides is null)
            return baseSettings;

        var merged = new TrainingAlgorithmSettings
        {
            Algorithm = baseSettings.Algorithm,
            Parameters = new List<TrainingParameter>(baseSettings.Parameters)
        };

        void SetOrAdd(string name, string? value)
        {
            if (value is null) return;
            var idx = merged.Parameters.FindIndex(p => p.Name == name);
            if (idx >= 0)
                merged.Parameters[idx] = merged.Parameters[idx] with { Value = value };
            else
                merged.Parameters.Add(new TrainingParameter(name, value));
        }

        if (overrides.TotalTimesteps.HasValue) SetOrAdd("total_timesteps", overrides.TotalTimesteps.Value.ToString());
        if (overrides.LearningRate.HasValue)   SetOrAdd("learning_rate",   overrides.LearningRate.Value.ToString("G"));
        if (overrides.NSteps.HasValue)         SetOrAdd("n_steps",         overrides.NSteps.Value.ToString());
        if (overrides.BatchSize.HasValue)      SetOrAdd("batch_size",      overrides.BatchSize.Value.ToString());
        if (overrides.NEpochs.HasValue)        SetOrAdd("n_epochs",        overrides.NEpochs.Value.ToString());
        if (overrides.Gamma.HasValue)          SetOrAdd("gamma",           overrides.Gamma.Value.ToString("G"));
        if (overrides.GaeLambda.HasValue)      SetOrAdd("gae_lambda",      overrides.GaeLambda.Value.ToString("G"));
        if (overrides.ClipRange.HasValue)      SetOrAdd("clip_range",      overrides.ClipRange.Value.ToString("G"));
        if (overrides.EntCoef.HasValue)        SetOrAdd("ent_coef",        overrides.EntCoef.Value.ToString("G"));
        if (overrides.Seed.HasValue)           SetOrAdd("seed",            overrides.Seed.Value.ToString());
        if (overrides.NEnvs.HasValue)          SetOrAdd("n_envs",          overrides.NEnvs.Value.ToString());

        return merged;
    }

    /// <summary>Applies web-request reward overrides on top of settings-file defaults.</summary>
    private static RewardSettings ApplyRewardOverrides(RewardSettings baseRewards, RewardSettingsDto? overrides)
    {
        if (overrides is null)
            return baseRewards;

        return new RewardSettings
        {
            StepPenalty = overrides.StepPenalty ?? baseRewards.StepPenalty,
            WinReward   = overrides.WinReward   ?? baseRewards.WinReward,
            LossReward  = overrides.LossReward  ?? baseRewards.LossReward
        };
    }

    /// <summary>Creates a new IOptions wrapper with sandbox overrides applied.</summary>
    private static IOptions<SandBoxConfiguration> ApplySandboxOverrides(
        IOptions<SandBoxConfiguration> original,
        TrainingSandboxSettingsDto? overrides)
    {
        if (overrides is null)
            return original;

        // Deep-clone via JSON round-trip then apply overrides
        var json = JsonSerializer.Serialize(original.Value);
        var cfg  = JsonSerializer.Deserialize<SandBoxConfiguration>(json)!;

        if (overrides.MaxTurns.HasValue) cfg.MaxTurns = cfg.MaxTurns.WithCurrent(overrides.MaxTurns.Value);

        // Structs must be copied out, mutated, then assigned back
        var mapSettings = cfg.MapSettings;
        var size = mapSettings.Size;
        if (overrides.MapWidth.HasValue)  size.Width  = size.Width.WithCurrent(overrides.MapWidth.Value);
        if (overrides.MapHeight.HasValue) size.Height = size.Height.WithCurrent(overrides.MapHeight.Value);
        mapSettings.Size = size;
        var elemPerc = mapSettings.ElementsPercentages;
        if (overrides.BlocksPercent.HasValue)  elemPerc.BlocksPercent      = elemPerc.BlocksPercent.WithCurrent((int)overrides.BlocksPercent.Value);
        if (overrides.EnemiesPercent.HasValue) elemPerc.PercentOfEnemies   = elemPerc.PercentOfEnemies.WithCurrent((int)overrides.EnemiesPercent.Value);
        mapSettings.ElementsPercentages = elemPerc;
        cfg.MapSettings = mapSettings;

        var hero = cfg.Hero;
        if (overrides.HeroSpeed.HasValue)      hero.Speed      = hero.Speed.WithCurrent(overrides.HeroSpeed.Value);
        if (overrides.HeroSightRange.HasValue) hero.SightRange = hero.SightRange.WithCurrent(overrides.HeroSightRange.Value);
        if (overrides.HeroStamina.HasValue)    hero.Stamina    = hero.Stamina.WithCurrent(overrides.HeroStamina.Value);
        cfg.Hero = hero;

        var enemy = cfg.Enemy;
        if (overrides.EnemySpeed.HasValue) enemy.Speed = enemy.Speed.WithCurrent(overrides.EnemySpeed.Value);
        cfg.Enemy = enemy;

        return Options.Create(cfg);
    }

    /// <summary>
    /// Saves preconditions.json to the experiment folder so the trained model
    /// can be inspected with the parameters used to produce it.
    /// </summary>
    private static async Task SavePreconditionsAsync(
        ModelType algorithmType,
        string experimentId,
        IReadOnlyDictionary<string, string> parameters,
        SandBoxConfiguration sandboxCfg,
        RewardSettings rewards,
        FileSourceConfiguration fileSourceCfg)
    {
        var folder = Path.Combine(
            fileSourceCfg.FileStorage.BasePath,
            fileSourceCfg.FileStorage.TrainedAlgorithms,
            algorithmType.ToString(),
            experimentId);

        try
        {
            Directory.CreateDirectory(folder);

            var preconditions = new TrainingPreconditionsDto
            {
                Algorithm      = algorithmType.ToString(),
                ExperimentId   = experimentId,
                Hyperparameters = parameters.ToDictionary(kv => kv.Key, kv => kv.Value),
                MaxTurns       = sandboxCfg.MaxTurns.Current,
                MapWidth       = sandboxCfg.MapSettings.Size.Width.Current,
                MapHeight      = sandboxCfg.MapSettings.Size.Height.Current,
                StepPenalty    = rewards.StepPenalty,
                WinReward      = rewards.WinReward,
                LossReward     = rewards.LossReward,
                StartedAt      = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(preconditions, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(folder, "preconditions.json"), json);
            Console.WriteLine($"[Training] Preconditions saved to '{folder}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Training] WARNING: Failed to save preconditions.json: {ex.Message}");
        }
    }
}
