using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Validation;
using AuxiliumLab.AiSandbox.Domain.Playgrounds.Builders;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.Domain.Agents.Factories;
using AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;

namespace AuxiliumLab.AiSandbox.Domain.Playgrounds.Factories;

public class PlaygroundFactory() : IPlaygroundFactory
{
    public static int PercentCalculation (int totalCells, int percent) => (totalCells * percent) / 100;

    public StandardPlayground CreateStandard(
        InitialAgentCharacters heroCharacters,
        InitialAgentCharacters enemyCharacters,
        int width, 
        int height,
        int percentOfBlocks = 10,
        int percentOfEnemies = 0)
    {
        // initialize builder for every playground creation to avoid multithreading issues with shared builder instance
        IPlaygroundBuilder playgroundBuilder = InitializePlaygroundBuilder();

        MapValidator.ValidateSize(width, height);
        MapValidator.ValidateElementsProportion(percentOfBlocks, percentOfEnemies);

        return playgroundBuilder.SetMap(new MapSquareCells(width, height))
            .PlaceBlocks(PercentCalculation(width * height, percentOfBlocks))
            .PlaceHero(heroCharacters)
            .PlaceExit()
            .PlaceEnemies(PercentCalculation(width * height, percentOfEnemies), enemyCharacters)
            .FillCellGrid()
            .Build();
    }

    private IPlaygroundBuilder InitializePlaygroundBuilder()
    {
        IEnemyFactory enemyFactory = new EnemyFactory();
        IHeroFactory heroFactory = new HeroFactory();
        IVisibilityService visibilityService = new VisibilityService();

        return new PlaygroundBuilder(enemyFactory, heroFactory, visibilityService);
    }
}


