using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Reporting;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout;

/// <summary>Watches EverQuest logs and reports bonuses. Call <see cref="PollAsync"/> about once a second, one call at a time.</summary>
public class Scout
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    readonly LogWatcher watcher;
    readonly ServerListCache lists;
    readonly BonusReporter reporter;
    readonly ErrorState errors = new();
    readonly IApiKeyStore apiKeyStore;
    readonly INotifier notifier;
    readonly IAppLog log;
    readonly TimeProvider time;
    readonly Dictionary<string, CharacterTracker> trackers = new(StringComparer.OrdinalIgnoreCase);

    public Scout(IEqProcesses eqProcesses, IBonusApi api, IApiKeyStore apiKeyStore, INotifier notifier, IAppLog log, TimeProvider time)
    {
        this.apiKeyStore = apiKeyStore;
        this.notifier = notifier;
        this.log = log;
        this.time = time;
        watcher = new(eqProcesses, log, time);
        lists = new(api, errors, log, time);
        reporter = new(api, apiKeyStore, lists, errors, notifier, log, time);
        Status = CurrentStatus();
    }

    public AppStatus Status { get; private set; }

    public event EventHandler StatusChanged = delegate { };

    /// <summary>Loads every server's lists. The lists are public, so this can't check the API key.</summary>
    /// <returns>The servers whose sites can't be reached.</returns>
    public async Task<IReadOnlyList<Server>> CheckConnectionAsync()
    {
        var unreachable = new List<Server>();
        foreach (var server in Server.All)
            if (await lists.LoadAsync(server) == LoadOutcome.CouldNotConnect)
                unreachable.Add(server);
        UpdateStatus();
        return unreachable;
    }

    public async Task PollAsync()
    {
        var poll = watcher.Poll();
        if (poll.LoggingHintNeeded)
            notifier.LoggingOff();

        await SyncTrackersAsync();

        foreach (var (activeLog, lines) in poll.NewLines)
            foreach (var line in lines)
                await HandleAsync(trackers[activeLog.Path], line);

        foreach (var tracker in trackers.Values.ToList())
            if (tracker.Tick() is { } report)
                await reporter.ReportAsync(report);

        await lists.TickAsync();
        await reporter.TickAsync();
        UpdateStatus();
    }

    async Task SyncTrackersAsync()
    {
        var active = watcher.ActiveLogs;
        foreach (var path in trackers.Keys.Except(active.Select(l => l.Path), StringComparer.OrdinalIgnoreCase).ToList())
            trackers.Remove(path);

        foreach (var activeLog in active.Where(l => !trackers.ContainsKey(l.Path)))
        {
            trackers[activeLog.Path] = new(activeLog.Character, activeLog.Server, time);
            await lists.GetAsync(activeLog.Server);
        }
    }

    async Task HandleAsync(CharacterTracker tracker, string line)
    {
        if (LogEvent.Parse(line) is not { } logEvent)
            return;

        var serverLists = logEvent is LogEvent.ZoneEntered or LogEvent.BonusMessage ? await lists.GetAsync(tracker.Server) : null;
        var report = tracker.Handle(logEvent, serverLists);

        if (logEvent is LogEvent.ZoneEntered entered)
            log.Write($"{tracker.Character} ({tracker.Server}) entered {entered.Zone}{(tracker.Zone is null ? ", which isn't on the zones list" : "")}");
        if (report is not null)
            await reporter.ReportAsync(report);
    }

    void UpdateStatus()
    {
        var status = CurrentStatus();
        if (status.HasApiKey == Status.HasApiKey
            && status.LastOperationFailed == Status.LastOperationFailed
            && status.Characters.SequenceEqual(Status.Characters))
            return;

        Status = status;
        StatusChanged(this, EventArgs.Empty);
    }

    AppStatus CurrentStatus() => new(
        trackers.Values.Select(t => new CharacterStatus(t.Character, t.Server.Name, t.Zone)).ToList(),
        HasApiKey: apiKeyStore.Read() is not null,
        errors.LastOperationFailed);
}
