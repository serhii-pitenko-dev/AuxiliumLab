namespace AuxiliumLab.Frontend.Configuration;

/// <summary>Root API settings bound from wwwroot/appsettings.json → "ApiSettings".</summary>
public class ApiSettings
{
    /// <summary>Base address of the AiSandbox backend (e.g. http://localhost:5000).</summary>
    public string AiSandboxBaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>Base address of the Ai Market Simulation backend (placeholder).</summary>
    public string MarketSimulationBaseUrl { get; set; } = "http://localhost:6000";
}
