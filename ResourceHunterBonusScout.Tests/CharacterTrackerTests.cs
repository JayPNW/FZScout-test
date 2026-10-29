using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Reporting;

namespace ResourceHunterBonusScout.Tests;

public class CharacterTrackerTests
{
    readonly FakeTimeProvider time = new();
    readonly ServerLists lists = new(["experience", "loot", "none"], ["Halls of Honor", "Crushbone"]);
    readonly CharacterTracker tracker;

    public CharacterTrackerTests()
    {
        tracker = new("Bowslinger", Server.Fangbreaker, time);
    }

    BonusReport? Handle(LogEvent logEvent) => tracker.Handle(logEvent, lists);

    static BonusReport Report(string zone, string bonusKey) => new("Bowslinger", Server.Fangbreaker, zone, bonusKey);

    [Fact]
    public void ReportsABonusMessageForTheCurrentZone()
    {
        Handle(new LogEvent.ZoneEntered("The Halls of Honor"));

        Assert.Equal("Halls of Honor", tracker.Zone);
        Assert.Equal(Report("Halls of Honor", "loot"), Handle(new LogEvent.BonusMessage("loot")));
    }

    [Fact]
    public void ReportsNoBonusFiveMinutesAfterZoneEntry()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));

        time.Advance(CharacterTracker.NoBonusDelay - TimeSpan.FromSeconds(1));
        Assert.Null(tracker.Tick());
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(Report("Crushbone", "none"), tracker.Tick());
        Assert.Null(tracker.Tick());
    }

    [Fact]
    public void BonusMessageCancelsTheTimer()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        Handle(new LogEvent.BonusMessage("loot"));

        time.Advance(CharacterTracker.NoBonusDelay);
        Assert.Null(tracker.Tick());
    }

    [Fact]
    public void ZoneEntryRestartsTheTimer()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        time.Advance(TimeSpan.FromMinutes(4));
        Handle(new LogEvent.ZoneEntered("The Halls of Honor"));

        time.Advance(TimeSpan.FromMinutes(4));
        Assert.Null(tracker.Tick());
        time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(Report("Halls of Honor", "none"), tracker.Tick());
    }

    [Fact]
    public void NothingIsReportedForAnUnknownZone()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        Handle(new LogEvent.ZoneEntered("The Bazaar"));

        Assert.Null(tracker.Zone);
        Assert.Null(Handle(new LogEvent.BonusMessage("loot")));
        time.Advance(CharacterTracker.NoBonusDelay);
        Assert.Null(tracker.Tick());
    }

    [Fact]
    public void ZoneIsUnknownWithoutLists()
    {
        tracker.Handle(new LogEvent.ZoneEntered("Crushbone"), null);

        Assert.Null(tracker.Zone);
    }

    [Fact]
    public void ReportsNoBonusTenSecondsAfterAKillWithoutABonusMessage()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        time.Advance(TimeSpan.FromSeconds(30));
        Handle(new LogEvent.Kill());

        time.Advance(CharacterTracker.AfterKillDelay - TimeSpan.FromSeconds(1));
        Assert.Null(tracker.Tick());
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(Report("Crushbone", "none"), tracker.Tick());
    }

    [Fact]
    public void KillDoesNotDelayTheTimer()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        time.Advance(CharacterTracker.NoBonusDelay - TimeSpan.FromSeconds(2));
        Handle(new LogEvent.Kill());

        time.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal(Report("Crushbone", "none"), tracker.Tick());
    }

    [Fact]
    public void BonusMessageAfterAKill()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        Handle(new LogEvent.Kill());

        Assert.Equal(Report("Crushbone", "experience"), Handle(new LogEvent.BonusMessage("experience")));
        time.Advance(CharacterTracker.NoBonusDelay);
        Assert.Null(tracker.Tick());
    }

    [Fact]
    public void KillAfterNoBonusWasReportedDoesNothing()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        time.Advance(CharacterTracker.NoBonusDelay);
        tracker.Tick();

        Handle(new LogEvent.Kill());
        time.Advance(CharacterTracker.AfterKillDelay);
        Assert.Null(tracker.Tick());
    }

    [Fact]
    public void ReportsABonusThatAppearsAfterNoBonus()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        time.Advance(CharacterTracker.NoBonusDelay);
        tracker.Tick();

        Assert.Equal(Report("Crushbone", "loot"), Handle(new LogEvent.BonusMessage("loot")));
    }

    [Fact]
    public void IgnoresABonusTheSiteDoesNotTrack()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));

        Assert.Null(Handle(new LogEvent.BonusMessage("tribute")));
        // The zone has a bonus, so it isn't "none" either.
        time.Advance(CharacterTracker.NoBonusDelay);
        Assert.Null(tracker.Tick());
    }

    [Fact]
    public void CampingPausesTheTimer()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        time.Advance(TimeSpan.FromMinutes(4));
        Handle(new LogEvent.CampStarted());

        time.Advance(TimeSpan.FromMinutes(10));
        Assert.Null(tracker.Tick());

        Handle(new LogEvent.CampAbandoned());
        time.Advance(TimeSpan.FromSeconds(59));
        Assert.Null(tracker.Tick());
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(Report("Crushbone", "none"), tracker.Tick());
    }

    [Fact]
    public void DailyResetRestartsTheTimer()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        Handle(new LogEvent.BonusMessage("loot"));
        time.Advance(TimeSpan.FromHours(1));

        Handle(new LogEvent.DailyReset());

        time.Advance(CharacterTracker.NoBonusDelay);
        Assert.Equal(Report("Crushbone", "none"), tracker.Tick());
    }

    [Fact]
    public void BonusAfterDailyReset()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        Handle(new LogEvent.BonusMessage("loot"));
        Handle(new LogEvent.DailyReset());

        Assert.Equal(Report("Crushbone", "experience"), Handle(new LogEvent.BonusMessage("experience")));
    }
}
