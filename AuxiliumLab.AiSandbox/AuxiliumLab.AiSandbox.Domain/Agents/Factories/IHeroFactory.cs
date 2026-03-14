using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Maps;

namespace AuxiliumLab.AiSandbox.Domain.Agents.Factories;

public interface IHeroFactory
{
    Hero CreateHero(InitialAgentCharacters characters);
}

