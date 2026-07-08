using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyCraftyStash.Models;

namespace MyCraftyStash.Services;

public class StashApiException : Exception
{
    public StashApiException(string message) : base(message) { }
}

/// <summary>
/// Thin client for the My Crafty Stash cloud API (Azure Static Web Apps).
/// Auth is the same mcs_ API key the Windows desktop app uses, sent as an
/// X-Api-Key header. The key is read from <see cref="StashSession"/> per
/// request so sign-in/out never needs a new client.
/// </summary>
public class StashApi
{
    public const string DefaultBaseUrl = "https://wonderful-meadow-0a1a40110.7.azurestaticapps.net";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http = new() { BaseAddress = new Uri(DefaultBaseUrl) };
    private readonly StashSession _session;

    public StashApi(StashSession session)
    {
        _session = session;
    }

    public Task<Whoami> WhoamiAsync(string? apiKeyOverride = null, CancellationToken ct = default) =>
        GetAsync<Whoami>("/api/whoami", apiKeyOverride, ct);

    public Task<ItemsPage> GetItemsAsync(int page = 1, int perPage = 48, string? search = null, string? type = null, CancellationToken ct = default)
    {
        var query = $"?page={page}&perPage={perPage}";
        if (!string.IsNullOrWhiteSpace(search)) query += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(type)) query += $"&type={Uri.EscapeDataString(type)}";
        return GetAsync<ItemsPage>($"/api/items{query}", null, ct);
    }

    private async Task<T> GetAsync<T>(string path, string? apiKeyOverride, CancellationToken ct)
    {
        var key = apiKeyOverride ?? _session.ApiKey
            ?? throw new StashApiException("Not signed in.");

        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Add("X-Api-Key", key);

        using var response = await _http.SendAsync(req, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new StashApiException("That API key was rejected. Check it in the Windows app under Settings > Cloud Sync > API Keys.");
        if (!response.IsSuccessStatusCode)
            throw new StashApiException($"The stash service returned an error (HTTP {(int)response.StatusCode}).");

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct)
            ?? throw new StashApiException("The stash service returned an empty response.");
    }
}
