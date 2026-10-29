using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout;

/// <summary>What the user can change in the settings window.</summary>
public class Settings
{
    public const string ApiKeyPrefix = "fz_live_";

    readonly IApiKeyStore apiKeyStore;
    readonly IStartWithWindows startWithWindows;

    public Settings(IApiKeyStore apiKeyStore, IStartWithWindows startWithWindows)
    {
        this.apiKeyStore = apiKeyStore;
        this.startWithWindows = startWithWindows;
    }

    public string ApiKey => apiKeyStore.Read() ?? "";

    // With no key, nothing has been saved yet, so use the default.
    public bool StartWithWindows => apiKeyStore.Read() is null || startWithWindows.IsEnabled;

    /// <returns>False, saving nothing, when the key doesn't start with <see cref="ApiKeyPrefix"/>.</returns>
    public bool TrySave(string apiKey, bool startWithWindows)
    {
        apiKey = apiKey.Trim();
        if (!apiKey.StartsWith(ApiKeyPrefix, StringComparison.Ordinal))
            return false;

        apiKeyStore.Write(apiKey);
        this.startWithWindows.IsEnabled = startWithWindows;
        return true;
    }
}
