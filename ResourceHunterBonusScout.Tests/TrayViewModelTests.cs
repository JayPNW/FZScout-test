using ResourceHunterBonusScout.ViewModels;

namespace ResourceHunterBonusScout.Tests;

public class TrayViewModelTests
{
    readonly TrayViewModel _viewModel = new();

    [Fact]
    public void StatusChangeIsNotified()
    {
        var changed = new List<string?>();
        this._viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        this._viewModel.Status = this._viewModel.Status with
        { HasApiKey = true };

        Assert.Equal(["Status"], changed);
    }

    [Fact]
    public void MenuCommandsRaiseEvents()
    {
        var raised = new List<string>();
        this._viewModel.SettingsRequested += delegate { raised.Add("SettingsRequested"); };
        this._viewModel.LogRequested += delegate { raised.Add("LogRequested"); };
        this._viewModel.AboutRequested += delegate { raised.Add("AboutRequested"); };
        this._viewModel.ExitRequested += delegate { raised.Add("ExitRequested"); };

        this._viewModel.ShowSettingsCommand.Execute(null);
        this._viewModel.OpenLogCommand.Execute(null);
        this._viewModel.ShowAboutCommand.Execute(null);
        this._viewModel.ExitCommand.Execute(null);

        Assert.Equal(["SettingsRequested", "LogRequested", "AboutRequested", "ExitRequested"], raised);
    }
}
