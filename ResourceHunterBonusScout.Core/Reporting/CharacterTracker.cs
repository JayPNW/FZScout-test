using ResourceHunterBonusScout.Logs;

namespace ResourceHunterBonusScout.Reporting;

/// <summary>Follows one character's log and decides what to report for its current zone.</summary>
public class CharacterTracker(string character, Server server, TimeProvider time)
{
    /// <summary>How long after a zone entry, with no bonus message, to report <see cref="BonusReport.NoBonus"/>.</summary>
    public static readonly TimeSpan NoBonusDelay = TimeSpan.FromMinutes(5);

    /// <summary>How long after a kill, with no bonus message, to report <see cref="BonusReport.NoBonus"/>.</summary>
    public static readonly TimeSpan AfterKillDelay = TimeSpan.FromSeconds(10);

    // How much was left on the timer when camping paused it.
    TimeSpan? _pausedRemaining;

    public string Character => character;
    public Server Server => server;

    /// <summary>The site's spelling, or null when the current zone is unknown.</summary>
    public string? Zone { get; private set; }

    /// <summary>When to report no bonus, after a zone entry or a kill. Null when nothing is waiting, or while paused.</summary>
    public DateTimeOffset? NoBonusAt { get; private set; }

    /// <param name="lists">The server's lists. Needed for zone entries and bonus messages; null if they couldn't be loaded.</param>
    public BonusReport? Handle(LogEvent logEvent, ServerLists? lists)
    {
        var now = time.GetUtcNow();
        switch (logEvent)
        {
            case LogEvent.ZoneEntered entered:
                Zone = lists?.FindZone(entered.Zone);
                StartTimer(Zone is null ? null : NoBonusDelay);
                return null;

            case LogEvent.BonusMessage bonus when Zone is not null:
                // Even a bonus the site doesn't track means the zone has one, so don't report none.
                StartTimer(null);
                return lists is not null && lists.BonusKeys.Contains(bonus.BonusKey) ? Report(bonus.BonusKey) : null;

            case LogEvent.Kill:
                if (NoBonusAt is { } at)
                {
                    NoBonusAt = Min(at, now + AfterKillDelay);
                }
                else if (this._pausedRemaining is { } remaining)
                {
                    this._pausedRemaining = Min(remaining, AfterKillDelay);
                }

                return null;

            case LogEvent.CampStarted when NoBonusAt is { } pauseAt:
                this._pausedRemaining = pauseAt - now;
                NoBonusAt = null;
                return null;

            case LogEvent.CampAbandoned when this._pausedRemaining is { } remaining:
                NoBonusAt = now + remaining;
                this._pausedRemaining = null;
                return null;

            case LogEvent.DailyReset when Zone is not null:
                StartTimer(NoBonusDelay);
                return null;

            // Restart only a timer that's still waiting. If the zone's bonus was already seen, it hasn't changed.
            case LogEvent.PersonaChanged when NoBonusAt is not null || this._pausedRemaining is not null:
                StartTimer(NoBonusDelay);
                return null;

            default:
                return null;
        }
    }

    /// <summary>Call regularly. Reports no bonus when the timer runs out.</summary>
    public BonusReport? Tick()
    {
        if (NoBonusAt is not { } at || time.GetUtcNow() < at || Zone is null)
        {
            return null;
        }

        NoBonusAt = null;
        return Report(BonusReport.NoBonus);
    }

    void StartTimer(TimeSpan? delay)
    {
        NoBonusAt = time.GetUtcNow() + delay;
        this._pausedRemaining = null;
    }

    BonusReport Report(string bonusKey) => new(character, server, Zone!, bonusKey);

    static T Min<T>(T a, T b) where T : IComparable<T> => a.CompareTo(b) <= 0 ? a : b;
}
