
namespace AuxiliumLab.Frontend.Features.Training.Pages;

public partial class TrainingStatusPage
{
    private List<TrainingJobStatusDto> _jobs = [];
    private List<TrainedModelInfoDto> _failedModels = [];
    private bool _loading;
    private Timer? _timer;

    internal DateTime? _from;
    internal DateTime? _to;

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
            var allJobs = await TrainingApi.GetTrainingStatusesAsync();
            var models = await TrainingApi.GetTrainedModelsAsync();

            // Exclude failed models whose ExperimentId already appears in the
            // in-memory job list to avoid showing the same failure twice.
            var jobExperimentIds = new HashSet<string>(
                allJobs.Select(j => j.ExperimentId),
                StringComparer.OrdinalIgnoreCase);
            var allFailed = models
                .Where(m => m.IsFailed && !jobExperimentIds.Contains(m.ExperimentId))
                .ToList();

            _jobs = FilterJobsByDate(allJobs);
            _failedModels = FilterFailedByDate(allFailed);
        }
        finally { _loading = false; StateHasChanged(); }
    }

    private List<TrainingJobStatusDto> FilterJobsByDate(List<TrainingJobStatusDto> jobs)
    {
        var from = _from;
        var to = _to ?? DateTime.UtcNow;
        if (from is null) return jobs;
        return jobs.Where(j => j.StartedAt >= from && j.StartedAt <= to).ToList();
    }

    private List<TrainedModelInfoDto> FilterFailedByDate(List<TrainedModelInfoDto> models)
    {
        var from = _from;
        var to = _to ?? DateTime.UtcNow;
        if (from is null) return models;
        return models.Where(m => m.TrainedAt >= from && m.TrainedAt <= to).ToList();
    }

    private async Task OnFromChanged(DateTime? value)
    {
        _from = value;
        await RefreshAsync();
    }

    private async Task OnToChanged(DateTime? value)
    {
        _to = value;
        await RefreshAsync();
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
