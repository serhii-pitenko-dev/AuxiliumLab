using AuxiliumLab.AiSandbox.Domain.Agents.Entities;

namespace AuxiliumLab.AiSandbox.Domain.Playgrounds.Factories;

public interface IPlaygroundFactory
{
    StandardPlayground CreateStandard(
        InitialAgentCharacters heroCharacters,
        InitialAgentCharacters enemyCharacters,
        int width, 
        int height, 
        int percentOfBlocks = 10, 
        int percentOfEnemies = 0);
}

