using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ResourceHunterBonusScout.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    readonly Settings _settings;

    /// <summary>Make a new one each time the window opens, so it shows what's stored and drops unsaved edits.</summary>
    public SettingsViewModel(Settings settings)
    {
        this._settings = settings;
        ApiKey = settings.ApiKey;
        StartWithWindows = settings.StartWithWindows;
        IdleTimer = settings.IdleTimer;
    }

    /// <summary>Save and Cancel both hide the window. The app keeps running in the tray.</summary>
    public event EventHandler CloseRequested = delegate { };

    [ObservableProperty]
    public partial string ApiKey { get; set; }

    [ObservableProperty]
    public partial bool IsApiKeyVisible { get; set; }

    [ObservableProperty]
    public partial bool StartWithWindows { get; set; }

    [ObservableProperty]
    public partial bool IdleTimer { get; set; }

    [ObservableProperty]
    public partial bool IsApiKeyRejected { get; set; }

    partial void OnApiKeyChanged(string value) => IsApiKeyRejected = false;

    [RelayCommand]
    void Save()
    {
        IsApiKeyRejected = !this._settings.TrySave(ApiKey, StartWithWindows, IdleTimer);
        if (!IsApiKeyRejected)
        {
            CloseRequested(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    void Cancel() => CloseRequested(this, EventArgs.Empty);
}
