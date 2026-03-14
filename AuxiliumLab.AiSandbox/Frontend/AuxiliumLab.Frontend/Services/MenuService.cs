using System.Net.Http.Json;

namespace AuxiliumLab.Frontend.Services;

/// <summary>
/// Loads and caches the menu configuration from <c>wwwroot/menu-config.json</c>.
/// Filtering for the active context is applied at render time.
/// </summary>
public interface IMenuService
{
    Task<MenuConfig> GetMenuAsync();
}

/// <inheritdoc />
public sealed class MenuService : IMenuService
{
    private readonly HttpClient _http;
    private MenuConfig? _cached;

    public MenuService(HttpClient http)
        => _http = http;

    public async Task<MenuConfig> GetMenuAsync()
    {
        _cached ??= await _http.GetFromJsonAsync<MenuConfig>("menu-config.json")
                    ?? new MenuConfig();
        return _cached;
    }
}
