using Velopack;

namespace ResourceHunterBonusScout;

static class Program
{
    const string MutexName = @"Local\ResourceHunterBonusScout";
    const string ShowSettingsEventName = @"Local\ResourceHunterBonusScout.ShowSettings";

    [STAThread]
    static void Main()
    {
        // Handles install and update hooks, and applies a downloaded update.
        VelopackApp.Build().Run();

        using var mutex = new Mutex(true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        using var showSettingsRequested = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
        var app = new App(showSettingsRequested);
        app.InitializeComponent();
        app.Run();
    }
}
