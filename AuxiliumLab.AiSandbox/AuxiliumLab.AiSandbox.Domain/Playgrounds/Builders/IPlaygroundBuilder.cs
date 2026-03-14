using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.InanimateObjects;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.Playgrounds.Builders;

public interface IPlaygroundBuilder
{
    IPlaygroundBuilder SetMap(MapSquareCells tileMap);
    IPlaygroundBuilder SetPlaygroundId(Guid id);
    IPlaygroundBuilder SetTurn(int turn);
    IPlaygroundBuilder PlaceBlocks(int blocksCount);
    IPlaygroundBuilder PlaceBlock(Block block, Coordinates coordinates);
    IPlaygroundBuilder PlaceHero(InitialAgentCharacters heroCharacters);
    IPlaygroundBuilder PlaceHero(Hero hero, Coordinates coordinates);
    IPlaygroundBuilder PlaceExit();
    IPlaygroundBuilder PlaceExit(Exit exit, Coordinates coordinates);
    IPlaygroundBuilder PlaceEnemies(int enemiesCount, InitialAgentCharacters enemyCharacters);
    IPlaygroundBuilder PlaceEnemy(Enemy enemy, Coordinates coordinates);
    IPlaygroundBuilder FillCellGrid();
    StandardPlayground Build();
}

