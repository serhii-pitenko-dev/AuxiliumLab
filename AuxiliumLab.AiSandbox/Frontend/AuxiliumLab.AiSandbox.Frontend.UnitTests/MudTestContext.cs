using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;

namespace AuxiliumLab.AiSandbox.Frontend.UnitTests;

/// <summary>
/// Extends bUnit's TestContext with MudBlazor-friendly defaults:
/// <list type="bullet">
///   <item>JSInterop uses <see cref="JSRuntimeMode.Loose"/> — MudBlazor internal JS calls don't throw.</item>
///   <item><see cref="MudPopoverProvider"/> is rendered automatically on the first <see cref="RenderComponent{T}()"/> call,
///         so popover-based components (MudSelect, MudMenu, MudChip…) work without extra test boilerplate.</item>
/// </list>
/// </summary>
public sealed class MudTestContext : Bunit.TestContext
{
    private bool _popoverProviderRendered;

    public MudTestContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Registers MudBlazor services. Call this before adding mock services and before rendering.
    /// </summary>
    public void SetupWithMudServices() => Services.AddMudServices();

    /// <summary>
    /// Renders <typeparamref name="TComponent"/>, automatically ensuring
    /// <see cref="MudPopoverProvider"/> is present the first time so that
    /// popover-dependent MudBlazor components work correctly.
    /// </summary>
    public new IRenderedComponent<TComponent> RenderComponent<TComponent>(
        Action<ComponentParameterCollectionBuilder<TComponent>>? parameterBuilder = null)
        where TComponent : IComponent
    {
        EnsureMudPopoverProvider();
        return base.RenderComponent<TComponent>(parameterBuilder);
    }

    private void EnsureMudPopoverProvider()
    {
        if (_popoverProviderRendered) return;
        _popoverProviderRendered = true;
        base.RenderComponent<MudPopoverProvider>();
    }
}
