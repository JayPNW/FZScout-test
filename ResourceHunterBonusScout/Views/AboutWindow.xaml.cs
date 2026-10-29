using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Navigation;

namespace ResourceHunterBonusScout.Views;

/// <summary>Shows the version, thanks, and the attribution the icon licenses require.</summary>
public partial class AboutWindow : Window
{
    public AboutWindow(string version)
    {
        InitializeComponent();
        this.VersionText.Text = $"Version {version}";
        this.Credits.Markdown = ReadCreditsFile();
    }

    static string ReadCreditsFile()
    {
        using var stream = typeof(AboutWindow).Assembly.GetManifestResourceStream("CREDITS.md")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    void OnOk(object sender, RoutedEventArgs e) => Close();

    void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
