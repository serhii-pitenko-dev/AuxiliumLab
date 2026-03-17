using AuxiliumLab.Frontend.Features.Simulation.Dto;

namespace AuxiliumLab.Frontend.Features.Simulation.Pages;

public partial class SimulationStatusPage
{
    private List<SimulationJobStatusDto> _jobs = [];
    private bool _loading;
    private Timer? _timer;

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
        _timer = new Timer(_ => InvokeAsync(RefreshAsync), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    private async Task RefreshAsync()
    {
        _loading = true;
        try { _jobs = await SimulationApi.GetSimulationStatusesAsync(); }
        finally { _loading = false; StateHasChanged(); }
    }

    private async Task StopAsync(Guid jobId)
    {
        await SimulationApi.StopSimulationAsync(jobId);
        Notifications.Notify($"Stop requested for simulation job {jobId.ToString()[..8]}");
        await RefreshAsync();
    }

    private MudBlazor.Color StateColor(SimulationJobState state) => state switch
    {
        SimulationJobState.Running   => MudBlazor.Color.Info,
        SimulationJobState.Completed => MudBlazor.Color.Success,
        SimulationJobState.Failed    => MudBlazor.Color.Error,
        _                            => MudBlazor.Color.Default
    };

    public void Dispose() => _timer?.Dispose();
}
