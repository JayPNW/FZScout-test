using ResourceHunterBonusScout.ViewModels;

namespace ResourceHunterBonusScout.Tests;

public class SettingsViewModelTests
{
    readonly InMemoryApiKeyStore apiKeyStore = new();
    readonly FakeStartWithWindows startWithWindows = new();
    int closeRequests;

    SettingsViewModel Open()
    {
        var viewModel = new SettingsViewModel(new Settings(apiKeyStore, startWithWindows));
        viewModel.CloseRequested += (_, _) => closeRequests++;
        return viewModel;
    }

    [Fact]
    public void OpensWithStoredValues()
    {
        apiKeyStore.ApiKey = "fz_live_abc";
        startWithWindows.IsEnabled = false;

        var viewModel = Open();

        Assert.Equal("fz_live_abc", viewModel.ApiKey);
        Assert.False(viewModel.StartWithWindows);
        Assert.False(viewModel.IsApiKeyVisible);
        Assert.False(viewModel.IsApiKeyRejected);
    }

    [Fact]
    public void SaveStoresValuesAndCloses()
    {
        var viewModel = Open();
        viewModel.ApiKey = "fz_live_abc";
        viewModel.StartWithWindows = true;

        viewModel.SaveCommand.Execute(null);

        Assert.Equal("fz_live_abc", apiKeyStore.ApiKey);
        Assert.True(startWithWindows.IsEnabled);
        Assert.False(viewModel.IsApiKeyRejected);
        Assert.Equal(1, closeRequests);
    }

    [Fact]
    public void SaveWithBadKeyShowsErrorAndStaysOpen()
    {
        var viewModel = Open();
        viewModel.ApiKey = "abc";

        viewModel.SaveCommand.Execute(null);

        Assert.True(viewModel.IsApiKeyRejected);
        Assert.Null(apiKeyStore.ApiKey);
        Assert.Equal(0, closeRequests);
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
        apiKeyStore.ApiKey = "fz_live_old";
        var viewModel = Open();
        viewModel.ApiKey = "fz_live_new";

        viewModel.CancelCommand.Execute(null);

        Assert.Equal("fz_live_old", apiKeyStore.ApiKey);
        Assert.Equal(1, closeRequests);
    }
}
