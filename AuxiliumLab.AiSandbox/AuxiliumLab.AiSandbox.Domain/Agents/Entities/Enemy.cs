using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.Agents.Entities;

public class Enemy: Agent
{
    /// <summary>
    /// Initializes a new instance of the Enemy class with specified agent characters and identifier.
    /// It will be placed on the map later and cell will be assigned then.
    /// </summary>
    /// <param name="characters"></param>
    /// <param name="id"></param>
    public Enemy(
        InitialAgentCharacters characters,
        Guid id) : base(ObjectType.Enemy, characters, null, id)
    {

    }

    /// <summary>
    /// Initializes a new instance of the Enemy class with the specified cell, agent characters, and unique identifier.
    /// Cell will be assigned to the Enemy and Enemy will be placed on the map in one step.
    /// </summary>
    /// <param name="cell">The cell where the enemy is located.</param>
    /// <param name="characters">The initial agent characters for the enemy.</param>
    /// <param name="id">The unique identifier for the enemy.</param>
    public Enemy(
        Cell cell,
        InitialAgentCharacters characters,
        Guid id) : base(ObjectType.Enemy, characters, cell, id) 
    {

    }

    public Enemy(): base(ObjectType.Enemy, new InitialAgentCharacters(), null, new Guid())
    { }

    public Enemy Clone()
    {
        var clone = new Enemy();

        CopyBaseTo(clone);

        return clone;
    }
}

