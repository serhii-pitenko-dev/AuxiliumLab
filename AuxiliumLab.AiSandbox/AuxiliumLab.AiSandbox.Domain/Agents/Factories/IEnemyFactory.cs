using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Maps;

namespace AuxiliumLab.AiSandbox.Domain.Agents.Factories;

public interface IEnemyFactory
{
    Enemy CreateEnemy(InitialAgentCharacters characters);
}

