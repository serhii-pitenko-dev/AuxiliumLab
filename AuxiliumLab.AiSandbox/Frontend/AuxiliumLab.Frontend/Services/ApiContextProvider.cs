using AuxiliumLab.Frontend.Configuration;
using Microsoft.Extensions.Options;

namespace AuxiliumLab.Frontend.Services;

/// <summary>
/// Reads the backend base URLs from <see cref="ApiSettings"/> (wwwroot/appsettings.json).
/// To switch domain context in the future simply change <see cref="ActiveContext"/>
/// or provide a derived implementation — all feature API clients only depend on
/// <see cref="IApiContextProvider"/>, not on concrete URLs.
/// </summary>
public sealed class ApiContextProvider : IApiContextProvider
{
    private readonly ApiSettings _settings;

    public ApiContextProvider(IOptions<ApiSettings> options)
        => _settings = options.Value;

    public string ActiveContext => "ai-sandbox";

    public string AiSandboxBaseUrl => _settings.AiSandboxBaseUrl.TrimEnd('/') + "/";

    public string MarketSimulationBaseUrl => _settings.MarketSimulationBaseUrl.TrimEnd('/') + "/";

    public string SimulationHubUrl => _settings.AiSandboxBaseUrl.TrimEnd('/') + "/hubs/simulation";
}
