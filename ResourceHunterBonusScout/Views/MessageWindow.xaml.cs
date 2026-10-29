using System.Windows;

namespace ResourceHunterBonusScout.Views;

/// <summary>The windows that can appear at startup.</summary>
public partial class MessageWindow : Window
{
    MessageWindow(string title, string message)
    {
        InitializeComponent();
        Title = title;
        this.Message.Text = message;
    }

    void OnOk(object sender, RoutedEventArgs e) => Close();

    static void Show(string title, string message)
    {
        var window = new MessageWindow(title, message);
        window.Show();
        window.Activate();
    }

    public static void ShowTrayIconHidden() => Show(
        "Pin the Resource Hunter Bonus Scout icon",
        "Resource Hunter Bonus Scout runs in the system tray, but Windows has hidden its icon behind the ^ arrow next to the clock.\n\n"
        + "To keep it in sight, click the ^ arrow and drag the icon onto the taskbar. "
        + "Or right-click the taskbar, choose Taskbar settings, open Other system tray icons, and turn on Resource Hunter Bonus Scout.");

    public static void ShowCantConnect(IEnumerable<Server> servers) => Show(
        "Can't connect",
        $"Resource Hunter Bonus Scout can't reach {string.Join(" or ", servers.Select(s => s.Site.Host))}. Check your internet connection.\n\n"
        + "The app keeps running in the tray and tries again when it needs to.");
}
