namespace ResourceHunterBonusScout.Tests;

public class SettingsTests
{
    readonly InMemoryApiKeyStore apiKeyStore = new();
    readonly FakeStartWithWindows startWithWindows = new();
    readonly Settings settings;

    public SettingsTests()
    {
        settings = new Settings(apiKeyStore, startWithWindows);
    }

    [Fact]
    public void FirstRunHasNoKeyAndStartsWithWindows()
    {
        Assert.Equal("", settings.ApiKey);
        Assert.True(settings.StartWithWindows);
    }

    [Fact]
    public void ReadsStoredValues()
    {
        apiKeyStore.ApiKey = "fz_live_abc";
        startWithWindows.IsEnabled = false;

        Assert.Equal("fz_live_abc", settings.ApiKey);
        Assert.False(settings.StartWithWindows);
    }

    [Fact]
    public void SaveTrimsKeyAndStoresValues()
    {
        Assert.True(settings.TrySave("  fz_live_abc \n", true));

        Assert.Equal("fz_live_abc", apiKeyStore.ApiKey);
        Assert.True(startWithWindows.IsEnabled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("FZ_LIVE_abc")]
    public void SaveRejectsKeyWithoutPrefix(string apiKey)
    {
        startWithWindows.IsEnabled = true;

        Assert.False(settings.TrySave(apiKey, false));

        Assert.Null(apiKeyStore.ApiKey);
        Assert.True(startWithWindows.IsEnabled);
    }
}
