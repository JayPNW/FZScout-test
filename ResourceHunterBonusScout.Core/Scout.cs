using System.Globalization;
using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Reporting;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout;

/// <summary>Watches EverQuest logs and reports bonuses. Call <see cref="PollAsync"/> about once a second, one call at a time.</summary>
public class Scout
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    readonly LogWatcher _watcher;
    readonly ServerListCache _lists;
    readonly BonusReporter _reporter;
    readonly ErrorState _errors = new();
    readonly IApiKeyStore _apiKeyStore;
    readonly ISettingsStore _settings;
    readonly INotifier _notifier;
    readonly IAppLog _log;
    readonly TimeProvider _time;
    readonly Dictionary<string, CharacterTracker> _trackers = new(StringComparer.OrdinalIgnoreCase);

    public Scout(IEqProcesses eqProcesses, IBonusApi api, IApiKeyStore apiKeyStore, ISettingsStore settings, INotifier notifier, IAppLog log, TimeProvider time)
    {
        this._apiKeyStore = apiKeyStore;
        this._settings = settings;
        this._notifier = notifier;
        this._log = log;
        this._time = time;
        this._watcher = new(eqProcesses, log, time);
        this._lists = new(api, this._errors, log, time);
        this._reporter = new(api, apiKeyStore, this._lists, this._errors, notifier, log, time);
        Status = CurrentStatus();
    }

    public event EventHandler StatusChanged = delegate { };

    public AppStatus Status { get; private set; }

    /// <summary>Loads every server's lists. The lists are public, so this can't check the API key.</summary>
    /// <returns>The servers whose sites can't be reached.</returns>
    public async Task<IReadOnlyList<Server>> CheckConnectionAsync()
    {
        var unreachable = new List<Server>();
        foreach (var server in Server.All)
        {
            if (await this._lists.LoadAsync(server) == LoadOutcome.CouldNotConnect)
            {
                unreachable.Add(server);
            }
        }

        UpdateStatus();
        return unreachable;
    }

    public async Task PollAsync()
    {
        var poll = this._watcher.Poll();
        if (poll.LoggingHintNeeded)
        {
            this._notifier.LoggingOff();
        }

        await SyncTrackersAsync();

        foreach (var (activeLog, lines) in poll.NewLines)
        {
            foreach (var line in lines)
            {
                await HandleAsync(this._trackers[activeLog.Path], line);
            }
        }

        foreach (var tracker in this._trackers.Values.ToList())
        {
            if (tracker.Tick() is { } report)
            {
                await this._reporter.ReportAsync(report);
            }
        }

        await this._lists.TickAsync();
        await this._reporter.TickAsync();
        UpdateStatus();
    }

    async Task SyncTrackersAsync()
    {
        var active = this._watcher.ActiveLogs;
        foreach (var path in this._trackers.Keys.Except(active.Select(l => l.Path), StringComparer.OrdinalIgnoreCase).ToList())
        {
            if (this._trackers[path] is { NoBonusAt: not null } tracker)
            {
                this._log.LogInformation($"{tracker.Character} ({tracker.Server}) log is no longer watched; timeout cancelled.");
            }

            this._trackers.Remove(path);
        }

        foreach (var activeLog in active.Where(l => !this._trackers.ContainsKey(l.Path)))
        {
            this._trackers[activeLog.Path] = new(activeLog.Character, activeLog.Server, this._time);
            await this._lists.GetAsync(activeLog.Server);
        }
    }

    async Task HandleAsync(CharacterTracker tracker, string line)
    {
        if (LogEvent.Parse(line) is not { } logEvent)
        {
            return;
        }

        var serverLists = logEvent is LogEvent.ZoneEntered or LogEvent.BonusMessage ? await this._lists.GetAsync(tracker.Server) : null;
        var noBonusAtBefore = tracker.NoBonusAt;
        tracker.IsIdleTimerOn = this._settings.Read().IdleTimer;
        var report = tracker.Handle(logEvent, serverLists);

        var timer = tracker.NoBonusAt is { } at && at != noBonusAtBefore ? $"; waiting for timeout at {ClockTime(at)}"
            : tracker.NoBonusAt is null && noBonusAtBefore is not null ? logEvent is LogEvent.CampStarted ? "; timeout paused" : "; timeout cancelled"
            : "";
        if (logEvent is LogEvent.ZoneEntered || timer.Length > 0)
        {
            var what = logEvent switch
            {
                LogEvent.ZoneEntered entered when tracker.Zone is null => $"entered {entered.Zone}, which isn't on the zones list",
                LogEvent.ZoneEntered entered => $"entered {entered.Zone}",
                LogEvent.BonusMessage bonus => $"got the {bonus.BonusKey} bonus message",
                LogEvent.Kill => "got a kill",
                LogEvent.CampStarted => "started to camp",
                LogEvent.CampAbandoned => "stopped camping",
                LogEvent.DailyReset => "saw the daily reset",
                LogEvent.PersonaChanged => "changed persona",
                _ => logEvent.ToString(),
            };
            var message = $"{tracker.Character} ({tracker.Server}) {what}{timer}.";
            if (logEvent is LogEvent.ZoneEntered && tracker.Zone is null)
            {
                this._log.LogError(message);
                this._errors.Failed();
            }
            else
            {
                this._log.LogInformation(message);
            }
        }

        if (report is not null)
        {
            await this._reporter.ReportAsync(report);
        }
    }

    string ClockTime(DateTimeOffset at) => TimeZoneInfo.ConvertTime(at, this._time.LocalTimeZone).ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    void UpdateStatus()
    {
        var status = CurrentStatus();
        if (status.HasApiKey == Status.HasApiKey
            && status.LastOperationFailed == Status.LastOperationFailed
            && status.EqRunning == Status.EqRunning
            && status.Characters.SequenceEqual(Status.Characters))
        {
            return;
        }

        Status = status;
        StatusChanged(this, EventArgs.Empty);
    }

    AppStatus CurrentStatus() => new(
        [.. this._trackers.Values.Select(t => new CharacterStatus(t.Character, t.Server.Name, t.Zone, t.NoBonusAt))],
        HasApiKey: this._apiKeyStore.Read() is not null,
        this._errors.LastOperationFailed,
        this._watcher.EqRunning);
}
