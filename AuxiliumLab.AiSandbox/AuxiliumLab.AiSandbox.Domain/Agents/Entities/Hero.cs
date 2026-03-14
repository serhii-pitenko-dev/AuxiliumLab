using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

namespace AuxiliumLab.AiSandbox.Domain.Agents.Entities;

public class Hero : Agent
{
    /// <summary>
    /// Initializes a new instance of the Hero class with specified agent characters and identifier.
    /// It will be placed on the map later and cell will be assigned then.
    /// </summary>
    /// <param name="characters">The initial agent characters associated with the hero.</param>
    /// <param name="id">The unique identifier for the hero.</param>
    public Hero(
        InitialAgentCharacters characters,
        Guid id) : base(ObjectType.Hero, characters, null, id)
    {

    }

    /// <summary>
    /// Initializes a new instance of the Hero class with the specified cell, characters, and identifier.
    /// Cell will be assigned to the Hero and Hero will be placed on the map in one step.
    /// </summary>
    /// <param name="cell">The cell where the hero is located.</param>
    /// <param name="characters">The initial character attributes for the hero.</param>
    /// <param name="id">The unique identifier for the hero.</param>
    public Hero(Cell cell,
        InitialAgentCharacters characters,
        Guid id) : base(ObjectType.Hero, characters, cell, id)
    {

    }

    public Hero() : base(ObjectType.Hero, new InitialAgentCharacters(),  null, new Guid())
    { }

    public Hero Clone()
    {
        var clone = new Hero();

        CopyBaseTo(clone);

        return clone;
    }
}

