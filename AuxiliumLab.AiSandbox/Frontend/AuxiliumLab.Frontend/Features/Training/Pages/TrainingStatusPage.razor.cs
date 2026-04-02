
namespace AuxiliumLab.Frontend.Features.Training.Pages;

public partial class TrainingStatusPage
{
    private List<TrainingJobStatusDto> _jobs = [];
    private List<TrainedModelInfoDto> _failedModels = [];
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
        try
        {
            _jobs = await TrainingApi.GetTrainingStatusesAsync();
            var models = await TrainingApi.GetTrainedModelsAsync();
            _failedModels = models.Where(m => m.IsFailed).ToList();
        }
        finally { _loading = false; StateHasChanged(); }
    }

    private async Task StopAsync(Guid jobId)
    {
        await TrainingApi.StopTrainingAsync(jobId);
        Notifications.Notify($"Stop requested for training job {jobId.ToString()[..8]}");
        await RefreshAsync();
    }

    private static double ProgressPercent(TrainingJobStatusDto job)
        => job.TotalTimesteps > 0
            ? Math.Min(100.0, 100.0 * job.TimestepsDone / job.TotalTimesteps)
            : 0;

    private static string FormatElapsed(TrainingJobStatusDto job)
    {
        var end = job.CompletedAt ?? DateTime.UtcNow;
        var elapsed = end - job.StartedAt;
        return elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes:D2}m {elapsed.Seconds:D2}s"
            : $"{elapsed.Minutes}m {elapsed.Seconds:D2}s";
    }

    private static string FormatEta(TrainingJobStatusDto job)
    {
        if (job.TimestepsDone <= 0 || job.TotalTimesteps <= 0) return "—";
        var elapsed = DateTime.UtcNow - job.StartedAt;
        var remaining = elapsed * ((double)(job.TotalTimesteps - job.TimestepsDone) / job.TimestepsDone);
        return remaining.TotalHours >= 1
            ? $"~{(int)remaining.TotalHours}h {remaining.Minutes:D2}m"
            : $"~{remaining.Minutes}m {remaining.Seconds:D2}s";
    }

    private static MudBlazor.Color StateColor(TrainingJobState state) => state switch
    {
        TrainingJobState.Running   => MudBlazor.Color.Info,
        TrainingJobState.Completed => MudBlazor.Color.Success,
        TrainingJobState.Failed    => MudBlazor.Color.Error,
        _                          => MudBlazor.Color.Default
    };

    public void Dispose() => _timer?.Dispose();
}
