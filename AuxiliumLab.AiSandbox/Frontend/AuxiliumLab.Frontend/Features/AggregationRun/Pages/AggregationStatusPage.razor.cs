
namespace AuxiliumLab.Frontend.Features.AggregationRun.Pages;

public partial class AggregationStatusPage
{
    private List<AggregationJobStatusDto> _jobs = [];
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
        try { _jobs = await AggregationApi.GetAggregationStatusesAsync(); }
        finally { _loading = false; StateHasChanged(); }
    }

    private async Task StopAsync(Guid jobId)
    {
        await AggregationApi.StopAggregationAsync(jobId);
        Notifications.Notify($"Stop requested for aggregation job {jobId.ToString()[..8]}");
        await RefreshAsync();
    }

    private MudBlazor.Color StateColor(AggregationJobState state) => state switch
    {
        AggregationJobState.Running   => MudBlazor.Color.Info,
        AggregationJobState.Completed => MudBlazor.Color.Success,
        AggregationJobState.Failed    => MudBlazor.Color.Error,
        _                             => MudBlazor.Color.Default
    };

    public void Dispose() => _timer?.Dispose();
}
