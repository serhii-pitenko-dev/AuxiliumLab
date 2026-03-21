
namespace AuxiliumLab.Frontend.Features.AggregationRun.Pages;

public partial class AggregationRunPage
{
    private StartAggregationCommand _cmd = new()
    {
        StandardSimulationCount = 100,
        Algorithm               = ModelType.PPO,
        PolicyType              = AiPolicy.MLP,
        Steps =
        [
            new AggregationStepDto { Name = "Random AI", Mode = "MassRandomAISimulation" },
            new AggregationStepDto { Name = "PPO - AI",  Mode = "MassTrainedAISimulation" }
        ]
    };

    private bool _loading;
    private AggregationJobStartedDto? _started;
    private string? _error;

    private void AddStep() => _cmd.Steps.Add(new AggregationStepDto());
    private void RemoveStep(int idx) => _cmd.Steps.RemoveAt(idx);

    private async Task StartAsync()
    {
        _loading = true;
        _error   = null;
        _started = null;

        try
        {
            _started = await AggregationApi.StartAggregationAsync(_cmd);
            Notifications.Notify($"Aggregation started: {_started?.JobId}");
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
