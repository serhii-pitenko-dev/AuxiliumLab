using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.Startup.Configuration;

namespace AuxiliumLab.AiSandbox.Startup.Menu;

internal interface IMenuRunner
{
    /// <summary>
    /// Interactively resolves startup settings from user input.
    /// </summary>
    /// <param name="defaults">Default settings from appsettings.json.</param>
    /// <returns>Resolved settings and the selected algorithm (null unless Training was chosen).</returns>
    (StartupSettings Settings, ModelType? SelectedAlgorithm) ResolveSettings(StartupSettings defaults);
}
