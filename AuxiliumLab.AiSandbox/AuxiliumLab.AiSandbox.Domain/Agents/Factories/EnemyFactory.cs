using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.Agents.Factories;

public class EnemyFactory : IEnemyFactory
{
    public Enemy CreateEnemy(InitialAgentCharacters characters)
    {
        return new Enemy(characters, Guid.NewGuid());
    }
}