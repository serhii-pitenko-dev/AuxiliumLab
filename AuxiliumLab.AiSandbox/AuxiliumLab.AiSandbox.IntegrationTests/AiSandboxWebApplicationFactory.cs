using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.GrpcHost.Services;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration;

namespace AuxiliumLab.AiSandbox.IntegrationTests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TProgram}"/> that overrides file storage
/// with a temporary directory so integration tests don't require a specific drive/path.
/// Also starts a real gRPC Kestrel listener on port 50062 so the Python RL service
/// can call back into the C# gym service during training integration tests.
/// </summary>
public sealed class AiSandboxWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _tempStoragePath = Path.Combine(Path.GetTempPath(), $"AiSandboxTests_{Guid.NewGuid():N}");
    private WebApplication? _gymGrpcApp;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        EnsureTempFolders();

        builder.ConfigureServices(services =>
        {
            // Override file storage configuration with the temp path.
            services.PostConfigure<FileSourceConfiguration>(cfg =>
            {
                cfg.FileStorage.BasePath              = _tempStoragePath;
                cfg.FileStorage.TrainedAlgorithms     = "trained";
                cfg.FileStorage.PrecreatedPlaygrounds = "playgrounds";
                cfg.FileStorage.SavedSimulations      = "simulations";
            });
        });

        var testServerHost = base.CreateHost(builder);

        // WebApplicationFactory uses TestServer (in-process) which does NOT bind
        // real TCP ports. The Python RL service needs to call back on port 50062
        // to drive gym Reset/Step/Close operations. Start a real Kestrel listener
        // on that port, sharing the singleton GymBrokerRegistry from the test server
        // so that gym-id routing works correctly.
        var gymBrokerRegistry = testServerHost.Services.GetRequiredService<GymBrokerRegistry>();

        var gymAppBuilder = WebApplication.CreateBuilder([]);
        gymAppBuilder.WebHost.ConfigureKestrel(opts =>
        {
            opts.ListenLocalhost(50062, lo => lo.Protocols = HttpProtocols.Http2);
        });
        gymAppBuilder.Services.AddGrpc();
        gymAppBuilder.Services.AddSingleton(gymBrokerRegistry);

        _gymGrpcApp = gymAppBuilder.Build();
        _gymGrpcApp.MapGrpcService<SimulationService>();
        _gymGrpcApp.StartAsync().GetAwaiter().GetResult();

        return testServerHost;
    }

    /// <summary>
    /// Removes all artifact files written during a test (trained models, simulation CSVs/JSONs,
    /// aggregation reports etc.) while keeping the directory tree intact so that singleton
    /// services that cached folder paths (e.g. <c>StatisticFileDataManager</c>) continue
    /// to work correctly in the next test of the same class.
    /// </summary>
    public void CleanArtifacts()
    {
        if (!Directory.Exists(_tempStoragePath))
            return;

        foreach (var file in Directory.GetFiles(_tempStoragePath, "*", SearchOption.AllDirectories))
        {
            try { File.Delete(file); }
            catch { /* best-effort */ }
        }
    }

    private void EnsureTempFolders()
    {
        Directory.CreateDirectory(Path.Combine(_tempStoragePath, "trained"));
        Directory.CreateDirectory(Path.Combine(_tempStoragePath, "playgrounds"));
        Directory.CreateDirectory(Path.Combine(_tempStoragePath, "simulations"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                _gymGrpcApp?.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                _gymGrpcApp?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _gymGrpcApp = null;
            }
            catch { /* best-effort */ }

            if (Directory.Exists(_tempStoragePath))
            {
                try { Directory.Delete(_tempStoragePath, recursive: true); }
                catch { /* best-effort cleanup */ }
            }
        }
        base.Dispose(disposing);
    }
}


