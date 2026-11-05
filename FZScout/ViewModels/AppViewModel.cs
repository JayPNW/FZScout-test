using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Velopack;

namespace FZScout.ViewModels;

partial class AppViewModel(
    string? runningVersion,
    string apiKey,
    bool launchesAtLogin,
    bool showsToastNotifications) : ObservableObject
{
    public ObservableCollection<LogLine> LogLines { get; } = [];

    [ObservableProperty]
    public partial ScoutingStatus ScoutingStatus { get; set; } = ScoutingStatus.NotScouting;

    [ObservableProperty]
    public partial DateTimeOffset? WaitingForBonusMessageUntil { get; set; }

    [ObservableProperty]
    public partial bool ApplicationHasStopped { get; set; }

    public string? RunningVersion { get; } = runningVersion;

    [ObservableProperty]
    public partial bool IsCheckingForUpdates { get; set; }

    [ObservableProperty]
    public partial UpdateCheckResult? LatestUpdateCheckResult { get; set; }

    [ObservableProperty]
    public partial SemanticVersion? DownloadedVersionToInstallAfterExit { get; set; }

    [ObservableProperty]
    public partial string ApiKey { get; set; } = apiKey;

    [ObservableProperty]
    public partial ApiKeyVerificationStatus ApiKeyVerificationStatus { get; set; } = ApiKeyVerificationStatus.NotVerified;

    [ObservableProperty]
    public partial bool LaunchesAtLogin { get; set; } = launchesAtLogin;

    [ObservableProperty]
    public partial bool ShowsToastNotifications { get; set; } = showsToastNotifications;

    public ICommand VerifyApiKeyCommand { get; } = new RelayCommand(() => { });

    public ICommand CheckForUpdatesCommand { get; } = new RelayCommand(() => { });

    public ICommand RestartToUpdateCommand { get; } = new RelayCommand(() => { });

    public ICommand ExitCommand { get; } = new RelayCommand(() => { });
}
