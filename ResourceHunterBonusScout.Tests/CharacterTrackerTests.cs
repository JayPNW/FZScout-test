using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Reporting;

namespace ResourceHunterBonusScout.Tests;

public class CharacterTrackerTests
{
    readonly FakeTimeProvider _time = new();
    readonly ServerLists _lists = new(["experience", "loot", "none"], ["Halls of Honor", "Crushbone"]);
    readonly CharacterTracker _tracker;

    public CharacterTrackerTests()
    {
        this._tracker = new("Bowslinger", Server.Fangbreaker, this._time);
    }

    BonusReport? Handle(LogEvent logEvent) => this._tracker.Handle(logEvent, this._lists);

    static BonusReport Report(string zone, string bonusKey) => new("Bowslinger", Server.Fangbreaker, zone, bonusKey);

    [Fact]
    public void ReportsABonusMessageForTheCurrentZone()
    {
        Handle(new LogEvent.ZoneEntered("The Halls of Honor"));

        Assert.Equal("Halls of Honor", this._tracker.Zone);
        Assert.Equal(Report("Halls of Honor", "loot"), Handle(new LogEvent.BonusMessage("loot")));
    }

    [Fact]
    public void ReportsNoBonusFiveMinutesAfterZoneEntry()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));

        this._time.Advance(CharacterTracker.NoBonusDelay - TimeSpan.FromSeconds(1));
        Assert.Null(this._tracker.Tick());
        this._time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(Report("Crushbone", "none"), this._tracker.Tick());
        Assert.Null(this._tracker.Tick());
    }

    [Fact]
    public void ZoneThatNeverHasABonusStopsTheTimer()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        Handle(new LogEvent.ZoneEntered("The Guild Lobby"));

        Assert.Null(this._tracker.Zone);
        Assert.Equal("The Guild Lobby", this._tracker.NoBonusZone);
        this._time.Advance(CharacterTracker.NoBonusDelay);
        Assert.Null(this._tracker.Tick());
    }

    [Fact]
    public void BonusMessageCancelsTheTimer()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        Handle(new LogEvent.BonusMessage("loot"));

        this._time.Advance(CharacterTracker.NoBonusDelay);
        Assert.Null(this._tracker.Tick());
    }

    [Fact]
    public void ZoneEntryRestartsTheTimer()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        this._time.Advance(TimeSpan.FromMinutes(4));
        Handle(new LogEvent.ZoneEntered("The Halls of Honor"));

        this._time.Advance(TimeSpan.FromMinutes(4));
        Assert.Null(this._tracker.Tick());
        this._time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(Report("Halls of Honor", "none"), this._tracker.Tick());
    }

    [Fact]
    public void NothingIsReportedForAnUnknownZone()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        Handle(new LogEvent.ZoneEntered("The Bazaar"));

        Assert.Null(this._tracker.Zone);
        Assert.Null(Handle(new LogEvent.BonusMessage("loot")));
        this._time.Advance(CharacterTracker.NoBonusDelay);
        Assert.Null(this._tracker.Tick());
    }

    [Fact]
    public void ZoneIsUnknownWithoutLists()
    {
        this._tracker.Handle(new LogEvent.ZoneEntered("Crushbone"), null);

        Assert.Null(this._tracker.Zone);
    }

    [Fact]
    public void ReportsNoBonusTenSecondsAfterAKillWithoutABonusMessage()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        this._time.Advance(TimeSpan.FromSeconds(30));
        Handle(new LogEvent.Kill());

        this._time.Advance(CharacterTracker.AfterKillDelay - TimeSpan.FromSeconds(1));
        Assert.Null(this._tracker.Tick());
        this._time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(Report("Crushbone", "none"), this._tracker.Tick());
    }

    [Fact]
    public void KillDoesNotDelayTheTimer()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        this._time.Advance(CharacterTracker.NoBonusDelay - TimeSpan.FromSeconds(2));
        Handle(new LogEvent.Kill());

        this._time.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal(Report("Crushbone", "none"), this._tracker.Tick());
    }

    [Fact]
    public void BonusMessageAfterAKill()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        Handle(new LogEvent.Kill());

        Assert.Equal(Report("Crushbone", "experience"), Handle(new LogEvent.BonusMessage("experience")));
        this._time.Advance(CharacterTracker.NoBonusDelay);
        Assert.Null(this._tracker.Tick());
    }

    [Fact]
    public void KillAfterNoBonusWasReportedDoesNothing()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        this._time.Advance(CharacterTracker.NoBonusDelay);
        this._tracker.Tick();

        Handle(new LogEvent.Kill());
        this._time.Advance(CharacterTracker.AfterKillDelay);
        Assert.Null(this._tracker.Tick());
    }

    [Fact]
    public void ReportsABonusThatAppearsAfterNoBonus()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        this._time.Advance(CharacterTracker.NoBonusDelay);
        this._tracker.Tick();

        Assert.Equal(Report("Crushbone", "loot"), Handle(new LogEvent.BonusMessage("loot")));
    }

    [Fact]
    public void IgnoresABonusTheSiteDoesNotTrack()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));

        Assert.Null(Handle(new LogEvent.BonusMessage("tribute")));
        // The zone has a bonus, so it isn't "none" either.
        this._time.Advance(CharacterTracker.NoBonusDelay);
        Assert.Null(this._tracker.Tick());
    }

    [Fact]
    public void CampingPausesTheTimer()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        this._time.Advance(TimeSpan.FromMinutes(4));
        Handle(new LogEvent.CampStarted());

        this._time.Advance(TimeSpan.FromMinutes(10));
        Assert.Null(this._tracker.Tick());

        Handle(new LogEvent.CampAbandoned());
        this._time.Advance(TimeSpan.FromSeconds(59));
        Assert.Null(this._tracker.Tick());
        this._time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(Report("Crushbone", "none"), this._tracker.Tick());
    }

    [Fact]
    public void DailyResetRestartsTheTimer()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        Handle(new LogEvent.BonusMessage("loot"));
        this._time.Advance(TimeSpan.FromHours(1));

        Handle(new LogEvent.DailyReset());

        this._time.Advance(CharacterTracker.NoBonusDelay);
        Assert.Equal(Report("Crushbone", "none"), this._tracker.Tick());
    }

    [Fact]
    public void PersonaChangeRestartsTheTimer()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        this._time.Advance(TimeSpan.FromMinutes(4));
        Handle(new LogEvent.PersonaChanged());

        this._time.Advance(TimeSpan.FromMinutes(4));
        Assert.Null(this._tracker.Tick());
        this._time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(Report("Crushbone", "none"), this._tracker.Tick());
    }

    [Fact]
    public void PersonaChangeAfterABonusDoesNothing()
    {
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        Handle(new LogEvent.BonusMessage("loot"));

        Handle(new LogEvent.PersonaChanged());

        this._time.Advance(CharacterTracker.NoBonusDelay);
        Assert.Null(this._tracker.Tick());
    }

    [Fact]
    public void WithTheIdleTimerOffOnlyAKillReportsNoBonus()
    {
        this._tracker.IsIdleTimerOn = false;
        Handle(new LogEvent.ZoneEntered("Crushbone"));

        this._time.Advance(TimeSpan.FromHours(1));
        Assert.Null(this._tracker.Tick());

        Handle(new LogEvent.Kill());
        this._time.Advance(CharacterTracker.AfterKillDelay);
        Assert.Equal(Report("Crushbone", "none"), this._tracker.Tick());
    }

    [Fact]
    public void WithTheIdleTimerOffCampingWaitsForAKill()
    {
        this._tracker.IsIdleTimerOn = false;
        Handle(new LogEvent.ZoneEntered("Crushbone"));
        Handle(new LogEvent.CampStarted());
        this._time.Advance(TimeSpan.FromMinutes(1));
        Handle(new LogEvent.CampAbandoned());

        this._time.Advance(TimeSpan.FromHours(1));
        Assert.Null(this._tracker.Tick());

        Handle(new LogEvent.Kill());
        this._time.Advance(CharacterTracker.AfterKillDelay);
        Assert.Equal(Report("Crushbone", "none"), this._tracker.Tick());
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
