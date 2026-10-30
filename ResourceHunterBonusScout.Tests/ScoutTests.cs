using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Reporting;
using ResourceHunterBonusScout.Site;
using ResourceHunterBonusScout.Tests.Logs;
using ResourceHunterBonusScout.Tests.Reporting;

namespace ResourceHunterBonusScout.Tests;

/// <summary>The whole app, from a real log file on disk to requests to the site.</summary>
public sealed class ScoutTests : IDisposable
{
    static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    readonly TempFolder _everQuest = new();
    readonly FakeSite _site = new();
    readonly StringWriter _output = new();
    readonly Scout _scout;
    readonly string _log;
    List<EqProcess> _processes;

    public ScoutTests()
    {
        var logs = Path.Combine(this._everQuest.Path, "Logs");
        Directory.CreateDirectory(logs);
        this._log = Path.Combine(logs, "eqlog_Bob_fangbreaker.txt");
        File.WriteAllText(this._log, string.Empty);
        this._processes = [new EqProcess(100, this._everQuest.File("eqgame.exe"))];

        var log = new Log(this._output, new FakeTimeProvider(Start));
        var client = this._site.Client;
        var lists = new ServerListCache(client, log);
        var reporter = new BonusReporter(client, lists, log);
        reporter.SetApiKey(new ApiKey("fz_live_test"));
        this._scout = new Scout(new LogWatcher(log), lists, reporter, () => this._processes);
    }

    static CancellationToken Cancel => TestContext.Current.CancellationToken;

    public void Dispose() => this._everQuest.Dispose();

    [Fact]
    public async Task ReportsABonusFromTheLog()
    {
        await this.Tick(0);

        this.Write("You have entered Clan Crushbone.");
        await this.Tick(1);
        this.Write("You have slain an orc pawn!");
        this.Write("This land is saturated with unstable magic, multiplying the loot that may be found here.");
        await this.Tick(2);

        Assert.Equal(
            [
                "GET /api/servers/fangbreaker/bonuses/types",
                "GET /api/servers/fangbreaker/bonuses/zones",
                """POST /api/client/bonuses {"serverName":"Fangbreaker","zoneName":"Crushbone","bonusKey":"loot"}""",
            ],
            this._site.Requests);
    }

    [Fact]
    public async Task ReportsNoneWhenAKillBringsNoBonus()
    {
        await this.Tick(0);

        this.Write("You have entered Befallen.");
        this.Write("You have slain a skeleton!");
        await this.Tick(1);
        await this.Tick(5);
        Assert.DoesNotContain(this._site.Requests, r => r.StartsWith("POST", StringComparison.Ordinal));

        await this.Tick(6);
        Assert.Equal(
            """POST /api/client/bonuses {"serverName":"Fangbreaker","zoneName":"Befallen","bonusKey":"none"}""",
            this._site.Requests[^1]);
    }

    [Fact]
    public async Task ScansForEverQuestEveryThirtySeconds()
    {
        this._processes = [];
        await this.Tick(0);

        this._processes = [new EqProcess(100, this._everQuest.File("eqgame.exe"))];
        await this.Tick(29);
        this.Write("You have entered Befallen.");
        await this.Tick(29);
        Assert.Empty(this._scout.Watcher.ActiveLogs);

        await this.Tick(30);
        this.Write("You have entered Befallen.");
        await this.Tick(31);
        Assert.Equal([this._log], this._scout.Watcher.ActiveLogs);
    }

    void Write(string message) => File.AppendAllText(this._log, $"[Thu Oct 29 12:00:00 2026] {message}\r\n");

    Task Tick(int seconds) => this._scout.TickAsync(Start.AddSeconds(seconds), Cancel);
}
