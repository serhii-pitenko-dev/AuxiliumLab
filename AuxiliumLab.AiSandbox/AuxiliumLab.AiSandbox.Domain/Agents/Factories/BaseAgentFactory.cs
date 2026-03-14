using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Validation.Agents;

namespace AuxiliumLab.AiSandbox.Domain.Agents.Factories;

public class BaseAgentFactory
{
    public BaseAgentFactory(InitialAgentCharacters characters)
    {
        InitialAgentCharactersValidator.Validate(characters);
    }
}

