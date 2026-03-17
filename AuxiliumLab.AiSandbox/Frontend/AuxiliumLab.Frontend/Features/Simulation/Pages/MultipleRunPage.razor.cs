using AuxiliumLab.Frontend.Features.Simulation.Dto;

namespace AuxiliumLab.Frontend.Features.Simulation.Pages;

public partial class MultipleRunPage
{
    private StartMassSimulationCommand _cmd = new()
    {
        Kind            = SimulationKind.RandomAI,
        SimulationCount = 100,
        Algorithm       = ModelType.PPO
    };

    private SimulationSandboxOverrideDto _sandboxOverride = new();
    private bool _loading;
    private SimulationJobStartedDto? _started;
    private string? _error;

    private void OnOverrideChanged(SimulationSandboxOverrideDto dto)
    {
        _sandboxOverride = dto;
        _cmd.SandboxSettings = dto;
    }

    private async Task StartAsync()
    {
        _loading = true;
        _error   = null;
        _started = null;

        try
        {
            _started = await SimulationApi.StartMassSimulationAsync(_cmd);
            Notifications.Notify($"Mass simulation started: {_started?.JobId}");
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }
}
