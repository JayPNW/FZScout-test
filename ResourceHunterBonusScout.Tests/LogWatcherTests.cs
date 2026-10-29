using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

public sealed class LogWatcherTests : IDisposable
{
    readonly FakeTimeProvider time = new();
    readonly FakeEqProcesses processes = new();
    readonly EqFolder eq = new();
    readonly LogWatcher watcher;
    readonly string bowslinger;

    public LogWatcherTests()
    {
        watcher = new(processes, new ListLog(), time);
        bowslinger = eq.LogPath("Bowslinger", "fangbreaker");
    }

    public void Dispose() => eq.Dispose();

    LogWatcherPoll Scan()
    {
        time.Advance(LogWatcher.ScanInterval);
        return watcher.Poll();
    }

    static IEnumerable<string> Lines(LogWatcherPoll poll) => poll.NewLines.SelectMany(l => l.Lines);

    [Fact]
    public void TailsALogFromWhereItWasWhenItGrows()
    {
        eq.Append(bowslinger, "You have entered Crushbone.");
        processes.Running.Add(new(1, eq.ExePath));
        Scan();
        Assert.Empty(watcher.ActiveLogs);

        eq.Append(bowslinger, "You have entered The Halls of Honor.");
        var poll = Scan();

        Assert.Equal([new ActiveLog(bowslinger, "Bowslinger", Server.Fangbreaker)], watcher.ActiveLogs);
        Assert.Equal(["[Mon Sep 14 16:10:29 2026] You have entered The Halls of Honor."], Lines(poll));
    }

    [Fact]
    public void ReadsBetweenScans()
    {
        processes.Running.Add(new(1, eq.ExePath));
        Scan();
        eq.Append(bowslinger, "one");
        Scan();

        eq.Append(bowslinger, "two");
        Assert.Equal(["[Mon Sep 14 16:10:29 2026] two"], Lines(watcher.Poll()));
    }

    [Fact]
    public void NewLogIsReadFromTheStart()
    {
        processes.Running.Add(new(1, eq.ExePath));
        Scan();

        eq.Append(bowslinger, "Welcome to EverQuest!", "You have entered Crushbone.");

        Assert.Equal(2, Lines(Scan()).Count());
    }

    [Fact]
    public void LineWithoutItsNewlineWaits()
    {
        processes.Running.Add(new(1, eq.ExePath));
        Scan();

        File.AppendAllText(bowslinger, "You have entered ");
        Assert.Empty(Lines(Scan()));
        File.AppendAllText(bowslinger, "Crushbone.\r\n");
        Assert.Equal(["You have entered Crushbone."], Lines(watcher.Poll()));
    }

    [Fact]
    public void IgnoresLogsForOtherServers()
    {
        processes.Running.Add(new(1, eq.ExePath));
        Scan();

        eq.Append(eq.LogPath("Bowslinger", "vaniki"), "You have entered Crushbone.");
        Scan();

        Assert.Empty(watcher.ActiveLogs);
    }

    [Fact]
    public void TailsEachLogOnceForProcessesSharingAFolder()
    {
        processes.Running.Add(new(1, eq.ExePath));
        processes.Running.Add(new(2, eq.ExePath));
        Scan();

        eq.Append(bowslinger, "one");
        eq.Append(eq.LogPath("Kixu", "frostreaver"), "two");

        Assert.Equal(2, Lines(Scan()).Count());
        Assert.Equal(2, watcher.ActiveLogs.Count);
    }

    [Fact]
    public void StopsWhenNoEqProcessFromTheFolderIsRunning()
    {
        processes.Running.Add(new(1, eq.ExePath));
        processes.Running.Add(new(2, eq.ExePath));
        Scan();
        eq.Append(bowslinger, "one");
        Scan();

        processes.Running.RemoveAt(0);
        Scan();
        Assert.Single(watcher.ActiveLogs);

        processes.Running.Clear();
        Scan();
        Assert.Empty(watcher.ActiveLogs);
    }

    [Fact]
    public void LoggingHintAfterFiveMinutesWithoutGrowthOncePerProcess()
    {
        eq.Append(bowslinger, "old");
        processes.Running.Add(new(1, eq.ExePath));
        watcher.Poll();

        time.Advance(LogWatcher.LoggingHintDelay - LogWatcher.ScanInterval);
        Assert.False(watcher.Poll().LoggingHintNeeded);
        Assert.True(Scan().LoggingHintNeeded);
        Assert.False(Scan().LoggingHintNeeded);

        processes.Running.Add(new(2, eq.ExePath));
        Scan();
        time.Advance(LogWatcher.LoggingHintDelay);
        Assert.True(watcher.Poll().LoggingHintNeeded);
    }

    [Fact]
    public void NoLoggingHintWhenAnyLogGrows()
    {
        processes.Running.Add(new(1, eq.ExePath));
        Scan();
        eq.Append(eq.LogPath("Bowslinger", "vaniki"), "one");
        Scan();

        time.Advance(LogWatcher.LoggingHintDelay);
        Assert.False(watcher.Poll().LoggingHintNeeded);
    }
}
