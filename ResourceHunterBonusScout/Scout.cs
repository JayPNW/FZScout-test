using ResourceHunterBonusScout.Logs;
using ResourceHunterBonusScout.Reporting;
using ResourceHunterBonusScout.Scouting;

namespace ResourceHunterBonusScout;

/// <summary>Connects the pieces: log events go to each character's scout, and what the scouts decide goes to the reporter.</summary>
sealed class Scout(LogWatcher watcher, ServerListCache lists, BonusReporter reporter, Func<IReadOnlyCollection<EqProcess>> findEqProcesses)
{
    readonly Dictionary<Character, CharacterScout> _characters = [];
    DateTimeOffset? _nextScan;

    public LogWatcher Watcher => watcher;

    /// <summary>Call about once a second.</summary>
    public async Task TickAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (this._nextScan is null || now >= this._nextScan)
        {
            watcher.Scan(findEqProcesses(), now);
            this._nextScan = now + LogWatcher.ScanInterval;
        }

        foreach (var logEvent in watcher.Poll())
        {
            var serverLists = await lists.GetAsync(logEvent.Character.Server, now, cancellationToken);
            var actions = this.ScoutFor(logEvent.Character).Handle(logEvent.Event, now, serverLists);
            await this.ExecuteAsync(actions, now, cancellationToken);
        }

        foreach (var scout in this._characters.Values)
        {
            await this.ExecuteAsync(scout.Tick(now), now, cancellationToken);
        }

        await lists.TickAsync(now, cancellationToken);
        await reporter.TickAsync(now, cancellationToken);
    }

    CharacterScout ScoutFor(Character character)
    {
        if (!this._characters.TryGetValue(character, out var scout))
        {
            scout = new CharacterScout(character);
            this._characters[character] = scout;
        }

        return scout;
    }

    async Task ExecuteAsync(IReadOnlyList<ScoutAction> actions, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var action in actions)
        {
            await reporter.ExecuteAsync(action, now, cancellationToken);
        }
    }
}
