using ResourceHunterBonusScout.Logs;

namespace ResourceHunterBonusScout.Reporting;

/// <summary>Follows one character's log and decides what to report for its current zone.</summary>
public class CharacterTracker(string character, Server server, TimeProvider time)
{
    /// <summary>How long after a zone entry, with no bonus message, to report <see cref="BonusReport.NoBonus"/>.</summary>
    public static readonly TimeSpan NoBonusDelay = TimeSpan.FromMinutes(5);

    /// <summary>How long after a kill, with no bonus message, to report <see cref="BonusReport.NoBonus"/>.</summary>
    public static readonly TimeSpan AfterKillDelay = TimeSpan.FromSeconds(10);

    // When to report no bonus. Null when nothing is waiting, or while paused.
    DateTimeOffset? _noBonusAt;
    // How much was left on the timer when camping paused it.
    TimeSpan? _pausedRemaining;

    public string Character => character;
    public Server Server => server;

    /// <summary>The site's spelling, or null when the current zone is unknown.</summary>
    public string? Zone { get; private set; }

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
                if (this._noBonusAt is { } at)
                {
                    this._noBonusAt = Min(at, now + AfterKillDelay);
                }
                else if (this._pausedRemaining is { } remaining)
                {
                    this._pausedRemaining = Min(remaining, AfterKillDelay);
                }

                return null;

            case LogEvent.CampStarted when this._noBonusAt is { } pauseAt:
                this._pausedRemaining = pauseAt - now;
                this._noBonusAt = null;
                return null;

            case LogEvent.CampAbandoned when this._pausedRemaining is { } remaining:
                this._noBonusAt = now + remaining;
                this._pausedRemaining = null;
                return null;

            case LogEvent.DailyReset when Zone is not null:
                StartTimer(NoBonusDelay);
                return null;

            default:
                return null;
        }
    }

    /// <summary>Call regularly. Reports no bonus when the timer runs out.</summary>
    public BonusReport? Tick()
    {
        if (this._noBonusAt is not { } at || time.GetUtcNow() < at || Zone is null)
        {
            return null;
        }

        this._noBonusAt = null;
        return Report(BonusReport.NoBonus);
    }

    void StartTimer(TimeSpan? delay)
    {
        this._noBonusAt = time.GetUtcNow() + delay;
        this._pausedRemaining = null;
    }

    BonusReport Report(string bonusKey) => new(character, server, Zone!, bonusKey);

    static T Min<T>(T a, T b) where T : IComparable<T> => a.CompareTo(b) <= 0 ? a : b;
}
