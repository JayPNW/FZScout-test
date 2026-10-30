using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Logs;

namespace ResourceHunterBonusScout.Tests.Logs;

public sealed class LogWatcherTests : IDisposable
{
    const string Befallen = "[Thu Oct 29 12:00:00 2026] You have entered Befallen.\r\n";

    static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    readonly TempFolder _everQuest = new();
    readonly StringWriter _output = new();
    readonly LogWatcher _watcher;
    readonly EqProcess _eq;

    public LogWatcherTests()
    {
        this._watcher = new LogWatcher(new Log(this._output, new FakeTimeProvider(Start)));
        Directory.CreateDirectory(this.LogsFolder);
        this._eq = new EqProcess(100, this._everQuest.File("eqgame.exe"));
    }

    string LogsFolder => Path.Combine(this._everQuest.Path, "Logs");

    public void Dispose() => this._everQuest.Dispose();

    [Fact]
    public void ReportsEventsFromNewLines()
    {
        this.WriteLog("eqlog_Bob_fangbreaker.txt", "[Thu Oct 29 11:00:00 2026] You have entered Crushbone.\r\n");
        this._watcher.Scan([this._eq], Start);

        this.WriteLog("eqlog_Bob_fangbreaker.txt", $"{Befallen}[Thu Oct 29 12:00:01 2026] Some chatter\r\n");

        Assert.Equal(
            [new CharacterLogEvent(new Character("Bob", Server.Fangbreaker), new ZoneEnteredLogEvent("Befallen"))],
            this._watcher.Poll());
    }

    [Fact]
    public void ALogIsActiveOnceItGrows()
    {
        this.WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);
        this.WriteLog("eqlog_Ann_frostreaver.txt", Befallen);
        this._watcher.Scan([this._eq], Start);
        this._watcher.Poll();
        Assert.Empty(this._watcher.ActiveLogs);

        this.WriteLog("eqlog_Ann_frostreaver.txt", Befallen);
        this._watcher.Poll();

        var ann = Path.Combine(this.LogsFolder, "eqlog_Ann_frostreaver.txt");
        Assert.Equal([ann], this._watcher.ActiveLogs);
        Assert.Equal($"12:00:00 Watching {ann}", this.Output());
    }

    [Fact]
    public void LogsForOtherServersAreIgnored()
    {
        this.WriteLog("eqlog_Bob_teek.txt", string.Empty);
        this._watcher.Scan([this._eq], Start);

        this.WriteLog("eqlog_Bob_teek.txt", Befallen);

        Assert.Empty(this._watcher.Poll());
        Assert.Empty(this._watcher.ActiveLogs);
    }

    [Fact]
    public void ALogCreatedWhileWatchingIsReadFromItsStart()
    {
        this._watcher.Scan([this._eq], Start);

        this.WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);
        this._watcher.Scan([this._eq], Start.AddSeconds(30));

        Assert.Single(this._watcher.Poll());
    }

    [Fact]
    public void SeveralProcessesInOneFolderShareOneTail()
    {
        this.WriteLog("eqlog_Bob_fangbreaker.txt", string.Empty);
        this._watcher.Scan([this._eq, this._eq with { Id = 101 }], Start);

        this.WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);

        Assert.Single(this._watcher.Poll());
    }

    [Fact]
    public void StopsWatchingWhenEverQuestExits()
    {
        this.WriteLog("eqlog_Bob_fangbreaker.txt", string.Empty);
        this._watcher.Scan([this._eq], Start);
        this.WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);
        this._watcher.Poll();

        this._watcher.Scan([], Start.AddSeconds(30));
        this.WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);

        Assert.Empty(this._watcher.Poll());
        Assert.Empty(this._watcher.ActiveLogs);
        Assert.EndsWith($"Stopped watching {Path.Combine(this.LogsFolder, "eqlog_Bob_fangbreaker.txt")}", this.Output());
    }

    [Fact]
    public void WhenEverQuestStartsAgainItStartsAtTheEnd()
    {
        this.WriteLog("eqlog_Bob_fangbreaker.txt", string.Empty);
        this._watcher.Scan([this._eq], Start);
        this._watcher.Scan([], Start.AddSeconds(30));
        this.WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);

        this._watcher.Scan([this._eq with { Id = 200 }], Start.AddSeconds(60));

        Assert.Empty(this._watcher.Poll());
    }

    [Fact]
    public void HintsToTurnOnLoggingWhenNoLogGrowsForFiveMinutes()
    {
        this.WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);
        this._watcher.Scan([this._eq], Start);

        this._watcher.Scan([this._eq], Start.AddSeconds(270));
        Assert.Equal(string.Empty, this.Output());

        this._watcher.Scan([this._eq], Start.AddSeconds(300));
        Assert.Equal($"12:00:00 WARNING: {LogWatcher.LoggingHint}", this.Output());
    }

    [Fact]
    public void HintsOnlyOncePerProcess()
    {
        this._watcher.Scan([this._eq], Start);
        this._watcher.Scan([this._eq], Start.AddMinutes(5));
        this._watcher.Scan([this._eq], Start.AddMinutes(10));

        Assert.Single(this.Output().Split('\n'));
    }

    [Fact]
    public void AnotherProcessGetsItsOwnHint()
    {
        this._watcher.Scan([this._eq], Start);
        this._watcher.Scan([this._eq], Start.AddMinutes(5));
        this._watcher.Scan([this._eq with { Id = 200 }], Start.AddMinutes(6));
        this._watcher.Scan([this._eq with { Id = 200 }], Start.AddMinutes(11));

        Assert.Equal(2, this.Output().Split('\n').Length);
    }

    [Fact]
    public void NoHintWhenAnyLogGrows()
    {
        this.WriteLog("eqlog_Bob_teek.txt", Befallen);
        this._watcher.Scan([this._eq], Start);

        this.WriteLog("eqlog_Bob_teek.txt", Befallen);
        this._watcher.Scan([this._eq], Start.AddSeconds(30));
        this._watcher.Scan([this._eq], Start.AddMinutes(10));

        Assert.Equal(string.Empty, this.Output());
    }

    [Fact]
    public void NoHintWhenANewLogAppears()
    {
        this._watcher.Scan([this._eq], Start);

        this.WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);
        this._watcher.Scan([this._eq], Start.AddMinutes(10));

        Assert.Equal(string.Empty, this.Output());
    }

    [Fact]
    public void MissingLogsFolderIsFine()
    {
        Directory.Delete(this.LogsFolder);

        this._watcher.Scan([this._eq], Start);

        Assert.Empty(this._watcher.Poll());
    }

    [Fact]
    public void FindingEverQuestProcessesDoesNotThrow()
    {
        Assert.NotNull(EqProcesses.Find());
    }

    void WriteLog(string name, string text) => File.AppendAllText(Path.Combine(this.LogsFolder, name), text);

    string Output() => this._output.ToString().TrimEnd();
}
