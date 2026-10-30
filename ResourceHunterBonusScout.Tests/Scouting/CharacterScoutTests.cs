using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Scouting;
using ResourceHunterBonusScout.Site;

namespace ResourceHunterBonusScout.Tests.Scouting;

public class CharacterScoutTests
{
    const string Befallen = "You have entered Befallen.";
    const string Crushbone = "You have entered Clan Crushbone.";
    const string Kill = "You have slain a skeleton!";
    const string Experience = "This land is saturated with unstable magic, improving the experiences of all who venture here.";
    const string Tribute = "This land is saturated with unstable magic, enhancing the potency of any offerings made here.";
    const string DailyReset = "Fonts of unstable magic are erupting and receding in response to some nameless power. The magic is cascading throughout Norrath and into the Planes beyond. Hunt for the lands that have been most recently saturated with this magic to reap their many and varied resources.";
    const string Camping = "It will take you about 30 seconds to prepare your camp.";
    const string CampAbandoned = "You abandon your preparations to camp.";

    static readonly Character Bob = new("Bob", Server.Frostreaver);
    static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    // Like Frostreaver's: no tribute.
    static readonly ServerLists Lists = new(
        new HashSet<BonusKey> { new("experience"), new("aa"), new("loot"), BonusKey.None },
        new HashSet<string> { "Befallen", "Crushbone" });

    readonly CharacterScout _scout = new(Bob);

    [Fact]
    public void BonusMessageIsReported()
    {
        this.Log(0, Befallen);

        Assert.Equal([Report("Befallen", "experience")], this.Log(10, Experience));
    }

    [Fact]
    public void ZoneIsReportedByItsSiteName()
    {
        this.Log(0, Crushbone);

        Assert.Equal([Report("Crushbone", "experience")], this.Log(10, Experience));
    }

    [Fact]
    public void NoBonusFiveMinutesAfterEnteringIsReportedAsNone()
    {
        this.Log(0, Befallen);

        Assert.Empty(this.Tick(299));
        Assert.Equal([ReportNone("Befallen")], this.Tick(300));
    }

    [Fact]
    public void NoneIsReportedOnlyOnce()
    {
        this.Log(0, Befallen);
        this.Tick(300);

        Assert.Empty(this.Tick(600));
    }

    [Fact]
    public void NoBonusFiveSecondsAfterAKillIsReportedAsNone()
    {
        this.Log(0, Befallen);
        this.Log(20, Kill);

        Assert.Empty(this.Tick(24));
        Assert.Equal([ReportNone("Befallen")], this.Tick(25));
        Assert.Empty(this.Tick(300));
    }

    [Fact]
    public void LaterKillsDoNotPushTheKillTimerBack()
    {
        this.Log(0, Befallen);
        this.Log(20, Kill);
        this.Log(23, Kill);

        Assert.Equal([ReportNone("Befallen")], this.Tick(25));
    }

    [Fact]
    public void KillsAfterABonusDoNothing()
    {
        this.Log(0, Befallen);
        this.Log(20, Kill);
        this.Log(20, Experience);
        this.Log(40, Kill);

        Assert.Empty(this.Tick(300));
    }

    [Fact]
    public void BonusAfterReportingNoneIsStillReported()
    {
        // A kill doesn't always trigger the message; sometimes it comes minutes later.
        this.Log(0, Befallen);
        this.Log(20, Kill);
        this.Tick(25);

        Assert.Equal([Report("Befallen", "experience")], this.Log(200, Experience));
    }

    [Fact]
    public void EnteringAnotherZoneRestartsTheIdleTimer()
    {
        this.Log(0, Befallen);
        this.Log(100, Crushbone);

        Assert.Empty(this.Tick(300));
        Assert.Equal([ReportNone("Crushbone")], this.Tick(400));
    }

    [Fact]
    public void EnteringAnotherZoneCancelsTheKillTimer()
    {
        this.Log(0, Befallen);
        this.Log(20, Kill);
        this.Log(22, Crushbone);

        Assert.Empty(this.Tick(30));
    }

    [Fact]
    public void ReEnteringTheSameZoneStartsANewVisit()
    {
        this.Log(0, Befallen);
        this.Log(10, Experience);
        this.Log(100, Befallen);

        Assert.Equal([ReportNone("Befallen")], this.Tick(400));
    }

    [Fact]
    public void DailyResetRestartsTheIdleTimer()
    {
        this.Log(0, Befallen);
        this.Log(10, Experience);
        this.Log(1000, DailyReset);

        Assert.Empty(this.Tick(1299));
        Assert.Equal([ReportNone("Befallen")], this.Tick(1300));
    }

    [Fact]
    public void DailyResetRearmsKills()
    {
        this.Log(0, Befallen);
        this.Log(10, Experience);
        this.Log(1000, DailyReset);
        this.Log(1010, Kill);

        Assert.Equal([ReportNone("Befallen")], this.Tick(1015));
    }

    [Fact]
    public void CampingPausesTheIdleTimer()
    {
        this.Log(0, Befallen);
        this.Log(200, Camping);
        this.Log(205, "It will take about 25 more seconds to prepare your camp.");

        Assert.Empty(this.Tick(10_000));
    }

    [Fact]
    public void AbandoningCampResumesTheIdleTimerWhereItLeftOff()
    {
        this.Log(0, Befallen);
        this.Log(200, Camping);
        this.Log(205, Camping);
        this.Log(210, CampAbandoned);

        Assert.Empty(this.Tick(309));
        Assert.Equal([ReportNone("Befallen")], this.Tick(310));
    }

    [Fact]
    public void DailyResetWhileCampingWaitsForTheCampToBeAbandoned()
    {
        this.Log(0, Befallen);
        this.Log(10, Experience);
        this.Log(100, Camping);
        this.Log(101, DailyReset);

        Assert.Empty(this.Tick(1000));
        this.Log(1000, CampAbandoned);
        Assert.Equal([ReportNone("Befallen")], this.Tick(1300));
    }

    [Fact]
    public void BonusBeforeAnyZoneIsIgnored()
    {
        Assert.Empty(this.Log(0, Experience));
        Assert.Empty(this.Tick(1000));
    }

    [Fact]
    public void BonusTypeNotOnTheServersListIsIgnored()
    {
        this.Log(0, Befallen);

        Assert.Empty(this.Log(10, Tribute));
    }

    [Fact]
    public void UnknownZoneIsAnError()
    {
        var actions = this.Log(0, "You have entered The Halls of Honor.");

        Assert.Equal(
            [new LogErrorAction($"Bob (Frostreaver) entered 'The Halls of Honor', which isn't on the Frostreaver zones list. Please report it at {CharacterScout.ReportIssuesUrl}")],
            actions);
    }

    [Fact]
    public void UnknownZoneIsNeverReported()
    {
        this.Log(0, "You have entered The Halls of Honor.");

        Assert.Empty(this.Log(10, Experience));
        Assert.Empty(this.Log(20, Kill));
        Assert.Empty(this.Tick(1000));
    }

    [Fact]
    public void WithoutListsZonesCantBeChecked()
    {
        var actions = this._scout.Handle(LogEvent.Parse(Befallen)!, Start, null);

        Assert.Equal(
            [new LogErrorAction("Bob (Frostreaver) entered 'Befallen', but the Frostreaver zones list isn't loaded, so it won't be reported.")],
            actions);
        Assert.Empty(this.Tick(1000));
    }

    [Fact]
    public void ZoneThatNeverHasABonusWaitsForNothing()
    {
        Assert.Empty(this.Log(0, "You have entered The Plane of Knowledge."));
        Assert.Empty(this.Log(10, Kill));
        Assert.Empty(this.Tick(1000));
    }

    [Fact]
    public void BonusInAZoneThatNeverHasOneIsAnError()
    {
        this.Log(0, "You have entered The Plane of Knowledge.");

        Assert.Equal(
            [new LogErrorAction($"Bob (Frostreaver) saw a 'experience' bonus in 'The Plane of Knowledge', which never has a bonus. Please report it at {CharacterScout.ReportIssuesUrl}")],
            this.Log(10, Experience));
    }

    [Fact]
    public void AreasInsideAZoneDontChangeIt()
    {
        this.Log(0, Befallen);
        this.Log(100, "You have entered an area where levitation effects do not function.");

        Assert.Equal([ReportNone("Befallen")], this.Tick(300));
    }

    static ReportBonusAction Report(string zone, string bonus) => new(Bob, new BonusReport(Server.Frostreaver, zone, new BonusKey(bonus)));

    static ReportNoBonusAction ReportNone(string zone) => new(Bob, zone);

    IReadOnlyList<ScoutAction> Log(int seconds, string message)
    {
        var logEvent = LogEvent.Parse($"[Thu Oct 29 12:00:00 2026] {message}");
        if (logEvent is null)
        {
            return [];
        }

        return this._scout.Handle(logEvent, Start.AddSeconds(seconds), Lists);
    }

    IReadOnlyList<ScoutAction> Tick(int seconds) => this._scout.Tick(Start.AddSeconds(seconds));
}
