namespace AuxiliumLab.AiSandbox.ApplicationServices.Saver.Persistence.Sandbox.States;

public record StandardPlaygroundState
{
    public int Turn { get; init; }
    public Guid Id { get; init; }
    public HeroState? Hero { get; init; }
    public ExitState? Exit { get; init; }
    public List<BlockState> Blocks { get; init; } = [];
    public List<EnemyState> Enemies { get; init; } = [];
    public MapSquareCellsState Map { get; init; } = null!;
}