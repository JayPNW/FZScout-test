using H.NotifyIcon;
using H.NotifyIcon.Core;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Views;

class TrayNotifier : INotifier
{
    readonly TaskbarIcon trayIcon;
    bool clickOpensSettings;

    public TrayNotifier(TaskbarIcon trayIcon, Action showSettings)
    {
        this.trayIcon = trayIcon;
        trayIcon.TrayBalloonTipClicked += (_, _) =>
        {
            if (clickOpensSettings)
                showSettings();
        };
    }

    public void ApiKeyRejected()
        => Show("API key rejected", "The site didn't accept your API key, so reporting has stopped. Click here to enter a new key.", clickOpensSettings: true);

    public void Banned(Server server)
        => Show($"Reports to {server.Site.Host} refused", $"{server.Site.Host} says your account is banned or timed out. Reporting to it stops until the app restarts.");

    public void LoggingOff()
        => Show("No EverQuest log is growing", "Type /log on in EverQuest.");

    void Show(string title, string message, bool clickOpensSettings = false)
    {
        this.clickOpensSettings = clickOpensSettings;
        trayIcon.ShowNotification(title, message, NotificationIcon.Warning);
    }
}
