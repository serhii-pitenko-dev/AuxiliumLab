using AuxiliumLab.AiSandbox.Ai;
using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Configuration;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.GrpcClients;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.PolicyTrainer;
using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Trainers;
using AuxiliumLab.AiSandbox.ApplicationServices.Executors;
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
    private readonly Sb3AlgorithmTypeProvider _algorithmTypeProvider;
    private readonly IPolicyTrainerClient _policyTrainerClient;
    private readonly GymBrokerRegistry _gymBrokerRegistry;

    public TrainingRunner(
        IServiceProvider serviceProvider,
        Sb3AlgorithmTypeProvider algorithmTypeProvider,
        IPolicyTrainerClient policyTrainerClient,
        GymBrokerRegistry gymBrokerRegistry)
    {
        _serviceProvider = serviceProvider;
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
        StartPpoTrainingCommand overrides,
        TrainingJobStatusDto? jobStatus = null,
        SandBoxConfiguration? sandboxConfig = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Build algorithm settings directly from the web-request DTO
        string algorithmName = algorithmType.ToString().ToUpper();
        var hp = overrides.Hyperparameters;
        var algoSettings = new TrainingAlgorithmSettings
        {
            Algorithm = algorithmName,
            Parameters =
            [
                new TrainingParameter("total_timesteps", hp.TotalTimesteps.ToString()),
                new TrainingParameter("n_envs",          hp.NEnvs.ToString()),
                new TrainingParameter("learning_rate",   hp.LearningRate.ToString("G")),
                new TrainingParameter("n_steps",         hp.NSteps.ToString()),
                new TrainingParameter("batch_size",      hp.BatchSize.ToString()),
                new TrainingParameter("n_epochs",        hp.NEpochs.ToString()),
                new TrainingParameter("gamma",           hp.Gamma.ToString("G")),
                new TrainingParameter("gae_lambda",      hp.GaeLambda.ToString("G")),
                new TrainingParameter("clip_range",      hp.ClipRange.ToString("G")),
                new TrainingParameter("ent_coef",        hp.EntCoef.ToString("G")),
                new TrainingParameter("seed",            hp.Seed.ToString()),
            ]
        };

        // Build reward settings from the web-request DTO
        var rw = overrides.RewardSettings;
        var rewards = new RewardSettings
        {
            StepPenalty = rw.StepPenalty,
            WinReward   = rw.WinReward,
            LossReward  = rw.LossReward
        };

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
        var fileSourceConfig = _serviceProvider.GetRequiredService<IOptions<FileSourceConfiguration>>();

        // Build sandbox configuration from the DTO (prefer overrides DTO, then explicit config)
        var sb = overrides.SandboxSettings;
        var effectiveSandboxConfig = sandboxConfig
            ?? SandBoxConfiguration.CreateFromValues(
                sb.MaxTurns, sb.MapWidth, sb.MapHeight,
                sb.BlocksPercent, sb.EnemiesPercent,
                sb.HeroSpeed, sb.HeroSightRange, sb.HeroStamina,
                sb.EnemySpeed, sb.EnemySightRange, sb.EnemyStamina);

        // 4. Create one executor + Sb3Actions pair per physical core (or the override count).
        int nEnvs = Math.Max(1, hp.NEnvs);
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
            sb3.SetEpisodeCallback(async () => await CreateEpisodeExecutor().RunAsync());

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

        // 5. Compute experiment identity and save preconditions BEFORE training
        //    so the folder always has diagnostic data even if training fails.
        string experimentId = training.BuildExperimentId();
        var parameterDict = algoSettings.Parameters
            .ToDictionary(p => p.Name, p => p.Value);

        // Update job status with the real experiment id immediately
        if (jobStatus is not null)
            jobStatus.ExperimentId = experimentId;

        await SavePreconditionsAsync(
            algorithmType,
            experimentId,
            parameterDict,
            effectiveSandboxConfig,
            rewards,
            fileSourceConfig.Value);

        // 6. Negotiate environment contract with Python before starting training.
        //    This replaces the old silent coupling where obs_dim was hard-coded
        //    on both sides. Any mismatch is now a hard error here.
        var spec = EnvironmentSpecBuilder.Build(effectiveSandboxConfig, experimentId);
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

        // 7. Start training on the Python side
        Console.WriteLine($"[Training] Starting {algorithmType} training with {nEnvs} gym(s)...");
        Console.WriteLine($"[Training] Experiment: {experimentId}");
        Console.WriteLine($"[Training] Gym IDs: {string.Join(", ", gymIds.Select(g => g.ToString("N")[..8]))}");
        var runId = await training.Run(
            _policyTrainerClient,
            gymIds,
            fileSourceConfig.Value.FileStorage.BasePath,
            fileSourceConfig.Value.FileStorage.TrainedAlgorithms);

        // Populate job status with progress metadata
        if (jobStatus is not null)
        {
            jobStatus.RunId           = runId;
            jobStatus.TotalTimesteps  = training.BuildTrainingRequest(algoSettings, nEnvs, gymIds,
                fileSourceConfig.Value.FileStorage.BasePath,
                fileSourceConfig.Value.FileStorage.TrainedAlgorithms).TotalTimesteps;
            jobStatus.NumEnvironments = nEnvs;
        }

        // 7b. Poll Python for training progress while waiting for gyms to close
        using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pollTask = PollTrainingProgressAsync(runId, jobStatus, pollCts.Token);

        // 8. Wait until all gyms close (Python training complete) or app is stopped.
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
            // Stop the progress-polling loop
            await pollCts.CancelAsync();
            try { await pollTask.ConfigureAwait(false); } catch (OperationCanceledException) { }

            // Clean up broker registrations so stale gym IDs don't linger
            foreach (var id in gymIds)
                _gymBrokerRegistry.Unregister(id);

            // Dispose per-gym cancellation tokens
            foreach (var cts in linkedCtsList) cts.Dispose();
            foreach (var cts in gymCtsList)    cts.Dispose();
        }

        // 9. Verify Python-side training actually succeeded
        try
        {
            var finalStatus = await _policyTrainerClient.GetTrainingStatusAsync(
                new StatusRequest { RunId = runId }, CancellationToken.None);
            if (!string.IsNullOrEmpty(finalStatus.ErrorMessage))
                throw new InvalidOperationException(
                    $"[Training] Python RL service reported failure: {finalStatus.ErrorMessage}");
            if (finalStatus.TimestepsDone == 0)
                throw new InvalidOperationException(
                    "[Training] Training completed 0 timesteps — the simulation environment likely failed to start.");
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[Training] WARNING: Could not verify Python training status: {ex.Message}");
        }

        // Return training metadata so callers (e.g. AggregationRunner) can include it in reports.
        return new TrainingRunInfo(algorithmName, experimentId, parameterDict);
    }

    // ── Helper Methods ──────────────────────────────────────────────────────────

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
                Algorithm       = algorithmType.ToString(),
                ExperimentId    = experimentId,
                Hyperparameters = parameters.ToDictionary(kv => kv.Key, kv => kv.Value),
                MaxTurns        = sandboxCfg.MaxTurns.Current,
                MapWidth        = sandboxCfg.MapSettings.Size.Width.Current,
                MapHeight       = sandboxCfg.MapSettings.Size.Height.Current,
                BlocksPercent   = sandboxCfg.MapSettings.ElementsPercentages.BlocksPercent.Current,
                EnemiesPercent  = sandboxCfg.MapSettings.ElementsPercentages.PercentOfEnemies.Current,
                HeroSpeed       = sandboxCfg.Hero.Speed.Current,
                HeroSightRange  = sandboxCfg.Hero.SightRange.Current,
                HeroStamina     = sandboxCfg.Hero.Stamina.Current,
                EnemySpeed      = sandboxCfg.Enemy.Speed.Current,
                EnemySightRange = sandboxCfg.Enemy.SightRange.Current,
                EnemyStamina    = sandboxCfg.Enemy.Stamina.Current,
                StepPenalty     = rewards.StepPenalty,
                WinReward       = rewards.WinReward,
                LossReward      = rewards.LossReward,
                StartedAt       = DateTime.UtcNow
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

    /// <summary>
    /// Saves error.json to the experiment folder so failures can be investigated later.
    /// </summary>
    internal static async Task SaveErrorAsync(
        string algorithm,
        string experimentId,
        string errorMessage,
        FileSourceConfiguration fileSourceCfg)
    {
        var folder = Path.Combine(
            fileSourceCfg.FileStorage.BasePath,
            fileSourceCfg.FileStorage.TrainedAlgorithms,
            algorithm,
            experimentId);

        try
        {
            Directory.CreateDirectory(folder);

            var errorInfo = new Dictionary<string, object>
            {
                ["Algorithm"] = algorithm,
                ["ExperimentId"] = experimentId,
                ["ErrorMessage"] = errorMessage,
                ["FailedAt"] = DateTime.UtcNow.ToString("o")
            };

            var json = JsonSerializer.Serialize(errorInfo, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(folder, "error.json"), json);
            Console.WriteLine($"[Training] Error info saved to '{folder}/error.json'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Training] WARNING: Failed to save error.json: {ex.Message}");
        }
    }

    /// <summary>
    /// Polls Python's GetTrainingStatus RPC every 3 seconds and updates the job status DTO.
    /// Runs until cancelled (when training completes or is stopped).
    /// </summary>
    private async Task PollTrainingProgressAsync(
        string runId, TrainingJobStatusDto? jobStatus, CancellationToken ct)
    {
        if (jobStatus is null || string.IsNullOrEmpty(runId)) return;

        var request = new StatusRequest { RunId = runId };
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), ct).ConfigureAwait(false);
                var response = await _policyTrainerClient.GetTrainingStatusAsync(request, ct);
                jobStatus.TimestepsDone = response.TimestepsDone;
                if (response.TotalTimesteps > 0)
                    jobStatus.TotalTimesteps = response.TotalTimesteps;
                if (response.NumEnvs > 0)
                    jobStatus.NumEnvironments = response.NumEnvs;
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"[Training] Progress poll failed: {ex.Message}");
            }
        }
    }
}
