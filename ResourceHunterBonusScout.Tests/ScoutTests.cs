using System.Net;
using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Reporting;

namespace ResourceHunterBonusScout.Tests;

public sealed class ScoutTests : IDisposable
{
    readonly FakeTimeProvider _time = new();
    readonly FakeEqProcesses _processes = new();
    readonly FakeBonusApi _api = new();
    readonly InMemoryApiKeyStore _apiKeyStore = new() { ApiKey = "fz_live_abc" };
    readonly InMemorySettingsStore _settingsStore = new();
    readonly FakeNotifier _notifier = new();
    readonly EqFolder _eq = new();
    readonly ListLog _log = new();
    readonly Scout _scout;
    readonly string _bowslinger;

    public ScoutTests()
    {
        this._scout = new(this._processes, this._api, this._apiKeyStore, this._settingsStore, this._notifier, this._log, this._time);
        this._bowslinger = this._eq.LogPath("Bowslinger", "fangbreaker");
        this._processes.Running.Add(new(1, this._eq.ExePath));
    }

    public void Dispose() => this._eq.Dispose();

    async Task Poll()
    {
        this._time.Advance(LogWatcher.ScanInterval);
        await this._scout.PollAsync();
    }

    [Fact]
    public async Task IdleTimerSettingTurnsOffTheFiveMinuteNoBonusReport()
    {
        this._settingsStore.Settings = new(IdleTimer: false);
        await Poll();
        this._eq.Append(this._bowslinger, "You have entered Crushbone.");
        await Poll();

        this._time.Advance(CharacterTracker.NoBonusDelay);
        await this._scout.PollAsync();

        Assert.Empty(this._api.Reports);
    }

    [Fact]
    public async Task ReportsBonusesFromTheLog()
    {
        await Poll();
        this._eq.Append(this._bowslinger,
            "Welcome to EverQuest!",
            "You have entered The Halls of Honor.",
            "You have slain a Stillmoon leopard!",
            "This land is saturated with unstable magic, multiplying the loot that may be found here.");
        await Poll();

        Assert.Equal([new BonusReport("Bowslinger", Server.Fangbreaker, "Halls of Honor", "loot")], this._api.Reports.Select(r => r.Report));
    }

    [Fact]
    public async Task ReportsNoBonus()
    {
        await Poll();
        this._eq.Append(this._bowslinger, "You have entered Crushbone.");
        await Poll();

        this._time.Advance(CharacterTracker.NoBonusDelay);
        await this._scout.PollAsync();

        Assert.Equal([new BonusReport("Bowslinger", Server.Fangbreaker, "Crushbone", "none")], this._api.Reports.Select(r => r.Report));
    }

    [Fact]
    public async Task StatusShowsWhenNoBonusWillBeReported()
    {
        await Poll();
        this._eq.Append(this._bowslinger, "You have entered Crushbone.");
        await Poll();
        Assert.Equal(this._time.GetUtcNow() + CharacterTracker.NoBonusDelay, this._scout.Status.Characters.Single().NoBonusAt);

        this._eq.Append(this._bowslinger, "You have slain an orc pawn!");
        await Poll();
        Assert.Equal(this._time.GetUtcNow() + CharacterTracker.AfterKillDelay, this._scout.Status.Characters.Single().NoBonusAt);

        this._time.Advance(CharacterTracker.AfterKillDelay);
        await this._scout.PollAsync();
        Assert.Null(this._scout.Status.Characters.Single().NoBonusAt);
    }

    [Fact]
    public async Task LogsTheNoBonusTimer()
    {
        await Poll();
        this._eq.Append(this._bowslinger, "You have entered Crushbone.");
        await Poll();
        this._eq.Append(this._bowslinger,
            "You have slain an orc pawn!",
            "It will take you about 30 seconds to prepare your camp.",
            "You abandon your preparations to camp.");
        await Poll();
        this._processes.Running.Clear();
        await Poll();

        Assert.Equal(
            [
                "Bowslinger (Fangbreaker) entered Crushbone; waiting for timeout at 00:05:06.",
                "Bowslinger (Fangbreaker) got a kill; waiting for timeout at 00:00:19.",
                "Bowslinger (Fangbreaker) started to camp; timeout paused.",
                "Bowslinger (Fangbreaker) stopped camping; waiting for timeout at 00:00:19.",
                "Bowslinger (Fangbreaker) log is no longer watched; timeout cancelled.",
            ],
            this._log.Lines.Where(line => line.StartsWith("Bowslinger (Fangbreaker) ", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task LogsAPersonaChange()
    {
        await Poll();
        this._eq.Append(this._bowslinger, "You have entered Crushbone.");
        await Poll();
        this._eq.Append(this._bowslinger, "You successfully loaded your Druid equipment set.");
        await Poll();

        Assert.Equal(
            "Bowslinger (Fangbreaker) changed persona; waiting for timeout at 00:05:09.",
            this._log.Lines.Last(line => line.StartsWith("Bowslinger (Fangbreaker) ", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task TimerIsCancelledWhenEqExits()
    {
        await Poll();
        this._eq.Append(this._bowslinger, "You have entered Crushbone.");
        await Poll();

        this._processes.Running.Clear();
        await Poll();
        this._time.Advance(CharacterTracker.NoBonusDelay);
        await this._scout.PollAsync();

        Assert.Empty(this._api.Reports);
    }

    [Fact]
    public async Task StatusListsCharactersAndZones()
    {
        await Poll();
        var changes = 0;
        this._scout.StatusChanged += (_, _) => changes++;

        this._eq.Append(this._bowslinger, "You have entered The Halls of Honor.");
        this._eq.Append(this._eq.LogPath("Kixu", "frostreaver"), "Welcome to EverQuest!");
        await Poll();
        var entered = this._time.GetUtcNow();
        await Poll();

        Assert.Equal(1, changes);
        Assert.Equal(TrayIconState.Waiting, this._scout.Status.IconState);
        Assert.Equal(
            [new CharacterStatus("Bowslinger", "Fangbreaker", "Halls of Honor", entered + CharacterTracker.NoBonusDelay), new CharacterStatus("Kixu", "Frostreaver", null)],
            this._scout.Status.Characters.OrderBy(c => c.Name));
    }

    [Fact]
    public async Task StatusTracksWhetherEqIsRunning()
    {
        Assert.False(this._scout.Status.EqRunning);

        await Poll();
        Assert.True(this._scout.Status.EqRunning);

        this._processes.Running.Clear();
        await Poll();
        Assert.False(this._scout.Status.EqRunning);
    }

    [Fact]
    public async Task LoadsListsWhenAServersFirstLogBecomesActive()
    {
        await Poll();
        Assert.Equal(0, this._api.ListLoads);

        this._eq.Append(this._bowslinger, "Welcome to EverQuest!");
        await Poll();
        Assert.Equal(1, this._api.ListLoads);
    }

    [Fact]
    public async Task FailedReportShowsTheErrorIcon()
    {
        this._api.Respond = _ => HttpStatusCode.InternalServerError;
        await Poll();
        this._eq.Append(this._bowslinger, "You have entered Crushbone.", "This land is saturated with unstable magic, multiplying the coin that may be found here.");
        await Poll();

        Assert.Equal(TrayIconState.Error, this._scout.Status.IconState);
    }

    [Fact]
    public async Task ZoneNotOnTheListIsAnError()
    {
        await Poll();
        this._eq.Append(this._eq.LogPath("Kixu", "frostreaver"), "You have entered The Bazaar.");
        await Poll();

        Assert.Equal(TrayIconState.Error, this._scout.Status.IconState);
        Assert.Contains("ERROR: Kixu (Frostreaver) entered The Bazaar, which isn't on the zones list.", this._log.Lines);
    }

    [Fact]
    public async Task ZoneThatNeverHasABonusIsIdle()
    {
        await Poll();
        this._eq.Append(this._eq.LogPath("Kixu", "frostreaver"), "You have entered The Plane of Tranquility.");
        await Poll();

        Assert.Equal(TrayIconState.Idle, this._scout.Status.IconState);
        Assert.Contains("Kixu (Frostreaver) entered The Plane of Tranquility, which never has a bonus.", this._log.Lines);
    }

    [Fact]
    public async Task BonusInAZoneThatNeverHasABonusIsAnError()
    {
        await Poll();
        this._eq.Append(this._bowslinger,
            "You have entered The Plane of Knowledge.",
            "This land is saturated with unstable magic, multiplying the loot that may be found here.");
        await Poll();

        Assert.Equal(TrayIconState.Error, this._scout.Status.IconState);
        Assert.Contains("ERROR: Bowslinger (Fangbreaker) got the loot bonus message in The Plane of Knowledge, which should never have a bonus.", this._log.Lines);
        Assert.Empty(this._api.Reports);
    }

    [Fact]
    public async Task NoKeyShowsTheErrorIcon()
    {
        this._apiKeyStore.ApiKey = null;
        await Poll();

        Assert.Equal(TrayIconState.Error, this._scout.Status.IconState);
    }

    [Fact]
    public async Task LoggingHint()
    {
        await Poll();
        this._time.Advance(LogWatcher.LoggingHintDelay);
        await this._scout.PollAsync();

        Assert.Equal(["LoggingOff"], this._notifier.Notifications);
    }

    [Fact]
    public async Task ConnectionCheckListsUnreachableSites()
    {
        this._api.ListFailure = new("network");

        Assert.Equal(Server.All, await this._scout.CheckConnectionAsync());
        Assert.Equal(TrayIconState.Error, this._scout.Status.IconState);
    }
}
