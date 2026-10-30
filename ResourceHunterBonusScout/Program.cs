using System.Reflection;
using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Reporting;
using ResourceHunterBonusScout.Site;

namespace ResourceHunterBonusScout;

static class Program
{
    static async Task Main()
    {
        var time = TimeProvider.System;
        var log = Log.ForConsole(time);

        using var mutex = new Mutex(true, "ResourceHunterBonusScout", out var isOnlyInstance);
        if (!isOnlyInstance)
        {
            log.Error("Resource Hunter Bonus Scout is already running.");
            log.Info("Press any key to exit.");
            Console.ReadKey(intercept: true);
            return;
        }

        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        log.Info($"Resource Hunter Bonus Scout {version}");

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var client = new FangbreakerZoneClient(http);
        var lists = new ServerListCache(client, log);
        var reporter = new BonusReporter(client, lists, log);
        var prompt = new ApiKeyPrompt(reporter, log);
        var scout = new Scout(new LogWatcher(log), lists, reporter, EqProcesses.Find);

        using var cancel = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancel.Cancel();
        };

        await reporter.ChangeApiKeyAsync(ApiKeyStore.Load(), cancel.Token);
        await TickAsync(scout, time, log, cancel.Token);
        PrintWatchedLogs(scout.Watcher, log);
        log.Info("Press Ctrl+C to exit.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), time);
        try
        {
            while (await timer.WaitForNextTickAsync(cancel.Token))
            {
                await prompt.TickAsync(cancel.Token);
                await TickAsync(scout, time, log, cancel.Token);
            }
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
        }
    }

    /// <summary>One bad tick is logged, not fatal: the next one gets a fresh start.</summary>
    static async Task TickAsync(Scout scout, TimeProvider time, Log log, CancellationToken cancellationToken)
    {
        try
        {
            await scout.TickAsync(time.GetUtcNow(), cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.Error($"Unexpected problem: {e}");
        }
    }

    static void PrintWatchedLogs(LogWatcher watcher, Log log)
    {
        if (watcher.WatchedLogs.Count == 0)
        {
            log.Info("No EverQuest logs found yet. Looking every 30 seconds while EverQuest runs.");
            return;
        }

        foreach (var folder in watcher.WatchedLogs.Select(Path.GetDirectoryName).Distinct())
        {
            log.Info($"EverQuest logs found in {folder}");
        }
    }
}
