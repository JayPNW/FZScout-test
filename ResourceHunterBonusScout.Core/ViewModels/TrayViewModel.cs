using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ResourceHunterBonusScout.ViewModels;

public partial class TrayViewModel : ObservableObject
{
    [ObservableProperty]
    public partial AppStatus Status { get; set; } = new([], HasApiKey: false, LastOperationFailed: false);

    public event EventHandler SettingsRequested = delegate{};
    public event EventHandler LogRequested = delegate{};
    public event EventHandler ExitRequested = delegate{};

    [RelayCommand]
    void ShowSettings() => SettingsRequested(this, EventArgs.Empty);

    [RelayCommand]
    void OpenLog() => LogRequested(this, EventArgs.Empty);

    [RelayCommand]
    void Exit() => ExitRequested(this, EventArgs.Empty);
}
