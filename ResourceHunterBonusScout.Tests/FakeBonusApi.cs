using System.Net;
using ResourceHunterBonusScout.Reporting;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

class FakeBonusApi : IBonusApi
{
    public List<string> BonusKeys { get; set; } = ["experience", "aa", "loot", "coin", "rare", "faction", "tradeskill", "respawn", "tribute", "item-exp", "none"];
    public List<string> Zones { get; set; } = ["Halls of Honor", "Crushbone", "The Feerrott", "Thurgadin"];

    /// <summary>Set to make loading the lists fail.</summary>
    public HttpRequestException? ListFailure { get; set; }
    public int ListLoads { get; private set; }

    /// <summary>Zone to bonus, for the <c>today</c> list. Zones not listed are unconfirmed.</summary>
    public Dictionary<string, string> TodaysBonuses { get; } = [];

    /// <summary>Set to make loading the <c>today</c> list fail.</summary>
    public HttpRequestException? TodaysBonusesFailure { get; set; }

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

    public Task<IReadOnlyDictionary<string, string>> GetTodaysBonusesAsync(Server server)
        => TodaysBonusesFailure is null ? Task.FromResult<IReadOnlyDictionary<string, string>>(TodaysBonuses) : Task.FromException<IReadOnlyDictionary<string, string>>(TodaysBonusesFailure);

    public Task<HttpStatusCode> ReportAsync(BonusReport report, string apiKey)
    {
        Reports.Add((report, apiKey));
        return Task.FromResult(Respond(report));
    }
}
