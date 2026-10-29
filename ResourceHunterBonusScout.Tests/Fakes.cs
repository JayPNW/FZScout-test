using System.Net;
using ResourceHunterBonusScout.Reporting;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

class InMemoryApiKeyStore : IApiKeyStore
{
    public string? ApiKey { get; set; }
    public string? Read() => ApiKey;
    public void Write(string apiKey) => ApiKey = apiKey;
}

class FakeStartWithWindows : IStartWithWindows
{
    public bool IsEnabled { get; set; }
}

class FakeBonusApi : IBonusApi
{
    public List<string> BonusKeys { get; set; } = ["experience", "aa", "loot", "coin", "rare", "faction", "tradeskill", "respawn", "tribute", "item-exp", "none"];
    public List<string> Zones { get; set; } = ["Halls of Honor", "Crushbone", "The Feerrott", "Thurgadin"];

    /// <summary>Set to make loading the lists fail.</summary>
    public HttpRequestException? ListFailure { get; set; }
    public int ListLoads { get; private set; }

    /// <summary>Return a status, or throw <see cref="HttpRequestException"/> for a network error.</summary>
    public Func<BonusReport, HttpStatusCode> Respond { get; set; } = _ => HttpStatusCode.OK;
    public List<(BonusReport Report, string ApiKey)> Reports { get; } = [];

    public Task<IReadOnlyList<string>> GetBonusKeysAsync(Server server)
    {
        ListLoads++;
        return ListFailure is null ? Task.FromResult<IReadOnlyList<string>>(BonusKeys) : Task.FromException<IReadOnlyList<string>>(ListFailure);
    }

    public Task<IReadOnlyList<string>> GetZonesAsync(Server server)
        => ListFailure is null ? Task.FromResult<IReadOnlyList<string>>(Zones) : Task.FromException<IReadOnlyList<string>>(ListFailure);

    public Task<HttpStatusCode> ReportAsync(BonusReport report, string apiKey)
    {
        Reports.Add((report, apiKey));
        return Task.FromResult(Respond(report));
    }
}

class FakeNotifier : INotifier
{
    public List<string> Notifications { get; } = [];
    public void ApiKeyRejected() => Notifications.Add(nameof(ApiKeyRejected));
    public void Banned(Server server) => Notifications.Add($"{nameof(Banned)} {server}");
    public void LoggingOff() => Notifications.Add(nameof(LoggingOff));
}

class ListLog : IAppLog
{
    public List<string> Lines { get; } = [];
    public void Write(string message) => Lines.Add(message);
}

class FakeEqProcesses : IEqProcesses
{
    public List<EqProcess> Running { get; } = [];
    public IReadOnlyList<EqProcess> Find() => Running.ToList();
}

/// <summary>An EverQuest folder with a <c>Logs</c> folder, deleted on dispose.</summary>
sealed class EqFolder : IDisposable
{
    public string Root { get; } = Directory.CreateTempSubdirectory("RHBS").FullName;
    public string ExePath => Path.Combine(Root, "eqgame.exe");
    public string Logs { get; }

    public EqFolder()
    {
        Logs = Path.Combine(Root, "Logs");
        Directory.CreateDirectory(Logs);
    }

    public string LogPath(string character, string server) => Path.Combine(Logs, $"eqlog_{character}_{server}.txt");

    public void Append(string path, params string[] lines)
        => File.AppendAllLines(path, lines.Select(line => $"[Mon Sep 14 16:10:29 2026] {line}"));

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
