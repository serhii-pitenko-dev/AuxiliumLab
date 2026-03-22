using AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation;
using AuxiliumLab.AiSandbox.ApplicationServices.Queries.Simulation;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumLab.AiSandbox.WebApi.Features.Simulation;

/// <summary>
/// Simulation feature – covers single and mass simulation runs.
/// All endpoints are under <c>/ai-sandbox/simulation</c>.
/// </summary>
[ApiController]
[Route("ai-sandbox/simulation")]
[Produces("application/json")]
public sealed class SimulationController : ControllerBase
{
    private readonly ISimulationCommands _simulationCommands;
    private readonly ISimulationQueries _simulationQueries;

    public SimulationController(ISimulationCommands simulationCommands, ISimulationQueries simulationQueries)
    {
        _simulationCommands = simulationCommands;
        _simulationQueries  = simulationQueries;
    }

    /// <summary>Starts a single simulation run in the background.</summary>
    /// <remarks>
    /// Use <c>Kind = RandomAI</c> for random agent; <c>Kind = TrainedAI</c> for PPO-trained agent.<br/>
    /// Returns 202 Accepted immediately with the job descriptor.
    /// </remarks>
    [HttpPost("run/single")]
    [ProducesResponseType(typeof(SimulationJobStartedDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> StartSingleSimulation(
        [FromBody] StartSingleSimulationCommand command,
        CancellationToken ct)
    {
        var result = await _simulationCommands.StartSingleSimulationAsync(command, ct);
        return Accepted(result);
    }

    /// <summary>Starts a mass (batch) simulation run in the background.</summary>
    /// <remarks>Returns 202 Accepted immediately with the job descriptor.</remarks>
    [HttpPost("run/mass")]
    [ProducesResponseType(typeof(SimulationJobStartedDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> StartMassSimulation(
        [FromBody] StartMassSimulationCommand command,
        CancellationToken ct)
    {
        var result = await _simulationCommands.StartMassSimulationAsync(command, ct);
        return Accepted(result);
    }

    /// <summary>Stops a running simulation job.</summary>
    [HttpPost("{jobId:guid}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopSimulation(Guid jobId, CancellationToken ct)
    {
        var success = await _simulationCommands.StopSimulationAsync(jobId, ct);
        return success ? Ok() : NotFound();
    }

    /// <summary>Pauses a running visualization simulation.</summary>
    [HttpPost("{jobId:guid}/pause")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PauseSimulation(Guid jobId, CancellationToken ct)
    {
        var success = await _simulationCommands.PauseSimulationAsync(jobId, ct);
        return success ? Ok() : NotFound();
    }

    /// <summary>Resumes a paused simulation.</summary>
    [HttpPost("{jobId:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResumeSimulation(Guid jobId, CancellationToken ct)
    {
        var success = await _simulationCommands.ResumeSimulationAsync(jobId, ct);
        return success ? Ok() : NotFound();
    }

    /// <summary>Returns the status of all simulation jobs (running and recently completed).</summary>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSimulationStatus(CancellationToken ct)
    {
        var statuses = await _simulationQueries.GetSimulationStatusesAsync(ct);
        return Ok(statuses);
    }
}
