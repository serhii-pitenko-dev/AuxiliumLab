using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using System.Text.Json.Serialization;

namespace AuxiliumLab.AiSandbox.Domain.Maps;

public class Cell
{
    public Coordinates Coordinates { get; init; }

    public SandboxMapBaseObject Object { get; private set; }

    public Cell(Coordinates coordinates)
    {
        Coordinates = coordinates;
    }

    public void PlaceObjectToThisCell(SandboxMapBaseObject mapObject)
    {
        Object = mapObject;
        Object.UpdateCell(this);
    }
}

