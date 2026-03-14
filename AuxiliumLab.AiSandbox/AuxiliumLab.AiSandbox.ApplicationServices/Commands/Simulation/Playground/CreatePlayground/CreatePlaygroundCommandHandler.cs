using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Domain.Playgrounds.Factories;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.ApplicationServices.Commands.Simulation.Playground.CreatePlayground;

public class CreatePlaygroundCommandHandler(
    IPlaygroundFactory playgroundFactory,
    IMemoryDataManager<StandardPlayground> playgroundMemoryDataManager) : ICreatePlaygroundCommandHandler
{
    public Guid Handle(CreatePlaygroundCommandParameters commandParameters)
    {
        StandardPlayground playground = commandParameters.MapConfiguration.Type switch
        {
            MapType.Standard => playgroundFactory.CreateStandard(
                            new InitialAgentCharacters(
                                commandParameters.HeroConfiguration.Speed,
                                commandParameters.HeroConfiguration.SightRange,
                                commandParameters.HeroConfiguration.Stamina,
                                new List<Coordinates>(),
                                new List<AgentAction>(),
                                new List<AgentAction>()),
                            new InitialAgentCharacters(
                                commandParameters.EnemyConfiguration.Speed,
                                commandParameters.EnemyConfiguration.SightRange,
                                commandParameters.EnemyConfiguration.Stamina,
                                new List<Coordinates>(),
                                new List<AgentAction>(),
                                new List<AgentAction>()),
                            commandParameters.MapConfiguration.Size.Height,
                            commandParameters.MapConfiguration.Size.Width,
                            commandParameters.MapConfiguration.ElementsPercentages.BlocksPercent,
                            commandParameters.MapConfiguration.ElementsPercentages.PercentOfEnemies),
            _ => throw new NotImplementedException("Map type not implemented"),
        };

        Guid playgroundId = playground.Id;
        playgroundMemoryDataManager.AddOrUpdate(playgroundId, playground);

        return playgroundId;   
    }
}
