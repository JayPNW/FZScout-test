using System.Reflection;
using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Reporting;
using ResourceHunterBonusScout.Site;

namespace ResourceHunterBonusScout;

static class Program
{
    static async Task Main(string[] args)
    {
        using var mutex = new Mutex(true, "ResourceHunterBonusScout", out var isOnlyInstance);
        if (!isOnlyInstance)
        {
            Console.Error.WriteLine("Resource Hunter Bonus Scout is already running.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(intercept: true);
            return;
        }

        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        Console.WriteLine($"Resource Hunter Bonus Scout {version}");

        if (args.Contains("--set-api-key"))
        {
            AskForApiKey();
        }

        var apiKey = ApiKeyStore.Load() ?? AskForApiKey();
        if (apiKey is null)
        {
            Console.WriteLine("API key: missing. Nothing will be reported. See docs/create-api-key.md.");
        }
        else
        {
            Console.WriteLine("API key: found. If the site rejects it, run with --set-api-key to replace it.");
        }

        var time = TimeProvider.System;
        var log = new Log(Console.Out, time);
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var client = new FangbreakerZoneClient(http);
        var lists = new ServerListCache(client, log);
        var reporter = new BonusReporter(client, lists, log);
        reporter.SetApiKey(apiKey);
        var scout = new Scout(new LogWatcher(log), lists, reporter, EqProcesses.Find);

        using var cancel = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancel.Cancel();
        };

        Console.WriteLine("Watching for EverQuest logs. Each log is listed once it grows. Press Ctrl+C to exit.");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), time);
        try
        {
            do
            {
                await scout.TickAsync(time.GetUtcNow(), cancel.Token);
            }
            while (await timer.WaitForNextTickAsync(cancel.Token));
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
        }
    }

    static ApiKey? AskForApiKey()
    {
        Console.Write("Paste your API key (it starts with fz_live_), or press Enter to skip: ");
        var text = ReadHidden().Trim();
        if (text.Length == 0)
        {
            return null;
        }

        var apiKey = new ApiKey(text);
        ApiKeyStore.Save(apiKey);
        Console.WriteLine("Saved to Windows Credential Manager.");
        return apiKey;
    }

    /// <summary>Reads a line without echoing it, so the key doesn't end up on screen.</summary>
    static string ReadHidden()
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine() ?? string.Empty;
        }

        var text = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return text.ToString();
            }

            if (key.Key == ConsoleKey.Backspace && text.Length > 0)
            {
                text.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                text.Append(key.KeyChar);
            }
        }
    }
}
