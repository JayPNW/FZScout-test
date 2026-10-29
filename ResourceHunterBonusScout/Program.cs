using Velopack;

namespace ResourceHunterBonusScout;

static class Program
{

    [STAThread]
    static void Main()
    {
        // Handles install and update hooks, and applies a downloaded update.
        VelopackApp.Build().Run();

        const string MutexName = @"Local\ResourceHunterBonusScout";
        using var mutex = new Mutex(true, MutexName, out var isFirstInstance);
        if (isFirstInstance)
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
