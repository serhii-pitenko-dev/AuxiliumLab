using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.Sb3Contract.Commands;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.Sb3Contract.Responses;
using AuxiliumLab.AiSandbox.GrpcHost.Protos;
using Grpc.Core;

namespace AuxiliumLab.AiSandbox.GrpcHost.Services;

/// <summary>
/// gRPC service that bridges Python SB3 gym calls to internal Sb3Contract MessageBroker messages.
/// Each gym (identified by gym_id) maps to one Sb3Actions instance in the C# simulation.
///
/// Uses <see cref="GymBrokerRegistry"/> to route each call to the correct per-gym isolated
/// <see cref="IMessageBroker"/> — the singleton broker is NOT used here, because Sb3Actions
/// subscribes on a per-gym broker, not the shared DI singleton.
/// </summary>
public class SimulationService : Protos.SimulationService.SimulationServiceBase
{
    private readonly ILogger<SimulationService> _logger;
    private readonly GymBrokerRegistry _gymBrokerRegistry;

    public SimulationService(ILogger<SimulationService> logger, GymBrokerRegistry gymBrokerRegistry)
    {
        _logger = logger;
        _gymBrokerRegistry = gymBrokerRegistry;
    }

    public override async Task<ResetResponse> Reset(ResetRequest request, ServerCallContext context)
    {
        Guid gymId = ParseGymId(request.GymId);
        _logger.LogInformation("Reset called for gym {GymId}, seed {Seed}", gymId, request.Seed);
        Console.WriteLine($"[SimulationService] Reset request for gym_id={gymId}");

        var broker = GetGymBroker(gymId);

        var commandId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<SimulationResetResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Action<SimulationResetResponse> handler = null!;
        handler = msg =>
        {
            if (msg.GymId == gymId && msg.CorrelationId == commandId)
            {
                broker.Unsubscribe(handler);
                tcs.TrySetResult(msg);
            }
        };
        broker.Subscribe(handler);

        broker.Publish(new RequestSimulationResetCommand(commandId, gymId, request.Seed));
        Console.WriteLine($"[SimulationService] Published RequestSimulationResetCommand for gym_id={gymId}");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(120));  // 120s: must exceed Python's 60s Reset timeout
        cts.Token.Register(() => tcs.TrySetCanceled());

        var result = await tcs.Task.ConfigureAwait(false);

        var response = new ResetResponse();
        response.Observation.AddRange(result.Observation);
        foreach (var kv in result.Info)
            response.Info.Add(kv.Key, kv.Value);

        return response;
    }

    public override async Task<StepResponse> Step(StepRequest request, ServerCallContext context)
    {
        Guid gymId = ParseGymId(request.GymId);
        _logger.LogDebug("Step called for gym {GymId}, action {Action}", gymId, request.Action);

        var broker = GetGymBroker(gymId);

        var commandId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<SimulationStepResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Action<SimulationStepResponse> handler = null!;
        handler = msg =>
        {
            if (msg.GymId == gymId && msg.CorrelationId == commandId)
            {
                broker.Unsubscribe(handler);
                tcs.TrySetResult(msg);
            }
        };
        broker.Subscribe(handler);

        broker.Publish(new RequestSimulationStepCommand(commandId, gymId, request.Action));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(60));  // 60s: must exceed Python's 30s Step timeout
        cts.Token.Register(() => tcs.TrySetCanceled());

        var result = await tcs.Task.ConfigureAwait(false);

        var response = new StepResponse
        {
            Reward = result.Reward,
            Terminated = result.Terminated,
            Truncated = result.Truncated
        };
        response.Observation.AddRange(result.Observation);
        foreach (var kv in result.Info)
            response.Info.Add(kv.Key, kv.Value);

        return response;
    }

    public override async Task<CloseResponse> Close(CloseRequest request, ServerCallContext context)
    {
        Guid gymId = ParseGymId(request.GymId);
        _logger.LogInformation("Close called for gym {GymId}", gymId);

        var broker = GetGymBroker(gymId);

        var commandId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<SimulationCloseResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Action<SimulationCloseResponse> handler = null!;
        handler = msg =>
        {
            if (msg.GymId == gymId && msg.CorrelationId == commandId)
            {
                broker.Unsubscribe(handler);
                tcs.TrySetResult(msg);
            }
        };
        broker.Subscribe(handler);

        broker.Publish(new RequestSimulationCloseCommand(commandId, gymId));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));
        cts.Token.Register(() => tcs.TrySetCanceled());

        var result = await tcs.Task.ConfigureAwait(false);

        return new CloseResponse { Success = result.Success, Message = "Environment closed" };
    }

    private IMessageBroker GetGymBroker(Guid gymId)
    {
        var broker = _gymBrokerRegistry.Get(gymId);
        if (broker is null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"No broker registered for gym_id '{gymId}'. "
                + "Ensure TrainingRunner has registered the gym before Python calls Reset/Step/Close."));
        return broker;
    }

    private static Guid ParseGymId(string gymId)
    {
        if (Guid.TryParse(gymId, out var g)) return g;
        throw new RpcException(new Status(StatusCode.InvalidArgument,
            $"Invalid gym_id format: '{gymId}'. Must be a valid UUID."));
    }
}
