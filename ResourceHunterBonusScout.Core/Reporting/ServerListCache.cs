using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Reporting;

public enum LoadOutcome { Loaded, CouldNotConnect, Failed }

/// <summary>Loads each server's lists, and reloads them when they're old.</summary>
public class ServerListCache(IBonusApi api, ErrorState errors, IAppLog log, TimeProvider time)
{
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    readonly Dictionary<Server, Entry> _entries = [];

    /// <returns>The lists, loading them if they're missing or old. While a retry is waiting, the old lists, or null.</returns>
    public async Task<ServerLists?> GetAsync(Server server)
    {
        var entry = EntryFor(server);
        if (entry.RetryAt is null && (entry.Lists is null || time.GetUtcNow() - entry.LoadedAt >= MaxAge))
        {
            await LoadAsync(server, isRetry: false);
        }

        return entry.Lists;
    }

    /// <summary>Loads the lists now, even if they're fresh.</summary>
    public Task<LoadOutcome> LoadAsync(Server server) => LoadAsync(server, isRetry: false);

    /// <summary>Call regularly, to run waiting retries.</summary>
    public async Task TickAsync()
    {
        var now = time.GetUtcNow();
        foreach (var server in this._entries.Where(e => e.Value.RetryAt <= now).Select(e => e.Key).ToList())
        {
            await LoadAsync(server, isRetry: true);
        }
    }

    async Task<LoadOutcome> LoadAsync(Server server, bool isRetry)
    {
        var entry = EntryFor(server);
        entry.RetryAt = null;
        try
        {
            var bonusKeys = await api.GetBonusKeysAsync(server);
            var zones = await api.GetZonesAsync(server);
            entry.Lists = new(bonusKeys, zones);
            entry.LoadedAt = time.GetUtcNow();
            log.Write($"Loaded {server} lists: {bonusKeys.Count} bonus types, {zones.Count} zones");
            errors.Succeeded();
            return LoadOutcome.Loaded;
        }
        catch (HttpRequestException e)
        {
            log.Write($"Loading {server} lists failed{(isRetry ? " again" : "")}: {e.Message}");
            errors.Failed();
            // After a failed retry, try again the next time the lists are needed.
            if (!isRetry)
            {
                entry.RetryAt = time.GetUtcNow() + RetryDelay;
            }

            return e.StatusCode is null || (int)e.StatusCode >= 500 ? LoadOutcome.CouldNotConnect : LoadOutcome.Failed;
        }
    }

    Entry EntryFor(Server server)
    {
        if (!this._entries.TryGetValue(server, out var entry))
        {
            this._entries[server] = entry = new();
        }

        return entry;
    }

    class Entry
    {
        public ServerLists? Lists;
        public DateTimeOffset LoadedAt;
        public DateTimeOffset? RetryAt;
    }
}
