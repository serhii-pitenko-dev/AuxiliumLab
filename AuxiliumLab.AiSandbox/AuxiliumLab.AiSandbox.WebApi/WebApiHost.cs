using AuxiliumLab.AiSandbox.WebApi.Configuration;
using AuxiliumLab.AiSandbox.WebApi.Features.Simulation;

namespace AuxiliumLab.AiSandbox.WebApi;

public static class WebApiHost
{
    /// <summary>
    /// Builds and runs the WebApi application. Designed to be awaited as a background task
    /// from the Startup project when <c>IsWebApiEnabled</c> is <see langword="true"/>.
    /// </summary>
    public static async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ── Services ──────────────────────────────────────────────────────────
        builder.Services.AddWebApiPresentationServices();

        // ── Build ─────────────────────────────────────────────────────────────
        var app = builder.Build();

        app.UseCors("BlazorFrontend");

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();
        app.MapHub<SimulationHub>("/hubs/simulation");

        await app.RunAsync(cancellationToken);
    }
}
