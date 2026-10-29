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

public partial class App : Application, IAppActions
{
    static readonly string DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ResourceHunterBonusScout");
    static readonly string Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";

    readonly EventWaitHandle showSettingsRequested;
    readonly CancellationTokenSource stopping = new();
    readonly DailyFileLog log = new(Path.Combine(DataFolder, "logs"), TimeProvider.System);
    readonly CredentialApiKeyStore apiKeyStore = new();
    readonly Settings settings;
    TrayIcon? trayIcon;
    SettingsWindow? settingsWindow;

    public App(EventWaitHandle showSettingsRequested)
    {
        this.showSettingsRequested = showSettingsRequested;
        settings = new(apiKeyStore, new RegistryStartWithWindows());
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        log.Write(new string('-', 80));
        log.Write($"Started version {Version}");
        DispatcherUnhandledException += (_, args) =>
        {
            log.Write($"Unexpected error: {args.Exception}");
            args.Handled = true;
        };

        var trayViewModel = new TrayViewModel(this);
        trayIcon = new TrayIcon { DataContext = trayViewModel };
        trayIcon.ForceCreate(enablesEfficiencyMode: false);

        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.Add(new("ResourceHunterBonusScout", Version));
        var scout = new Scout(new EqProcesses(), new BonusApi(http), apiKeyStore, new TrayNotifier(trayIcon, ShowSettings), log, TimeProvider.System);
        trayViewModel.Status = scout.Status;
        scout.StatusChanged += (_, _) => trayViewModel.Status = scout.Status;

        ThreadPool.RegisterWaitForSingleObject(showSettingsRequested, (_, _) => Dispatcher.BeginInvoke(ShowSettings), null, Timeout.Infinite, executeOnlyOnce: false);

        if (apiKeyStore.Read() is null)
            ShowSettings();
        if (TrayPin.ShouldShowHint())
            MessageWindow.ShowTrayIconHidden();

        var unreachable = await scout.CheckConnectionAsync();
        if (unreachable.Count > 0)
            MessageWindow.ShowCantConnect(unreachable);

        _ = Updater.RunAsync(log, stopping.Token);
        await PollAsync(scout);
    }

    async Task PollAsync(Scout scout)
    {
        using var timer = new PeriodicTimer(Scout.PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stopping.Token))
            {
                try
                {
                    await scout.PollAsync();
                }
                catch (Exception e)
                {
                    log.Write($"Unexpected error: {e}");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void ShowSettings()
    {
        if (settingsWindow is null)
        {
            settingsWindow = new SettingsWindow(new SettingsViewModel(settings));
            settingsWindow.Closed += delegate { settingsWindow = null; };
            settingsWindow.Show();
        }
        settingsWindow.Activate();
    }

    public void OpenTodaysLog()
    {
        var path = File.Exists(log.TodaysFile) ? log.TodaysFile : Path.GetDirectoryName(log.TodaysFile)!;
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    void IAppActions.Exit()
    {
        log.Write("Exiting");
        stopping.Cancel();
        trayIcon?.Dispose();
        Shutdown();
    }
}
