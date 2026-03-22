using Microsoft.AspNetCore.Components;

namespace AuxiliumLab.Frontend.Shared;

public partial class SandboxSettingsForm
{
    [Parameter, EditorRequired] public SimulationSandboxOverrideDto Override { get; set; } = new();
    [Parameter] public EventCallback<SimulationSandboxOverrideDto> OverrideChanged { get; set; }

    private bool _expanded;

    private int MaxTurns
    {
        get => Override.MaxTurns;
        set { Override.MaxTurns = value; OverrideChanged.InvokeAsync(Override); }
    }
    private int MapWidth
    {
        get => Override.MapWidth;
        set { Override.MapWidth = value; OverrideChanged.InvokeAsync(Override); }
    }
    private int MapHeight
    {
        get => Override.MapHeight;
        set { Override.MapHeight = value; OverrideChanged.InvokeAsync(Override); }
    }
    private double BlocksPercent
    {
        get => Override.BlocksPercent;
        set { Override.BlocksPercent = value; OverrideChanged.InvokeAsync(Override); }
    }
    private double EnemiesPercent
    {
        get => Override.EnemiesPercent;
        set { Override.EnemiesPercent = value; OverrideChanged.InvokeAsync(Override); }
    }
    private int HeroSpeed
    {
        get => Override.HeroSpeed;
        set { Override.HeroSpeed = value; OverrideChanged.InvokeAsync(Override); }
    }
    private int HeroSightRange
    {
        get => Override.HeroSightRange;
        set { Override.HeroSightRange = value; OverrideChanged.InvokeAsync(Override); }
    }
    private int HeroStamina
    {
        get => Override.HeroStamina;
        set { Override.HeroStamina = value; OverrideChanged.InvokeAsync(Override); }
    }
    private int EnemySpeed
    {
        get => Override.EnemySpeed;
        set { Override.EnemySpeed = value; OverrideChanged.InvokeAsync(Override); }
    }
}
