using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Statistic;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumLab.AiSandbox.WebApi.Features.Statistic;

/// <summary>
/// Statistic feature – returns completed run summaries.
/// All endpoints are under <c>/ai-sandbox/statistic</c>.
/// </summary>
[ApiController]
[Route("ai-sandbox/statistic")]
[Produces("application/json")]
public sealed class StatisticController : ControllerBase
{
    private readonly IStatisticQueries _statisticQueries;

    public StatisticController(IStatisticQueries statisticQueries)
    {
        _statisticQueries = statisticQueries;
    }

    /// <summary>Returns all completed simulation runs (single and mass).</summary>
    [HttpGet("simulations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompletedSimulations(CancellationToken ct)
    {
        var runs = await _statisticQueries.GetCompletedSimulationRunsAsync(ct);
        return Ok(runs);
    }

    /// <summary>Returns all completed aggregation runs with per-step summaries.</summary>
    [HttpGet("aggregations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompletedAggregations(CancellationToken ct)
    {
        var runs = await _statisticQueries.GetCompletedAggregationRunsAsync(ct);
        return Ok(runs);
    }
}
