using AuxiliumLab.AiSandbox.WebApi.Features.Simulation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json.Serialization;

namespace AuxiliumLab.AiSandbox.WebApi.Configuration;

public static class WebApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers REST API presentation services: controllers (with optional additional assembly),
    /// OpenAPI / Swagger document generation, endpoint explorer, SignalR, and CORS for the Blazor frontend.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="additionalControllerAssembly">
    ///   Optional extra assembly to scan for controllers (e.g. the WebApi feature assembly
    ///   when the host is the GrpcHost and controllers live in a separate project).
    /// </param>
    public static IServiceCollection AddWebApiPresentationServices(
        this IServiceCollection services,
        Assembly? additionalControllerAssembly = null)
    {
        var mvcBuilder = services.AddControllers()
            .AddJsonOptions(o =>
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        if (additionalControllerAssembly is not null)
            mvcBuilder.AddApplicationPart(additionalControllerAssembly);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title   = "AuxiliumLab AiSandbox API",
                Version = "v1",
                Description = "REST API for training, simulation, statistics, and aggregation runs."
            });
        });

        // SignalR for real-time simulation visualization
        services.AddSignalR();
        services.AddSingleton<ISimulationHubNotifier, SimulationHubNotifier>();

        // CORS — allow the Blazor WebAssembly frontend to connect
        services.AddCors(options =>
            options.AddPolicy("BlazorFrontend", policy =>
                policy.WithOrigins(
                        "http://localhost:5001",
                        "https://localhost:5001",
                        "http://localhost:7001",
                        "https://localhost:7001",
                        // Docker: frontend container is mapped to host port 8080
                        "http://localhost:8080")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()));

        return services;
    }
}

