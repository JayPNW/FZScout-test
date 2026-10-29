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
    readonly FakeNotifier _notifier = new();
    readonly EqFolder _eq = new();
    readonly Scout _scout;
    readonly string _bowslinger;

    public ScoutTests()
    {
        this._scout = new(this._processes, this._api, this._apiKeyStore, this._notifier, new ListLog(), this._time);
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
        var changes = 0;
        this._scout.StatusChanged += (_, _) => changes++;

        await Poll();
        this._eq.Append(this._bowslinger, "You have entered The Halls of Honor.");
        this._eq.Append(this._eq.LogPath("Kixu", "frostreaver"), "You have entered The Bazaar.");
        await Poll();
        await Poll();

        Assert.Equal(1, changes);
        Assert.Equal(TrayIconState.Active, this._scout.Status.IconState);
        Assert.Equal(
            [new CharacterStatus("Bowslinger", "Fangbreaker", "Halls of Honor"), new CharacterStatus("Kixu", "Frostreaver", null)],
            this._scout.Status.Characters.OrderBy(c => c.Name));
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
