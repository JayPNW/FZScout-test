using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Site;

namespace ResourceHunterBonusScout.Scouting;

/// <summary>
/// The business rules for one character: turns their log events and the passing of time into reports.
/// See the Timing and Reporting sections of InternalDocumentation/SPEC.md.
/// </summary>
sealed class CharacterScout(Character character)
{
    /// <summary>EverQuest announces a bonus 3 to 3.5 minutes after entry when there's no kill, so after 5 there is none.</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(5);

    /// <summary>A kill that triggers the bonus message gets it in the same second.</summary>
    public static readonly TimeSpan KillTimeout = TimeSpan.FromSeconds(5);

    public const string ReportIssuesUrl = "https://github.com/JayBazuzi/ResourceHunterBonusScout/issues";

    /// <summary>Already reported this zone visit (or since the daily reset), so stop waiting.</summary>
    bool _reported;

    DateTimeOffset? _idleDeadline;

    /// <summary>Set while camping: what was left on the idle timer when it was paused.</summary>
    TimeSpan? _idleRemaining;

    bool _camping;

    DateTimeOffset? _killDeadline;

    public Character Character => character;

    public Zone? Zone { get; private set; }

    /// <param name="lists">The server's lists, or null if they couldn't be loaded.</param>
    public IReadOnlyList<ScoutAction> Handle(LogEvent logEvent, DateTimeOffset now, ServerLists? lists) => logEvent switch
    {
        ZoneEnteredLogEvent e => this.OnZoneEntered(e.Zone, now, lists),
        KillLogEvent => this.OnKill(now),
        BonusReportLogEvent e => this.OnBonus(e.BonusKey, lists),
        DailyResetLogEvent => this.OnDailyReset(now),
        CampingLogEvent => this.OnCamping(now),
        CampAbandonedLogEvent => this.OnCampAbandoned(now),
        _ => [],
    };

    /// <summary>Call regularly. Reports <c>none</c> when a timer runs out without a bonus message.</summary>
    public IReadOnlyList<ScoutAction> Tick(DateTimeOffset now)
    {
        if (this.Zone is not KnownZone zone)
        {
            return [];
        }

        if (!IsDue(this._killDeadline, now) && !IsDue(this._idleDeadline, now))
        {
            return [];
        }

        this.MarkReported();
        return [new ReportNoBonusAction(character, zone.SiteName)];
    }

    static bool IsDue(DateTimeOffset? deadline, DateTimeOffset now) => deadline is not null && deadline <= now;

    IReadOnlyList<ScoutAction> OnZoneEntered(string logZone, DateTimeOffset now, ServerLists? lists)
    {
        this.Zone = ZoneNames.Resolve(logZone, lists?.Zones);
        this._reported = false;
        this._killDeadline = null;
        this._idleRemaining = null;
        this._idleDeadline = null;
        this._camping = false;

        if (this.Zone is KnownZone)
        {
            this.StartIdleTimer(now);
            return [];
        }

        if (this.Zone is not UnknownZone)
        {
            return [];
        }

        if (lists is null)
        {
            return [new LogErrorAction($"{character} entered '{logZone}', but the {character.Server} zones list isn't loaded, so it won't be reported.")];
        }

        return [new LogErrorAction($"{character} entered '{logZone}', which isn't on the {character.Server} zones list. Please report it at {ReportIssuesUrl}")];
    }

    IReadOnlyList<ScoutAction> OnKill(DateTimeOffset now)
    {
        if (this.Zone is KnownZone && !this._reported && this._killDeadline is null)
        {
            this._killDeadline = now + KillTimeout;
        }

        return [];
    }

    IReadOnlyList<ScoutAction> OnBonus(BonusKey bonus, ServerLists? lists)
    {
        if (this.Zone is NoBonusZone noBonusZone)
        {
            return [new LogErrorAction($"{character} saw a '{bonus}' bonus in '{noBonusZone.Name}', which never has a bonus. Please report it at {ReportIssuesUrl}")];
        }

        if (this.Zone is not KnownZone zone)
        {
            return [];
        }

        if (lists is not null && !lists.Types.Contains(bonus))
        {
            return [];
        }

        this.MarkReported();
        return [new ReportBonusAction(character, new BonusReport(character.Server, zone.SiteName, bonus))];
    }

    IReadOnlyList<ScoutAction> OnDailyReset(DateTimeOffset now)
    {
        if (this.Zone is not KnownZone)
        {
            return [];
        }

        this._reported = false;
        this._killDeadline = null;
        this.StartIdleTimer(now);
        return [];
    }

    IReadOnlyList<ScoutAction> OnCamping(DateTimeOffset now)
    {
        this._camping = true;
        if (this._idleDeadline is { } deadline)
        {
            this._idleRemaining = deadline - now;
            this._idleDeadline = null;
        }

        return [];
    }

    IReadOnlyList<ScoutAction> OnCampAbandoned(DateTimeOffset now)
    {
        this._camping = false;
        if (this._idleRemaining is { } remaining)
        {
            this._idleDeadline = now + remaining;
            this._idleRemaining = null;
        }

        return [];
    }

    void StartIdleTimer(DateTimeOffset now)
    {
        if (this._camping)
        {
            this._idleRemaining = IdleTimeout;
        }
        else
        {
            this._idleDeadline = now + IdleTimeout;
        }
    }

    void MarkReported()
    {
        this._reported = true;
        this._killDeadline = null;
        this._idleDeadline = null;
        this._idleRemaining = null;
    }
}
