using AuxiliumLab.AiSandbox.AiTrainingOrchestrator.Configuration;
using AuxiliumLab.AiSandbox.GrpcHost.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace AuxiliumLab.AiSandbox.GrpcHost.Configuration;

/// <summary>
/// Encapsulates all WebApplication / Kestrel / gRPC wiring required for the training mode.
/// A single <see cref="WebApplication"/> serves:
/// <list type="bullet">
///   <item>REST API (HTTP/1.1) on port 5000</item>
///   <item>gRPC gym server (HTTP/2) on port 50062</item>
/// </list>
/// Program.cs only calls <see cref="Build"/> and never touches
/// <see cref="WebApplication.CreateBuilder"/> or Kestrel directly.
/// </summary>
public static class GrpcTrainingHost
{
    /// <summary>
    /// Creates a <see cref="WebApplication"/> pre-configured for:
    /// <list type="bullet">
    ///   <item>REST controllers on port 5000 (HTTP/1.1)</item>
    ///   <item>gRPC gym server on port 50062 (HTTP/2)</item>
    /// </list>
    /// The optional <paramref name="configure"/> callback lets the caller (Startup)
    /// register its own core services without knowing about Kestrel or gRPC internals.
    /// </summary>
    /// <param name="args">Command-line arguments forwarded to <see cref="WebApplication.CreateBuilder"/>.</param>
    /// <param name="configure">
    ///   Optional callback invoked with the <see cref="WebApplicationBuilder"/> before any
    ///   gRPC-specific services are added. Use it to register domain/application services,
    ///   REST controllers, and infrastructure services.
    /// </param>
    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ── Caller registers core + presentation services ─────────────────────
        configure?.Invoke(builder);

        // ── Training-specific: settings ───────────────────────────────────────
        builder.Configuration.AddJsonFile("training-settings.json", optional: false, reloadOnChange: false);
        builder.Configuration.AddJsonFile("aggregation-settings.json", optional: true, reloadOnChange: false);
        var trainingSettings =
            builder.Configuration.GetSection("TrainingSettings").Get<TrainingSettings>()
            ?? new TrainingSettings();
        builder.Services.AddSingleton(trainingSettings);

        // ── gRPC server (Python → C# gym) on port 50062 ───────────────────────
        builder.Services.AddGrpc();

        // ── Kestrel: REST on 5000 (HTTP/1), gRPC on 50062 (HTTP/2) ───────────
        builder.WebHost.ConfigureKestrel(opts =>
        {
            // ListenAnyIP (0.0.0.0) is required for Docker container networking.
            // ListenLocalhost binds to 127.0.0.1 only and would reject all
            // container-to-container and host-to-container traffic.
            opts.ListenAnyIP(5000, lo => lo.Protocols = HttpProtocols.Http1);
            opts.ListenAnyIP(50062, lo => lo.Protocols = HttpProtocols.Http2);
        });

        // ── Build and map ─────────────────────────────────────────────────────
        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "AiSandbox API v1");
            c.RoutePrefix = "swagger";
        });

        app.MapControllers();
        app.MapGrpcService<SimulationService>();

        return app;
    }
}
