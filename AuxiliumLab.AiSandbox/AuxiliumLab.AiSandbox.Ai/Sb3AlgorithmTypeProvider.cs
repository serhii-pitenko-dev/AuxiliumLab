using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;

namespace AuxiliumLab.AiSandbox.Ai;

/// <summary>
/// Factory that creates Sb3Actions instances. Each call to Create produces a
/// distinct instance with a unique GymId, suitable for one parallel gym environment.
/// </summary>
public class Sb3AlgorithmTypeProvider
{
    public Sb3Actions Create(
        ModelType modelType,
        IMessageBroker messageBroker,
        IMemoryDataManager<AgentStateForAIDecision> agentStateMemoryRepository,
        float stepPenalty = -0.1f,
        float winReward = 10f,
        float lossReward = -10f)
    {
        var config = new AiConfiguration
        {
            ModelType = modelType,
            Version = "1.0",
            PolicyType = AiPolicy.MLP
        };

        return new Sb3Actions(
            messageBroker,
            agentStateMemoryRepository,
            modelType,
            config,
            Guid.NewGuid(),
            stepPenalty,
            winReward,
            lossReward);
    }
}



