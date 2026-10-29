using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Logs;

namespace ResourceHunterBonusScout.Tests;

public sealed class LogWatcherTests : IDisposable
{
    readonly FakeTimeProvider _time = new();
    readonly FakeEqProcesses _processes = new();
    readonly EqFolder _eq = new();
    readonly LogWatcher _watcher;
    readonly string _bowslinger;

    public LogWatcherTests()
    {
        this._watcher = new(this._processes, new ListLog(), this._time);
        this._bowslinger = this._eq.LogPath("Bowslinger", "fangbreaker");
    }

    public void Dispose() => this._eq.Dispose();

    LogWatcherPoll Scan()
    {
        this._time.Advance(LogWatcher.ScanInterval);
        return this._watcher.Poll();
    }

    static IEnumerable<string> Lines(LogWatcherPoll poll) => poll.NewLines.SelectMany(l => l.Lines);

    [Fact]
    public void TailsALogFromWhereItWasWhenItGrows()
    {
        this._eq.Append(this._bowslinger, "You have entered Crushbone.");
        this._processes.Running.Add(new(1, this._eq.ExePath));
        Scan();
        Assert.Empty(this._watcher.ActiveLogs);

        this._eq.Append(this._bowslinger, "You have entered The Halls of Honor.");
        var poll = Scan();

        Assert.Equal([new ActiveLog(this._bowslinger, "Bowslinger", Server.Fangbreaker)], this._watcher.ActiveLogs);
        Assert.Equal(["[Mon Sep 14 16:10:29 2026] You have entered The Halls of Honor."], Lines(poll));
    }

    [Fact]
    public void ReadsBetweenScans()
    {
        this._processes.Running.Add(new(1, this._eq.ExePath));
        Scan();
        this._eq.Append(this._bowslinger, "one");
        Scan();

        this._eq.Append(this._bowslinger, "two");
        Assert.Equal(["[Mon Sep 14 16:10:29 2026] two"], Lines(this._watcher.Poll()));
    }

    [Fact]
    public void NewLogIsReadFromTheStart()
    {
        this._processes.Running.Add(new(1, this._eq.ExePath));
        Scan();

        this._eq.Append(this._bowslinger, "Welcome to EverQuest!", "You have entered Crushbone.");

        Assert.Equal(2, Lines(Scan()).Count());
    }

    [Fact]
    public void LineWithoutItsNewlineWaits()
    {
        this._processes.Running.Add(new(1, this._eq.ExePath));
        Scan();

        File.AppendAllText(this._bowslinger, "You have entered ");
        Assert.Empty(Lines(Scan()));
        File.AppendAllText(this._bowslinger, "Crushbone.\r\n");
        Assert.Equal(["You have entered Crushbone."], Lines(this._watcher.Poll()));
    }

    [Fact]
    public void IgnoresLogsForOtherServers()
    {
        this._processes.Running.Add(new(1, this._eq.ExePath));
        Scan();

        this._eq.Append(this._eq.LogPath("Bowslinger", "vaniki"), "You have entered Crushbone.");
        Scan();

        Assert.Empty(this._watcher.ActiveLogs);
    }

    [Fact]
    public void TailsEachLogOnceForProcessesSharingAFolder()
    {
        this._processes.Running.Add(new(1, this._eq.ExePath));
        this._processes.Running.Add(new(2, this._eq.ExePath));
        Scan();

        this._eq.Append(this._bowslinger, "one");
        this._eq.Append(this._eq.LogPath("Kixu", "frostreaver"), "two");

        Assert.Equal(2, Lines(Scan()).Count());
        Assert.Equal(2, this._watcher.ActiveLogs.Count);
    }

    [Fact]
    public void StopsWhenNoEqProcessFromTheFolderIsRunning()
    {
        this._processes.Running.Add(new(1, this._eq.ExePath));
        this._processes.Running.Add(new(2, this._eq.ExePath));
        Scan();
        this._eq.Append(this._bowslinger, "one");
        Scan();

        this._processes.Running.RemoveAt(0);
        Scan();
        Assert.Single(this._watcher.ActiveLogs);

        this._processes.Running.Clear();
        Scan();
        Assert.Empty(this._watcher.ActiveLogs);
    }

    [Fact]
    public void LoggingHintAfterFiveMinutesWithoutGrowthOncePerProcess()
    {
        this._eq.Append(this._bowslinger, "old");
        this._processes.Running.Add(new(1, this._eq.ExePath));
        this._watcher.Poll();

        this._time.Advance(LogWatcher.LoggingHintDelay - LogWatcher.ScanInterval);
        Assert.False(this._watcher.Poll().LoggingHintNeeded);
        Assert.True(Scan().LoggingHintNeeded);
        Assert.False(Scan().LoggingHintNeeded);

        this._processes.Running.Add(new(2, this._eq.ExePath));
        Scan();
        this._time.Advance(LogWatcher.LoggingHintDelay);
        Assert.True(this._watcher.Poll().LoggingHintNeeded);
    }

    [Fact]
    public void NoLoggingHintWhenAnyLogGrows()
    {
        this._processes.Running.Add(new(1, this._eq.ExePath));
        Scan();
        this._eq.Append(this._eq.LogPath("Bowslinger", "vaniki"), "one");
        Scan();

        this._time.Advance(LogWatcher.LoggingHintDelay);
        Assert.False(this._watcher.Poll().LoggingHintNeeded);
    }
}
