using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.Agents.Factories;

public class HeroFactory: IHeroFactory
{
    public Hero CreateHero(InitialAgentCharacters characters)
    {
        return new Hero(characters, Guid.NewGuid());
    }
}

