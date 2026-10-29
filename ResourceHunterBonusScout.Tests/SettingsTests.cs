namespace ResourceHunterBonusScout.Tests;

public class SettingsTests
{
    readonly InMemoryApiKeyStore _apiKeyStore = new();
    readonly FakeStartWithWindows _startWithWindows = new();
    readonly InMemorySettingsStore _settingsStore = new();
    readonly Settings _settings;

    public SettingsTests()
    {
        this._settings = new Settings(this._apiKeyStore, this._startWithWindows, this._settingsStore);
    }

    [Fact]
    public void FirstRunHasNoKeyAndStartsWithWindows()
    {
        Assert.Equal("", this._settings.ApiKey);
        Assert.True(this._settings.StartWithWindows);
        Assert.True(this._settings.IdleTimer);
    }

    [Fact]
    public void ReadsStoredValues()
    {
        this._apiKeyStore.ApiKey = "fz_live_abc";
        this._startWithWindows.IsEnabled = false;
        this._settingsStore.Settings = new(IdleTimer: false);

        Assert.Equal("fz_live_abc", this._settings.ApiKey);
        Assert.False(this._settings.StartWithWindows);
        Assert.False(this._settings.IdleTimer);
    }

    [Fact]
    public void SaveTrimsKeyAndStoresValues()
    {
        Assert.True(this._settings.TrySave("  fz_live_abc \n", true, false));

        Assert.Equal("fz_live_abc", this._apiKeyStore.ApiKey);
        Assert.True(this._startWithWindows.IsEnabled);
        Assert.False(this._settingsStore.Settings.IdleTimer);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("FZ_LIVE_abc")]
    public void SaveRejectsKeyWithoutPrefix(string apiKey)
    {
        this._startWithWindows.IsEnabled = true;

        Assert.False(this._settings.TrySave(apiKey, false, false));

        Assert.Null(this._apiKeyStore.ApiKey);
        Assert.True(this._startWithWindows.IsEnabled);
        Assert.True(this._settingsStore.Settings.IdleTimer);
    }
}
