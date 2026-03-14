using AuxiliumLab.AiSandbox.ConsolePresentation;
using AuxiliumLab.AiSandbox.ConsolePresentation.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuxiliumLab.AiSandbox.ConsolePresentation.Configuration;

public static class ConsolePresentationServiceCollectionExtensions
{
    public static void AddConsoleConfigurationFile(this IConfigurationBuilder configurationBuilder)
    {
        var assemblyLocation = Path.GetDirectoryName(
            typeof(ConsolePresentationServiceCollectionExtensions).Assembly.Location)!;
        configurationBuilder.AddJsonFile(
            Path.Combine(assemblyLocation, "Settings.json"), optional: false, reloadOnChange: true);
    }

    /// <summary>
    /// Registers console presentation services. Settings.json must already have been
    /// added to configuration before calling this overload (e.g. via
    /// <c>Host.CreateDefaultBuilder(...).ConfigureAppConfiguration(...)</c>).
    /// </summary>
    public static IServiceCollection AddConsolePresentationServices
        (this IServiceCollection services,
         IConfiguration configuration)
    {
        services.Configure<ConsoleSettings>(configuration.GetSection("ConsoleSettings"));
        services.AddSingleton<IConsoleRunner, ConsoleRunner>();
        return services;
    }
}