using AuxiliumLab.Frontend.Features.Statistics.Dto;

namespace AuxiliumLab.Frontend.Features.DataManagement.Pages;

public partial class AggregationVisualizationPage
{
    private record StepChartItem(string StepName, double WinPercentage, double AverageTurns);

    private List<CompletedAggregationRunDto> _runs = [];
    private Guid? _selectedJobId;
    private CompletedAggregationRunDto? _selected;
    private List<StepChartItem> _chartItems = [];
    private bool _loading;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _runs = await StatisticsApi.GetCompletedAggregationsAsync();
        }
        finally { _loading = false; }
    }

    protected override void OnParametersSet() => SelectRun();

    private void SelectRun()
    {
        _selected   = _selectedJobId.HasValue ? _runs.FirstOrDefault(r => r.JobId == _selectedJobId) : null;
        _chartItems = _selected?.Steps.Select(s => new StepChartItem(s.StepName, s.WinPercentage, s.AverageTurns)).ToList() ?? [];
    }

    // Re-select when dropdown changes
    private Guid? SelectedJobId
    {
        get => _selectedJobId;
        set { _selectedJobId = value; SelectRun(); }
    }
}
