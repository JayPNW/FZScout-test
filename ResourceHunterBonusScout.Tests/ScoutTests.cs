using System.Net;
using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Reporting;

namespace ResourceHunterBonusScout.Tests;

public sealed class ScoutTests : IDisposable
{
    readonly FakeTimeProvider time = new();
    readonly FakeEqProcesses processes = new();
    readonly FakeBonusApi api = new();
    readonly InMemoryApiKeyStore apiKeyStore = new() { ApiKey = "fz_live_abc" };
    readonly FakeNotifier notifier = new();
    readonly EqFolder eq = new();
    readonly Scout scout;
    readonly string bowslinger;

    public ScoutTests()
    {
        scout = new(processes, api, apiKeyStore, notifier, new ListLog(), time);
        bowslinger = eq.LogPath("Bowslinger", "fangbreaker");
        processes.Running.Add(new(1, eq.ExePath));
    }

    public void Dispose() => eq.Dispose();

    async Task Poll()
    {
        time.Advance(LogWatcher.ScanInterval);
        await scout.PollAsync();
    }

    [Fact]
    public async Task ReportsBonusesFromTheLog()
    {
        await Poll();
        eq.Append(bowslinger,
            "Welcome to EverQuest!",
            "You have entered The Halls of Honor.",
            "You have slain a Stillmoon leopard!",
            "This land is saturated with unstable magic, multiplying the loot that may be found here.");
        await Poll();

        Assert.Equal([new BonusReport("Bowslinger", Server.Fangbreaker, "Halls of Honor", "loot")], api.Reports.Select(r => r.Report));
    }

    [Fact]
    public async Task ReportsNoBonus()
    {
        await Poll();
        eq.Append(bowslinger, "You have entered Crushbone.");
        await Poll();

        time.Advance(CharacterTracker.NoBonusDelay);
        await scout.PollAsync();

        Assert.Equal([new BonusReport("Bowslinger", Server.Fangbreaker, "Crushbone", "none")], api.Reports.Select(r => r.Report));
    }

    [Fact]
    public async Task TimerIsCancelledWhenEqExits()
    {
        await Poll();
        eq.Append(bowslinger, "You have entered Crushbone.");
        await Poll();

        processes.Running.Clear();
        await Poll();
        time.Advance(CharacterTracker.NoBonusDelay);
        await scout.PollAsync();

        Assert.Empty(api.Reports);
    }

    [Fact]
    public async Task StatusListsCharactersAndZones()
    {
        var changes = 0;
        scout.StatusChanged += (_, _) => changes++;

        await Poll();
        eq.Append(bowslinger, "You have entered The Halls of Honor.");
        eq.Append(eq.LogPath("Kixu", "frostreaver"), "You have entered The Bazaar.");
        await Poll();
        await Poll();

        Assert.Equal(1, changes);
        Assert.Equal(TrayIconState.Active, scout.Status.IconState);
        Assert.Equal(
            [new CharacterStatus("Bowslinger", "Fangbreaker", "Halls of Honor"), new CharacterStatus("Kixu", "Frostreaver", null)],
            scout.Status.Characters.OrderBy(c => c.Name));
    }

    [Fact]
    public async Task LoadsListsWhenAServersFirstLogBecomesActive()
    {
        await Poll();
        Assert.Equal(0, api.ListLoads);

        eq.Append(bowslinger, "Welcome to EverQuest!");
        await Poll();
        Assert.Equal(1, api.ListLoads);
    }

    [Fact]
    public async Task FailedReportShowsTheErrorIcon()
    {
        api.Respond = _ => HttpStatusCode.InternalServerError;
        await Poll();
        eq.Append(bowslinger, "You have entered Crushbone.", "This land is saturated with unstable magic, multiplying the coin that may be found here.");
        await Poll();

        Assert.Equal(TrayIconState.Error, scout.Status.IconState);
    }

    [Fact]
    public async Task NoKeyShowsTheErrorIcon()
    {
        apiKeyStore.ApiKey = null;
        await Poll();

        Assert.Equal(TrayIconState.Error, scout.Status.IconState);
    }

    [Fact]
    public async Task LoggingHint()
    {
        await Poll();
        time.Advance(LogWatcher.LoggingHintDelay);
        await scout.PollAsync();

        Assert.Equal(["LoggingOff"], notifier.Notifications);
    }

    [Fact]
    public async Task ConnectionCheckListsUnreachableSites()
    {
        api.ListFailure = new("network");

        Assert.Equal(Server.All, await scout.CheckConnectionAsync());
        Assert.Equal(TrayIconState.Error, scout.Status.IconState);
    }
}
