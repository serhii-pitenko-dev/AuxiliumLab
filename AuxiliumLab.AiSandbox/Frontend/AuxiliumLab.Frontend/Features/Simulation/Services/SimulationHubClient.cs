using AuxiliumLab.Frontend.Features.Simulation.Dto;
using AuxiliumLab.Frontend.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace AuxiliumLab.Frontend.Features.Simulation.Services;

/// <summary>
/// Manages a SignalR connection to the backend <c>/hubs/simulation</c> hub.
/// Provides a typed event surface for the visualization component.
/// </summary>
public interface ISimulationHubClient : IAsyncDisposable
{
    event Action<SimulationStartedDto>? OnSimulationStarted;
    event Action<AgentMovedDto>?        OnAgentMoved;
    event Action<AgentToggledDto>?      OnAgentToggled;
    event Action<TurnCompletedDto>?     OnTurnCompleted;
    event Action<SimulationEndedDto>?   OnSimulationEnded;

    Task ConnectAsync(string jobId, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class SimulationHubClient : ISimulationHubClient
{
    private readonly IApiContextProvider _context;
    private HubConnection? _connection;

    public event Action<SimulationStartedDto>? OnSimulationStarted;
    public event Action<AgentMovedDto>?        OnAgentMoved;
    public event Action<AgentToggledDto>?      OnAgentToggled;
    public event Action<TurnCompletedDto>?     OnTurnCompleted;
    public event Action<SimulationEndedDto>?   OnSimulationEnded;

    public SimulationHubClient(IApiContextProvider context)
        => _context = context;

    public async Task ConnectAsync(string jobId, CancellationToken ct = default)
    {
        if (_connection is not null)
            await DisconnectAsync(ct);

        _connection = new HubConnectionBuilder()
            .WithUrl(_context.SimulationHubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<SimulationStartedDto>("SimulationStarted", dto => OnSimulationStarted?.Invoke(dto));
        _connection.On<AgentMovedDto>       ("AgentMoved",         dto => OnAgentMoved?.Invoke(dto));
        _connection.On<AgentToggledDto>     ("AgentToggled",       dto => OnAgentToggled?.Invoke(dto));
        _connection.On<TurnCompletedDto>    ("TurnCompleted",      dto => OnTurnCompleted?.Invoke(dto));
        _connection.On<SimulationEndedDto>  ("SimulationEnded",    dto => OnSimulationEnded?.Invoke(dto));

        await _connection.StartAsync(ct);
        await _connection.InvokeAsync("JoinSimulation", jobId, ct);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_connection is null) return;
        await _connection.StopAsync(ct);
        await _connection.DisposeAsync();
        _connection = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}
