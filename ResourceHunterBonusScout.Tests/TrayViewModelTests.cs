using ResourceHunterBonusScout.ViewModels;

namespace ResourceHunterBonusScout.Tests;

public class TrayViewModelTests
{
    readonly FakeAppActions app = new();
    readonly TrayViewModel viewModel;

    public TrayViewModelTests()
    {
        viewModel = new TrayViewModel(app);
    }

    [Fact]
    public void StatusChangeIsNotified()
    {
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.Status = viewModel.Status with { HasApiKey = true };

        Assert.Equal(["Status"], changed);
    }

    [Fact]
    public void MenuCommandsCallTheApp()
    {
        viewModel.ShowSettingsCommand.Execute(null);
        viewModel.OpenLogCommand.Execute(null);
        viewModel.ExitCommand.Execute(null);

        Assert.Equal(["ShowSettings", "OpenTodaysLog", "Exit"], app.Calls);
    }
}
