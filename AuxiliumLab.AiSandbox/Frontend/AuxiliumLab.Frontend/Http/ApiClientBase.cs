using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AuxiliumLab.Frontend.Http;

/// <summary>
/// Base class for all feature API clients.
/// Provides consistent error handling and JSON deserialization settings.
/// </summary>
public abstract class ApiClientBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    protected readonly HttpClient Http;

    protected ApiClientBase(HttpClient http)
        => Http = http;

    protected async Task<T?> GetAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await Http.GetAsync(path, ct);
        return await ReadResponseAsync<T>(response, ct);
    }

    protected async Task<T?> PostAsync<T>(string path, object? body, CancellationToken ct = default)
    {
        var response = await Http.PostAsJsonAsync(path, body, JsonOptions, ct);
        return await ReadResponseAsync<T>(response, ct);
    }

    protected async Task PostAsync(string path, object? body, CancellationToken ct = default)
    {
        var response = await Http.PostAsJsonAsync(path, body, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"POST {path} failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(ct)}");
    }

    protected async Task<bool> PostVoidAsync(string path, CancellationToken ct = default)
    {
        var response = await Http.PostAsync(path, null, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;
        response.EnsureSuccessStatusCode();
        return true;
    }

    private static async Task<T?> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"{response.RequestMessage?.RequestUri} ({(int)response.StatusCode}): {body}");
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }
}
