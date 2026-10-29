using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Logs;

/// <summary>Finds the logs that EverQuest is writing to, and tails them.</summary>
public class LogWatcher(IEqProcesses eqProcesses, IAppLog log, TimeProvider time)
{
    public static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan LoggingHintDelay = TimeSpan.FromMinutes(5);

    readonly Dictionary<string, Folder> _folders = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<int, WatchedProcess> _processes = [];
    readonly Dictionary<string, (ActiveLog Log, LogTail Tail, string Folder)> _tails = new(StringComparer.OrdinalIgnoreCase);
    DateTimeOffset? _lastScan;

    public IReadOnlyList<ActiveLog> ActiveLogs => [.. this._tails.Values.Select(t => t.Log)];

    public bool EqRunning => this._processes.Count > 0;

    public LogWatcherPoll Poll()
    {
        var now = time.GetUtcNow();
        var hintNeeded = false;
        if (this._lastScan is not { } last || now - last >= ScanInterval)
        {
            hintNeeded = Scan(now);
            this._lastScan = now;
        }

        var newLines = this._tails.Values
            .Select(t => (t.Log, Lines: t.Tail.ReadNewLines()))
            .Where(t => t.Lines.Count > 0)
            .ToList();
        return new(newLines, hintNeeded);
    }

    static IEnumerable<(string Path, long Length)> ListLogs(string folder)
    {
        try
        {
            return new DirectoryInfo(folder).GetFiles(LogFileName.SearchPattern).Select(f => (f.FullName, f.Length)).ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    static string LogsFolder(string exePath) => Path.Combine(Path.GetDirectoryName(exePath)!, "Logs");

    bool Scan(DateTimeOffset now)
    {
        var running = eqProcesses.Find();

        foreach (var id in this._processes.Keys.Except(running.Select(p => p.Id)).ToList())
        {
            this._processes.Remove(id);
        }

        foreach (var process in running)
        {
            if (!this._processes.ContainsKey(process.Id))
            {
                this._processes[process.Id] = new(process.Id, LogsFolder(process.ExePath), now);
            }
        }

        var runningFolders = this._processes.Values.Select(p => p.Folder).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in this._folders.Keys.Where(f => !runningFolders.Contains(f)).ToList())
        {
            StopWatching(folder);
        }

        foreach (var folder in runningFolders)
        {
            ScanFolder(folder, now);
        }

        var hintNeeded = false;
        foreach (var process in this._processes.Values)
        {
            if (process.Hinted || now - process.FirstSeen < LoggingHintDelay)
            {
                continue;
            }
            // Any growing log means logging is on, even one for another server.
            if (this._folders[process.Folder].LastGrowth is { } grew && grew >= process.FirstSeen)
            {
                continue;
            }

            process.Hinted = true;
            hintNeeded = true;
            log.LogWarning($"No log in {process.Folder} has grown since EverQuest process {process.Id} started");
        }
        return hintNeeded;
    }

    void ScanFolder(string path, DateTimeOffset now)
    {
        var isNew = !this._folders.TryGetValue(path, out var folder);
        if (isNew)
        {
            this._folders[path] = folder = new();
            log.LogInformation($"Found EverQuest logs folder {path}");
        }

        foreach (var (file, length) in ListLogs(path))
        {
            var oldLength = folder!.Sizes.GetValueOrDefault(file);
            folder.Sizes[file] = length;
            // Start at the end of the file. A file that appears later started empty.
            if (isNew || length <= oldLength)
            {
                continue;
            }

            folder.LastGrowth = now;
            if (!this._tails.ContainsKey(file) && LogFileName.Parse(file) is (var character, var server))
            {
                this._tails[file] = (new(file, character, server), new(file, oldLength), path);
                log.LogInformation($"Watching {file}");
            }
        }
    }

    void StopWatching(string folder)
    {
        foreach (var (file, _) in this._tails.Where(t => string.Equals(t.Value.Folder, folder, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            this._tails.Remove(file);
            log.LogInformation($"Stopped watching {file}");
        }
        this._folders.Remove(folder);
    }

    class Folder
    {
        public readonly Dictionary<string, long> Sizes = new(StringComparer.OrdinalIgnoreCase);
        public DateTimeOffset? LastGrowth;
    }

    record WatchedProcess(int Id, string Folder, DateTimeOffset FirstSeen)
    {
        public bool Hinted;
    }
}
