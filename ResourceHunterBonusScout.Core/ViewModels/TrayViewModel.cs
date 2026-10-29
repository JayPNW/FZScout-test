using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.ViewModels;

public partial class TrayViewModel : ObservableObject
{
    readonly IAppActions app;

    public TrayViewModel(IAppActions app)
    {
        this.app = app;
        Status = new([], HasApiKey: false, LastOperationFailed: false);
    }

    [ObservableProperty]
    public partial AppStatus Status { get; set; }

    [RelayCommand]
    void ShowSettings() => app.ShowSettings();

    [RelayCommand]
    void OpenLog() => app.OpenTodaysLog();

    [RelayCommand]
    void Exit() => app.Exit();
}
