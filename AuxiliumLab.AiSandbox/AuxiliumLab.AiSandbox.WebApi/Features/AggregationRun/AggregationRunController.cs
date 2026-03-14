using AuxiliumLab.AiSandbox.ApplicationServices.Commands.AggregationRun;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.AggregationRun.Dto;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.AggregationRun;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumLab.AiSandbox.WebApi.Features.AggregationRun;

/// <summary>
/// AggregationRun feature – covers launching multi-step aggregation runs.
/// All endpoints are under <c>/ai-sandbox/aggregation</c>.
/// </summary>
[ApiController]
[Route("ai-sandbox/aggregation")]
[Produces("application/json")]
public sealed class AggregationRunController : ControllerBase
{
    private readonly IAggregationRunCommands _aggregationRunCommands;
    private readonly IAggregationRunQueries _aggregationRunQueries;

    public AggregationRunController(
        IAggregationRunCommands aggregationRunCommands,
        IAggregationRunQueries aggregationRunQueries)
    {
        _aggregationRunCommands = aggregationRunCommands;
        _aggregationRunQueries  = aggregationRunQueries;
    }

    /// <summary>Starts a full aggregation run in the background.</summary>
    /// <remarks>
    /// When <c>Steps</c> is empty the steps from <c>aggregation-settings.json</c> are used.<br/>
    /// Returns 202 Accepted immediately with the job descriptor.
    /// </remarks>
    [HttpPost("run")]
    [ProducesResponseType(typeof(AggregationJobStartedDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> StartAggregation(
        [FromBody] StartAggregationCommand command,
        CancellationToken ct)
    {
        var result = await _aggregationRunCommands.StartAggregationAsync(command, ct);
        return Accepted(result);
    }

    /// <summary>Returns the status of all aggregation jobs (running and recently completed).</summary>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAggregationStatus(CancellationToken ct)
    {
        var statuses = await _aggregationRunQueries.GetAggregationStatusesAsync(ct);
        return Ok(statuses);
    }

    /// <summary>Stops a running aggregation job.</summary>
    [HttpPost("{jobId:guid}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopAggregation(Guid jobId, CancellationToken ct)
    {
        var success = await _aggregationRunCommands.StopAggregationAsync(jobId, ct);
        return success ? Ok() : NotFound();
    }
}
