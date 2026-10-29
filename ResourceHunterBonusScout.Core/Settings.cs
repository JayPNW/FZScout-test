using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout;

/// <summary>What the user can change in the settings window.</summary>
public class Settings
{
    public const string ApiKeyPrefix = "fz_live_";

    readonly IApiKeyStore _apiKeyStore;
    readonly IStartWithWindows _startWithWindows;
    readonly ISettingsStore _store;

    public Settings(IApiKeyStore apiKeyStore, IStartWithWindows startWithWindows, ISettingsStore store)
    {
        this._apiKeyStore = apiKeyStore;
        this._startWithWindows = startWithWindows;
        this._store = store;
    }

    public string ApiKey => this._apiKeyStore.Read() ?? "";

    // With no key, nothing has been saved yet, so use the default.
    public bool StartWithWindows => this._apiKeyStore.Read() is null || this._startWithWindows.IsEnabled;

    public bool IdleTimer => this._store.Read().IdleTimer;

    /// <returns>False, saving nothing, when the key doesn't start with <see cref="ApiKeyPrefix"/>.</returns>
    public bool TrySave(string apiKey, bool startWithWindows, bool idleTimer)
    {
        apiKey = apiKey.Trim();
        if (!apiKey.StartsWith(ApiKeyPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        this._apiKeyStore.Write(apiKey);
        this._startWithWindows.IsEnabled = startWithWindows;
        this._store.Write(this._store.Read() with { IdleTimer = idleTimer });
        return true;
    }
}
