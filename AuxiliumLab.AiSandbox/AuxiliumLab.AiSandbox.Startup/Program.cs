using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.ApplicationServices.Configuration;
using AuxiliumLab.AiSandbox.Common.Extensions;
using AuxiliumLab.AiSandbox.Domain.Configuration;
using AuxiliumLab.AiSandbox.GrpcHost.Configuration;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects.StartupSettings;
using AuxiliumLab.AiSandbox.WebApi.Configuration;
using AuxiliumLab.AiSandbox.WebApi.Features.Simulation;
using AuxiliumLab.AiSandbox.WebApi.Features.Training;
using Microsoft.Extensions.Configuration;

// ── 1. Read file-storage settings early to create required directories ────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json",          optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var fileSourceCfg = configuration
    .GetSection(FileSourceConfiguration.SectionName)
    .Get<FileSourceConfiguration>() ?? new FileSourceConfiguration();

EnsureStorageFolders(fileSourceCfg);

// ── 2. Build unified host (REST on 5000 + gRPC gym on 50062) ─────────────────
var host = GrpcTrainingHost.Build(args, builder =>
{
    builder.Services.AddEventAggregator();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddPolicyTrainerClient(builder.Configuration);
    builder.Services.AddDomainServices();
    builder.Services.AddAiSandboxServices(ExecutionMode.Training);
    builder.Services.AddApplicationServices();
    builder.Services.AddWebApiPresentationServices(typeof(TrainingController).Assembly);
});

// ── 3. Map real-time hub (must be after Build, before Run) ────────────────────
host.MapHub<SimulationHub>("/hubs/simulation");

// ── 4. Run ────────────────────────────────────────────────────────────────────
await host.RunAsync();

// ── Helper: create required storage directories if missing ───────────────────
static void EnsureStorageFolders(FileSourceConfiguration fileSourceCfg)
{
    var basePath = fileSourceCfg.FileStorage?.BasePath ?? string.Empty;
    if (string.IsNullOrWhiteSpace(basePath))
    {
        Console.WriteLine("[WARNING] FileSource.FileStorage.BasePath is empty; skipping creation of storage folders.");
        return;
    }

    var storage = fileSourceCfg.FileStorage!;
    var root = Path.GetPathRoot(basePath) ?? string.Empty;
    if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        throw new DirectoryNotFoundException(
            $"Drive or root '{root}' for FileSource.FileStorage.BasePath '{basePath}' does not exist.");

    var folders = new[]
    {
        basePath,
        Path.Combine(basePath, storage.TrainedAlgorithms),
        Path.Combine(basePath, storage.PrecreatedPlaygrounds),
        Path.Combine(basePath, storage.SavedSimulations),
    };

    foreach (var folder in folders)
    {
        try
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Could not create directory '{folder}': {ex.Message}");
        }
    }
}
