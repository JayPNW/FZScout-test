using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using ResourceHunterBonusScout.Services;
using ResourceHunterBonusScout.ViewModels;
using ResourceHunterBonusScout.Views;
using ResourceHunterBonusScout.Windows;

namespace ResourceHunterBonusScout;

public partial class App : Application
{
    static readonly string DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ResourceHunterBonusScout");
    static readonly string Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";

    readonly CancellationTokenSource _stopping = new();
    readonly DailyFileLog _log = new(Path.Combine(DataFolder, "logs"), TimeProvider.System);
    readonly CredentialApiKeyStore _apiKeyStore = new();
    readonly JsonSettingsStore _settingsStore = new(Path.Combine(DataFolder, "settings.json"));
    readonly Settings _settings;
    TrayIcon? _trayIcon;
    SettingsWindow? _settingsWindow;
    AboutWindow? _aboutWindow;
    public App()
    {
        this._settings = new(this._apiKeyStore, new RegistryStartWithWindows(), this._settingsStore);
    }

    IAppLog Log => this._log;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Log.LogInformation(new string('-', 80));
        Log.LogInformation($"Started version {Version}");
        DispatcherUnhandledException += (_, args) =>
        {
            Log.LogError($"Unexpected error: {args.Exception}");
            args.Handled = true;
        };

        var trayViewModel = new TrayViewModel();
        trayViewModel.SettingsRequested += delegate { ShowSettings(); };
        trayViewModel.LogRequested += delegate { OpenTodaysLog(); };
        trayViewModel.AboutRequested += delegate { ShowAbout(); };
        trayViewModel.ExitRequested += delegate { Quit(); };
        this._trayIcon = new TrayIcon { DataContext = trayViewModel };
        this._trayIcon.ForceCreate(enablesEfficiencyMode: false);

        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        httpClient.DefaultRequestHeaders.UserAgent.Add(new("ResourceHunterBonusScout", Version));
        var scout = new Scout(new EqProcesses(), new BonusApi(httpClient), this._apiKeyStore, this._settingsStore, new TrayNotifier(this._trayIcon, ShowSettings), this._log, TimeProvider.System);
        trayViewModel.Status = scout.Status;
        scout.StatusChanged += (_, _) => trayViewModel.Status = scout.Status;

        if (this._apiKeyStore.Read() is null)
        {
            ShowSettings();
        }

        if (TrayPin.ShouldShowHint())
        {
            MessageWindow.ShowTrayIconHidden();
        }

        var unreachable = await scout.CheckConnectionAsync();
        if (unreachable.Count > 0)
        {
            MessageWindow.ShowCantConnect(unreachable);
        }

        _ = Updater.RunAsync(this._log, this._stopping.Token);
        await PollAsync(scout);
    }

    async Task PollAsync(Scout scout)
    {
        using var timer = new PeriodicTimer(Scout.PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(this._stopping.Token))
            {
                try
                {
                    await scout.PollAsync();
                }
                catch (Exception e)
                {
                    Log.LogError($"Unexpected error: {e}");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    void ShowSettings()
    {
        if (this._settingsWindow is null)
        {
            this._settingsWindow = new SettingsWindow(new SettingsViewModel(this._settings));
            this._settingsWindow.Closed += delegate { this._settingsWindow = null; };
            this._settingsWindow.Show();
        }
        this._settingsWindow.Activate();
    }

    void ShowAbout()
    {
        if (this._aboutWindow is null)
        {
            this._aboutWindow = new AboutWindow(Version);
            this._aboutWindow.Closed += delegate { this._aboutWindow = null; };
            this._aboutWindow.Show();
        }
        this._aboutWindow.Activate();
    }

    void OpenTodaysLog()
    {
        var path = File.Exists(this._log.TodaysFile) ? this._log.TodaysFile : Path.GetDirectoryName(this._log.TodaysFile)!;
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    void Quit()
    {
        Log.LogInformation("Exiting");
        this._stopping.Cancel();
        this._trayIcon?.Dispose();
        Shutdown();
    }
}
