namespace ResourceHunterBonusScout.Tests;

public class SettingsTests
{
    readonly InMemoryApiKeyStore _apiKeyStore = new();
    readonly FakeStartWithWindows _startWithWindows = new();
    readonly Settings _settings;

    public SettingsTests()
    {
        this._settings = new Settings(this._apiKeyStore, this._startWithWindows);
    }

    [Fact]
    public void FirstRunHasNoKeyAndStartsWithWindows()
    {
        Assert.Equal("", this._settings.ApiKey);
        Assert.True(this._settings.StartWithWindows);
    }

    [Fact]
    public void ReadsStoredValues()
    {
        this._apiKeyStore.ApiKey = "fz_live_abc";
        this._startWithWindows.IsEnabled = false;

        Assert.Equal("fz_live_abc", this._settings.ApiKey);
        Assert.False(this._settings.StartWithWindows);
    }

    [Fact]
    public void SaveTrimsKeyAndStoresValues()
    {
        Assert.True(this._settings.TrySave("  fz_live_abc \n", true));

        Assert.Equal("fz_live_abc", this._apiKeyStore.ApiKey);
        Assert.True(this._startWithWindows.IsEnabled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("FZ_LIVE_abc")]
    public void SaveRejectsKeyWithoutPrefix(string apiKey)
    {
        this._startWithWindows.IsEnabled = true;

        Assert.False(this._settings.TrySave(apiKey, false));

        Assert.Null(this._apiKeyStore.ApiKey);
        Assert.True(this._startWithWindows.IsEnabled);
    }
}
