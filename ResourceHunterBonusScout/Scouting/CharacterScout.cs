using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Site;

namespace ResourceHunterBonusScout.Scouting;

/// <summary>
/// The business rules for one character: turns their log events and the passing of time into reports.
/// See the Timing and Reporting sections of InternalDocumentation/SPEC.md.
/// </summary>
class CharacterScout(Character character)
{
    /// <summary>EverQuest announces a bonus 3 to 3.5 minutes after entry when there's no kill, so after 5 there is none.</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(5);

    /// <summary>A kill that triggers the bonus message gets it in the same second.</summary>
    public static readonly TimeSpan KillTimeout = TimeSpan.FromSeconds(5);

    public const string ReportIssuesUrl = "https://github.com/JayBazuzi/ResourceHunterBonusScout/issues/new";

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
    public IReadOnlyList<ScoutAction> Handle(LogEvent logEvent, DateTimeOffset now, ServerLists? lists)
    {
        return [.. ResolveZone(lists), .. Dispatch(logEvent, now, lists)];
    }

    /// <summary>Call regularly. Reports <c>none</c> when a timer runs out without a bonus message.</summary>
    /// <param name="lists">The server's lists if they're loaded. Ticking never loads them.</param>
    public IReadOnlyList<ScoutAction> Tick(DateTimeOffset now, ServerLists? lists)
    {
        var resolved = ResolveZone(lists);
        if (!IsDue(this._killDeadline, now) && !IsDue(this._idleDeadline, now))
        {
            return resolved;
        }

        if (Zone is UnresolvedZone unresolved)
        {
            MarkReported();
            return [new LogErrorAction($"{character} was in '{unresolved.LogName}', but the {character.Server} zones list still isn't loaded, so 'none' wasn't reported.")];
        }

        if (Zone is not KnownZone zone)
        {
            return resolved;
        }

        MarkReported();
        return [new ReportNoBonusAction(character, zone.SiteName)];
    }

    static bool IsDue(DateTimeOffset? deadline, DateTimeOffset now) => deadline is not null && deadline <= now;

    static bool IsReportable(Zone? zone) => zone is KnownZone or UnresolvedZone;

    IReadOnlyList<ScoutAction> Dispatch(LogEvent logEvent, DateTimeOffset now, ServerLists? lists) => logEvent switch
    {
        ZoneEnteredLogEvent e => OnZoneEntered(e.Zone, now, lists),
        KillLogEvent => OnKill(now),
        BonusReportLogEvent e => OnBonus(e.BonusKey, lists),
        DailyResetLogEvent => OnDailyReset(now),
        CampingLogEvent => OnCamping(now),
        CampAbandonedLogEvent => OnCampAbandoned(now),
        _ => [],
    };

    /// <summary>Once the zones list loads, works out which zone an <see cref="UnresolvedZone"/> is. Its timers keep running.</summary>
    IReadOnlyList<ScoutAction> ResolveZone(ServerLists? lists)
    {
        if (Zone is not UnresolvedZone unresolved || lists is null)
        {
            return [];
        }

        Zone = ZoneNames.Resolve(unresolved.LogName, lists.Zones);
        if (Zone is KnownZone)
        {
            return [];
        }

        MarkReported();
        return [UnknownZoneError(unresolved.LogName)];
    }

    LogErrorAction UnknownZoneError(string logZone)
    {
        return new($"{character} entered '{logZone}', which isn't on the {character.Server} zones list. Please report it at {ReportIssuesUrl}");
    }

    IReadOnlyList<ScoutAction> OnZoneEntered(string logZone, DateTimeOffset now, ServerLists? lists)
    {
        Zone = ZoneNames.Resolve(logZone, lists?.Zones);
        this._reported = false;
        this._killDeadline = null;
        this._idleRemaining = null;
        this._idleDeadline = null;
        this._camping = false;

        if (Zone is UnknownZone)
        {
            return [UnknownZoneError(logZone)];
        }

        if (IsReportable(Zone))
        {
            StartIdleTimer(now);
        }

        if (this._idleDeadline is { } deadline)
        {
            return [new LogInfoAction($"{character} entered {logZone}, waiting until {deadline.ToLocalTime():HH:mm:ss} for a bonus message.")];
        }

        return [new LogInfoAction($"{character} entered {logZone}.")];
    }

    IReadOnlyList<ScoutAction> OnKill(DateTimeOffset now)
    {
        if (IsReportable(Zone) && !this._reported && this._killDeadline is null)
        {
            this._killDeadline = now + KillTimeout;
            return [WaitingUntil(character, this._killDeadline.Value)];
        }

        return [];
    }

    IReadOnlyList<ScoutAction> OnBonus(BonusKey bonus, ServerLists? lists)
    {
        if (Zone is NeverBonusZone noBonusZone)
        {
            return [new LogErrorAction($"{character} saw a '{bonus}' bonus in '{noBonusZone.Name}', which never has a bonus. Please report it at {ReportIssuesUrl}")];
        }

        if (Zone is UnresolvedZone unresolved)
        {
            MarkReported();
            return [new LogErrorAction($"{character} saw a '{bonus}' bonus in '{unresolved.LogName}', but the {character.Server} zones list isn't loaded, so it wasn't reported.")];
        }

        if (Zone is not KnownZone zone)
        {
            return [];
        }

        if (lists is not null && !lists.Types.Contains(bonus))
        {
            return [];
        }

        MarkReported();
        return [new ReportBonusAction(character, new BonusReport(character.Server, zone.SiteName, bonus))];
    }

    IReadOnlyList<ScoutAction> OnDailyReset(DateTimeOffset now)
    {
        // Yesterday's reports still waiting to retry would now be wrong.
        var drop = new DropPendingReportsAction(character);
        if (!IsReportable(Zone))
        {
            return [drop];
        }

        this._reported = false;
        this._killDeadline = null;
        StartIdleTimer(now);
        return [drop, .. IdleTimerStarted()];
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
            return IdleTimerStarted();
        }

        return [];
    }

    static LogInfoAction WaitingUntil(Character character, DateTimeOffset deadline)
    {
        return new($"{character} waiting until {deadline.ToLocalTime():HH:mm:ss} for a bonus message.");
    }

    IReadOnlyList<ScoutAction> IdleTimerStarted()
    {
        return this._idleDeadline is { } deadline ? [WaitingUntil(character, deadline)] : [];
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
