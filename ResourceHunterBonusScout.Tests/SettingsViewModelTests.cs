using ResourceHunterBonusScout.ViewModels;

namespace ResourceHunterBonusScout.Tests;

public class SettingsViewModelTests
{
    readonly InMemoryApiKeyStore _apiKeyStore = new();
    readonly FakeStartWithWindows _startWithWindows = new();
    readonly InMemorySettingsStore _settingsStore = new();
    int _closeRequests;

    SettingsViewModel Open()
    {
        var viewModel = new SettingsViewModel(new Settings(this._apiKeyStore, this._startWithWindows, this._settingsStore));
        viewModel.CloseRequested += (_, _) => this._closeRequests++;
        return viewModel;
    }

    [Fact]
    public void OpensWithStoredValues()
    {
        this._apiKeyStore.ApiKey = "fz_live_abc";
        this._startWithWindows.IsEnabled = false;
        this._settingsStore.Settings = new(IdleTimer: false);

        var viewModel = Open();

        Assert.Equal("fz_live_abc", viewModel.ApiKey);
        Assert.False(viewModel.StartWithWindows);
        Assert.False(viewModel.IdleTimer);
        Assert.False(viewModel.IsApiKeyVisible);
        Assert.False(viewModel.IsApiKeyRejected);
    }

    [Fact]
    public void SaveStoresValuesAndCloses()
    {
        var viewModel = Open();
        viewModel.ApiKey = "fz_live_abc";
        viewModel.StartWithWindows = true;
        viewModel.IdleTimer = false;

        viewModel.SaveCommand.Execute(null);

        Assert.Equal("fz_live_abc", this._apiKeyStore.ApiKey);
        Assert.True(this._startWithWindows.IsEnabled);
        Assert.False(this._settingsStore.Settings.IdleTimer);
        Assert.False(viewModel.IsApiKeyRejected);
        Assert.Equal(1, this._closeRequests);
    }

    [Fact]
    public void SaveWithBadKeyShowsErrorAndStaysOpen()
    {
        var viewModel = Open();
        viewModel.ApiKey = "abc";

        viewModel.SaveCommand.Execute(null);

        Assert.True(viewModel.IsApiKeyRejected);
        Assert.Null(this._apiKeyStore.ApiKey);
        Assert.Equal(0, this._closeRequests);
    }

    [Fact]
    public void EditingKeyClearsError()
    {
        var viewModel = Open();
        viewModel.SaveCommand.Execute(null);

        viewModel.ApiKey = "fz_live_";

        Assert.False(viewModel.IsApiKeyRejected);
    }

    [Fact]
    public void CancelClosesWithoutSaving()
    {
        this._apiKeyStore.ApiKey = "fz_live_old";
        var viewModel = Open();
        viewModel.ApiKey = "fz_live_new";

        viewModel.CancelCommand.Execute(null);

        Assert.Equal("fz_live_old", this._apiKeyStore.ApiKey);
        Assert.Equal(1, this._closeRequests);
    }
}
