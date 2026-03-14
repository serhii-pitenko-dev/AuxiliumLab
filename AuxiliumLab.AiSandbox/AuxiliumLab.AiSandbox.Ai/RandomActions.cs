using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Commands;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Responses;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Ai;

/// <summary>
/// Should be one instace per one simulation 
/// </summary>
public class RandomActions : IAiActions
{
    public ModelType ModelType { get; init; } = ModelType.Random;
    public string Version { get; init; } = "1.0";
    public AiConfiguration AiConfiguration { get; init; } = new AiConfiguration
    {
        ModelType = ModelType.Random,
        Version = "1.0",
        PolicyType = AiPolicy.MLP
    };

    private readonly Random _random = new();
    private readonly IMessageBroker _messageBroker;
    private readonly IMemoryDataManager<AgentStateForAIDecision> _agentStateMemoryRepository;

    public RandomActions(
        IMessageBroker messageBroker, 
        IMemoryDataManager<AgentStateForAIDecision> agentStateMemoryRepository)
    {
        _messageBroker = messageBroker;
        _agentStateMemoryRepository = agentStateMemoryRepository;
    }

    public void Initialize()
    {
        _messageBroker.Subscribe<GameStartedEvent>(msg =>
        {
            _messageBroker.Publish(new AiReadyToActionsResponse(Guid.NewGuid(), msg.PlaygroundId, msg.Id));
        });

        _messageBroker.Subscribe<RequestAgentDecisionMakeCommand>(msg =>
        {
            var agent = _agentStateMemoryRepository.LoadObject(msg.AgentId);
            AgentDecisionBaseResponse response = HandleAgentActionMessage(agent, msg.Id);
            _messageBroker.Publish(response);
        });
    }

    private AgentDecisionBaseResponse HandleAgentActionMessage(AgentStateForAIDecision agent, Guid correlationId)
    {
        return Action(agent, correlationId);
    }

    private AgentDecisionBaseResponse Action(AgentStateForAIDecision agent, Guid correlationId)
    {
        var action = agent.AvailableLimitedActions[Random.Shared.Next(agent.AvailableLimitedActions.Count)];
        switch (action)
        {
            case AgentAction.Move:
                // Calculate the path without modifying the agent
                return CalculatePath(agent, correlationId);
            case AgentAction.Run:
                // Randomly decide whether to use abilities
                return UseAbilities(agent, action, correlationId);
            default:
                break;
        }

        throw new NotImplementedException($"Action {action} is not implemented in RandomActions AI.");
    }

    private AgentDecisionBaseResponse UseAbilities(AgentStateForAIDecision agentState, AgentAction ability, Guid correlationId)
    {
        bool isActivated = agentState.IsRun;
        bool isSuccess = false;

        if (_random.NextDouble() < 0.1) // 10% chance to activate ability
        {
            if (ability == AgentAction.Run && !agentState.IsRun)
            {
                isActivated = true;
                isSuccess = true;
            }
        }

        if (_random.NextDouble() < 0.1) // 10% chance to deactivate ability
        {
            if (ability == AgentAction.Run && agentState.IsRun)
            {
                isActivated = false;
                isSuccess = true;
            }
        }

        return new AgentDecisionUseAbilityResponse(
            Guid.NewGuid(), 
            agentState.Id, 
            isActivated, 
            ability, 
            correlationId, 
            isSuccess);
    }

    private AgentDecisionMoveResponse CalculatePath(AgentStateForAIDecision agentState, Guid correlationId)
    {
        // Get random number of moves based on agent's speed
        int numberOfMoves = _random.Next(0, agentState.Speed + 1);
        Coordinates from = agentState.Coordinates;
        Coordinates to = agentState.Coordinates;
        to = CalculateNextMove(agentState, from);

        return new AgentDecisionMoveResponse(
            Guid.NewGuid(),
            agentState.Id,
            from,
            to,
            correlationId,
            IsSuccess: true);
    }

    private Coordinates CalculateNextMove(AgentStateForAIDecision agentState, Coordinates currentPosition)
    {
        if (agentState.VisibleCells.Count == 0)
        {
            return currentPosition; // No visible cells to move to
        }

        // Filter for neighboring cells only (Manhattan distance = 1)
        var neighboringCells = agentState.VisibleCells
            .Where(cell => Math.Max(Math.Abs(cell.Coordinates.X - currentPosition.X),
                                   Math.Abs(cell.Coordinates.Y - currentPosition.Y)) == 1)
            .ToList();

        if (neighboringCells.Count == 0)
        {
            return currentPosition; // No neighboring cells available
        }

        // Get a random neighboring cell
        VisibleCellData targetCell = neighboringCells[_random.Next(neighboringCells.Count)];

        return targetCell.Coordinates;
    }
}