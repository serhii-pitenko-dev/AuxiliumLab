using AuxiliumLab.Frontend.Features.Statistics.Dto;

namespace AuxiliumLab.Frontend.Features.DataManagement.Pages;

public partial class MultipleRunVisualizationPage
{
    private record PieItem(string Label, double Value);

    private List<CompletedSimulationRunDto> _runs = [];
    private Guid? _selectedJobId;
    private CompletedSimulationRunDto? _selected;
    private List<PieItem> _pieItems = [];
    private bool _loading;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try { _runs = await StatisticsApi.GetCompletedSimulationsAsync(); }
        finally { _loading = false; }
    }

    private Guid? SelectedJobId
    {
        get => _selectedJobId;
        set
        {
            _selectedJobId = value;
            _selected = value.HasValue ? _runs.FirstOrDefault(r => r.JobId == value) : null;
            _pieItems = _selected is not null
                ? [new PieItem("Wins", _selected.Wins), new PieItem("Losses", _selected.Losses)]
                : [];
        }
    }
}
