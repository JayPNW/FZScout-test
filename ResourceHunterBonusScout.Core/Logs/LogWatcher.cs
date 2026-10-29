using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Logs;

public record ActiveLog(string Path, string Character, Server Server);

/// <param name="NewLines">Lines added to each active log since the last poll.</param>
/// <param name="LoggingHintNeeded">Whether an EQ process has run long enough without any of its logs growing.</param>
public record LogWatcherPoll(IReadOnlyList<(ActiveLog Log, IReadOnlyList<string> Lines)> NewLines, bool LoggingHintNeeded);

/// <summary>Finds the logs that EverQuest is writing to, and tails them.</summary>
public class LogWatcher(IEqProcesses eqProcesses, IAppLog log, TimeProvider time)
{
    public static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan LoggingHintDelay = TimeSpan.FromMinutes(5);

    class Folder
    {
        public readonly Dictionary<string, long> Sizes = new(StringComparer.OrdinalIgnoreCase);
        public DateTimeOffset? LastGrowth;
    }

    record WatchedProcess(int Id, string Folder, DateTimeOffset FirstSeen)
    {
        public bool Hinted;
    }

    readonly Dictionary<string, Folder> folders = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<int, WatchedProcess> processes = [];
    readonly Dictionary<string, (ActiveLog Log, LogTail Tail, string Folder)> tails = new(StringComparer.OrdinalIgnoreCase);
    DateTimeOffset? lastScan;

    public IReadOnlyList<ActiveLog> ActiveLogs => tails.Values.Select(t => t.Log).ToList();

    public LogWatcherPoll Poll()
    {
        var now = time.GetUtcNow();
        var hintNeeded = false;
        if (lastScan is not { } last || now - last >= ScanInterval)
        {
            hintNeeded = Scan(now);
            lastScan = now;
        }

        var newLines = tails.Values
            .Select(t => (t.Log, Lines: t.Tail.ReadNewLines()))
            .Where(t => t.Lines.Count > 0)
            .ToList();
        return new(newLines, hintNeeded);
    }

    bool Scan(DateTimeOffset now)
    {
        var running = eqProcesses.Find();

        foreach (var id in processes.Keys.Except(running.Select(p => p.Id)).ToList())
            processes.Remove(id);
        foreach (var process in running)
            if (!processes.ContainsKey(process.Id))
                processes[process.Id] = new(process.Id, LogsFolder(process.ExePath), now);

        var runningFolders = processes.Values.Select(p => p.Folder).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders.Keys.Where(f => !runningFolders.Contains(f)).ToList())
            StopWatching(folder);
        foreach (var folder in runningFolders)
            ScanFolder(folder, now);

        var hintNeeded = false;
        foreach (var process in processes.Values)
        {
            if (process.Hinted || now - process.FirstSeen < LoggingHintDelay)
                continue;
            // Any growing log means logging is on, even one for another server.
            if (folders[process.Folder].LastGrowth is { } grew && grew >= process.FirstSeen)
                continue;
            process.Hinted = true;
            hintNeeded = true;
            log.Write($"No log in {process.Folder} has grown since EverQuest process {process.Id} started");
        }
        return hintNeeded;
    }

    void ScanFolder(string path, DateTimeOffset now)
    {
        var isNew = !folders.TryGetValue(path, out var folder);
        if (isNew)
        {
            folders[path] = folder = new();
            log.Write($"Found EverQuest logs folder {path}");
        }

        foreach (var (file, length) in ListLogs(path))
        {
            var oldLength = folder!.Sizes.GetValueOrDefault(file);
            folder.Sizes[file] = length;
            // Start at the end of the file. A file that appears later started empty.
            if (isNew || length <= oldLength)
                continue;

            folder.LastGrowth = now;
            if (!tails.ContainsKey(file) && LogFileName.Parse(file) is (var character, var server))
            {
                tails[file] = (new(file, character, server), new(file, oldLength), path);
                log.Write($"Watching {file}");
            }
        }
    }

    void StopWatching(string folder)
    {
        foreach (var (file, _) in tails.Where(t => string.Equals(t.Value.Folder, folder, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            tails.Remove(file);
            log.Write($"Stopped watching {file}");
        }
        folders.Remove(folder);
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
}
