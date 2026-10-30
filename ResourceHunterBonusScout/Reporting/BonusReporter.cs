using ResourceHunterBonusScout.Scouting;
using ResourceHunterBonusScout.Site;

namespace ResourceHunterBonusScout.Reporting;

/// <summary>Carries out what the scouts decide, following the Reporting section of InternalDocumentation/SPEC.md.</summary>
sealed class BonusReporter(FangbreakerZoneClient client, ServerListCache lists, Log log)
{
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    readonly HashSet<Server> _banned = [];
    readonly Dictionary<(Character Character, string Zone), PendingRetry> _retries = [];
    ApiKey? _apiKey;
    bool _keyRejected;

    /// <summary>Changing the key resumes reporting after the old one was rejected.</summary>
    public void SetApiKey(ApiKey? apiKey)
    {
        this._apiKey = apiKey;
        this._keyRejected = false;
    }

    public Task ExecuteAsync(ScoutAction action, DateTimeOffset now, CancellationToken cancellationToken = default) => action switch
    {
        LogErrorAction a => this.LogErrorAsync(a.Message),
        DropPendingReportsAction a => this.DropRetriesAsync(a.Character),
        ReportBonusAction or ReportNoBonusAction => this.AttemptAsync(action, now, isRetry: false, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    /// <summary>Call regularly. Sends retries that are due.</summary>
    public async Task TickAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var due = this._retries.Where(r => r.Value.At <= now).ToList();
        foreach (var (key, retry) in due)
        {
            this._retries.Remove(key);
            await this.AttemptAsync(retry.Action, now, isRetry: true, cancellationToken);
        }
    }

    static (Character Character, BonusReport Report) Describe(ScoutAction action) => action switch
    {
        ReportBonusAction a => (a.Character, a.Report),
        ReportNoBonusAction a => (a.Character, new BonusReport(a.Character.Server, a.Zone, BonusKey.None)),
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    Task DropRetriesAsync(Character character)
    {
        foreach (var key in this._retries.Keys.Where(k => k.Character == character).ToList())
        {
            this._retries.Remove(key);
        }

        return Task.CompletedTask;
    }

    Task LogErrorAsync(string message)
    {
        log.Error(message);
        return Task.CompletedTask;
    }

    async Task AttemptAsync(ScoutAction action, DateTimeOffset now, bool isRetry, CancellationToken cancellationToken)
    {
        var (character, report) = Describe(action);

        // A newer report for the same character and zone replaces one waiting to retry.
        this._retries.Remove((character, report.Zone));

        if (!this.CanReport(report))
        {
            return;
        }

        if (action is ReportNoBonusAction && !await this.SiteHasNoBonusAsync(report, cancellationToken))
        {
            return;
        }

        var result = await client.ReportBonusAsync(report, this._apiKey!.Value, cancellationToken);
        if (result.IsSuccess)
        {
            log.Info($"Reported {report} for {character.Name}.");
            return;
        }

        log.Error($"Couldn't report {report}. {result.Error}");
        await this.HandleFailureAsync(result.Outcome, action, character, report, now, isRetry, cancellationToken);
    }

    bool CanReport(BonusReport report)
    {
        if (this._apiKey is null)
        {
            log.Warning($"No API key, so {report} wasn't reported.");
            return false;
        }

        if (this._keyRejected)
        {
            log.Warning($"The API key was rejected, so {report} wasn't reported.");
            return false;
        }

        if (this._banned.Contains(report.Server))
        {
            log.Warning($"{report.Server.Site} has banned this account, so {report} wasn't reported.");
            return false;
        }

        return true;
    }

    /// <summary>True unless the site already shows a bonus for the zone, which we assume is right.</summary>
    async Task<bool> SiteHasNoBonusAsync(BonusReport report, CancellationToken cancellationToken)
    {
        var today = await client.GetTodayAsync(report.Server, cancellationToken);
        if (!today.IsSuccess)
        {
            log.Error($"Couldn't check today's bonuses, so {report} wasn't reported. {today.Error}");
            return false;
        }

        var shown = today.Value!.GetValueOrDefault(report.Zone, BonusKey.Unconfirmed);
        if (shown != BonusKey.None && shown != BonusKey.Unconfirmed)
        {
            log.Info($"{report.Server.Site} already shows '{shown}' for '{report.Zone}', so 'none' wasn't reported.");
            return false;
        }

        return true;
    }

    Task HandleFailureAsync(ApiOutcome outcome, ScoutAction action, Character character, BonusReport report, DateTimeOffset now, bool isRetry, CancellationToken cancellationToken) => outcome switch
    {
        ApiOutcome.KeyRejected => this.StopUntilKeyChanges(),
        ApiOutcome.Banned => this.StopReportingTo(report.Server),
        ApiOutcome.NotFound => lists.ReloadAsync(report.Server, now, cancellationToken),
        ApiOutcome.Unavailable => this.RetryOnce(action, character, report, now, isRetry),
        _ => Task.CompletedTask,
    };

    Task StopUntilKeyChanges()
    {
        this._keyRejected = true;
        log.Error("API key rejected. Reporting is stopped until the key changes.");
        return Task.CompletedTask;
    }

    Task StopReportingTo(Server server)
    {
        this._banned.Add(server);
        log.Error($"{server.Site} has banned or timed out this account. Reporting to it is stopped until restart.");
        return Task.CompletedTask;
    }

    Task RetryOnce(ScoutAction action, Character character, BonusReport report, DateTimeOffset now, bool isRetry)
    {
        if (isRetry)
        {
            log.Error($"Gave up reporting {report}.");
        }
        else
        {
            this._retries[(character, report.Zone)] = new PendingRetry(action, now + RetryDelay);
        }

        return Task.CompletedTask;
    }

    sealed record PendingRetry(ScoutAction Action, DateTimeOffset At);
}
