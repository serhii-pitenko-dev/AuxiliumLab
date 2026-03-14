namespace AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

public class Coordinates
{
    public int X { get; init; }
    public int Y { get; init; }
    
    public Coordinates(int x, int y)
    {
        if (x < 0) throw new ArgumentException("X must be non-negative", nameof(x));
        if (y < 0) throw new ArgumentException("Y must be non-negative", nameof(y));
        X = x;
        Y = y;
    }

    public static bool operator ==(Coordinates left, Coordinates right)
    {
        return left.X == right.X && left.Y == right.Y;
    }

    public static bool operator !=(Coordinates left, Coordinates right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        return obj is Coordinates coordinates && this == coordinates;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}