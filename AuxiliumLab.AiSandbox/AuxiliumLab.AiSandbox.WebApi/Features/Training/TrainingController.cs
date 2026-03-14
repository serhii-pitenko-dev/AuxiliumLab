using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Training;
using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Training.Dto;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Training;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumLab.AiSandbox.WebApi.Features.Training;

/// <summary>
/// Training feature – covers launching PPO training runs and querying model/job status.
/// All endpoints are under <c>/ai-sandbox/training</c>.
/// </summary>
[ApiController]
[Route("ai-sandbox/training")]
[Produces("application/json")]
public sealed class TrainingController : ControllerBase
{
    private readonly ITrainingCommands _trainingCommands;
    private readonly ITrainingQueries _trainingQueries;

    public TrainingController(ITrainingCommands trainingCommands, ITrainingQueries trainingQueries)
    {
        _trainingCommands = trainingCommands;
        _trainingQueries  = trainingQueries;
    }

    /// <summary>Starts a PPO training run in the background.</summary>
    /// <remarks>Returns 202 Accepted immediately with the job descriptor.</remarks>
    [HttpPost("ppo")]
    [ProducesResponseType(typeof(TrainingJobStartedDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> StartPpoTraining(
        [FromBody] StartPpoTrainingCommand command,
        CancellationToken ct)
    {
        var result = await _trainingCommands.StartPpoTrainingAsync(command, ct);
        return Accepted(result);
    }

    /// <summary>Returns all trained models found on disk.</summary>
    [HttpGet("models")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrainedModels(CancellationToken ct)
    {
        var models = await _trainingQueries.GetTrainedModelsAsync(ct);
        return Ok(models);
    }

    /// <summary>Returns the status of all training jobs (running and recently completed).</summary>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrainingStatus(CancellationToken ct)
    {
        var statuses = await _trainingQueries.GetTrainingStatusesAsync(ct);
        return Ok(statuses);
    }

    /// <summary>Stops a running training job.</summary>
    [HttpPost("{jobId:guid}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopTraining(Guid jobId, CancellationToken ct)
    {
        var success = await _trainingCommands.StopTrainingAsync(jobId, ct);
        return success ? Ok() : NotFound();
    }
}
