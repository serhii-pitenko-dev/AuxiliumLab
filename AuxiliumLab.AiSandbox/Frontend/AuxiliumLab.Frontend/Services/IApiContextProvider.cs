namespace AuxiliumLab.Frontend.Services;

/// <summary>
/// Provides the currently active domain context (AiSandbox, MarketSimulation, etc.)
/// and the corresponding base URLs.  Switching context only requires updating this service.
/// </summary>
public interface IApiContextProvider
{
    /// <summary>The active context identifier, e.g. "ai-sandbox".</summary>
    string ActiveContext { get; }

    /// <summary>Base URL for the AiSandbox backend REST API.</summary>
    string AiSandboxBaseUrl { get; }

    /// <summary>Base URL for the MarketSimulation backend REST API (placeholder).</summary>
    string MarketSimulationBaseUrl { get; }

    /// <summary>Base URL for the SignalR simulation hub.</summary>
    string SimulationHubUrl { get; }
}
