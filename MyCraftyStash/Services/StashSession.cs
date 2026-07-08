using MyCraftyStash.Models;

namespace MyCraftyStash.Services;

/// <summary>
/// App-wide auth state: holds the mcs_ API key (SecureStorage-backed:
/// Keychain on Apple platforms, DPAPI on Windows) and who it belongs to.
/// </summary>
public class StashSession
{
    private const string StorageKey = "mcs_api_key";

    public string? ApiKey { get; private set; }
    public string? FirstName { get; private set; }
    public bool IsSignedIn => !string.IsNullOrEmpty(ApiKey);

    /// <summary>Loads a previously stored key at startup.</summary>
    public async Task InitializeAsync()
    {
        try
        {
            ApiKey = await SecureStorage.Default.GetAsync(StorageKey);
        }
        catch
        {
            // Corrupt/unavailable secure storage: treat as signed out.
            ApiKey = null;
        }
    }

    /// <summary>Validates the key against /api/whoami before storing it.</summary>
    public async Task<Whoami> SignInAsync(StashApi api, string rawKey)
    {
        var key = rawKey.Trim();
        var who = await api.WhoamiAsync(apiKeyOverride: key);
        await SecureStorage.Default.SetAsync(StorageKey, key);
        ApiKey = key;
        FirstName = who.FirstName;
        return who;
    }

    public void SignOut()
    {
        SecureStorage.Default.Remove(StorageKey);
        ApiKey = null;
        FirstName = null;
    }
}
