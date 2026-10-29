using ResourceHunterBonusScout.ViewModels;

namespace ResourceHunterBonusScout.Tests;

public class TrayViewModelTests
{
    readonly TrayViewModel viewModel = new();

    [Fact]
    public void StatusChangeIsNotified()
    {
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.Status = viewModel.Status with { HasApiKey = true };

        Assert.Equal(["Status"], changed);
    }

    [Fact]
    public void MenuCommandsRaiseEvents()
    {
        var raised = new List<string>();
        viewModel.SettingsRequested += delegate { raised.Add("SettingsRequested"); };
        viewModel.LogRequested += delegate { raised.Add("LogRequested"); };
        viewModel.ExitRequested += delegate { raised.Add("ExitRequested"); };

        viewModel.ShowSettingsCommand.Execute(null);
        viewModel.OpenLogCommand.Execute(null);
        viewModel.ExitCommand.Execute(null);

        Assert.Equal(["SettingsRequested", "LogRequested", "ExitRequested"], raised);
    }
}
