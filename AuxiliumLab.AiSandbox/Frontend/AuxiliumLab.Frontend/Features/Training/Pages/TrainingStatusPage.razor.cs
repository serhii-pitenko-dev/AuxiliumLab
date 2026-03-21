
namespace AuxiliumLab.Frontend.Features.Training.Pages;

public partial class TrainingStatusPage
{
    private List<TrainingJobStatusDto> _jobs = [];
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
        try { _jobs = await TrainingApi.GetTrainingStatusesAsync(); }
        finally { _loading = false; StateHasChanged(); }
    }

    private async Task StopAsync(Guid jobId)
    {
        await TrainingApi.StopTrainingAsync(jobId);
        Notifications.Notify($"Stop requested for training job {jobId.ToString()[..8]}");
        await RefreshAsync();
    }

    private MudBlazor.Color StateColor(TrainingJobState state) => state switch
    {
        TrainingJobState.Running   => MudBlazor.Color.Info,
        TrainingJobState.Completed => MudBlazor.Color.Success,
        TrainingJobState.Failed    => MudBlazor.Color.Error,
        _                          => MudBlazor.Color.Default
    };

    public void Dispose() => _timer?.Dispose();
}
