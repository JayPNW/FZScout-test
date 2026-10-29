using System.Net;
using ResourceHunterBonusScout.Reporting;

namespace ResourceHunterBonusScout.Services;

/// <summary>The community sites' API. See API.md.</summary>
public interface IBonusApi
{
    /// <summary>The server's <c>types</c> list.</summary>
    /// <exception cref="HttpRequestException">On a network error or an unsuccessful response.</exception>
    Task<IReadOnlyList<string>> GetBonusKeysAsync(Server server);

    /// <summary>The server's <c>zones</c> list.</summary>
    /// <exception cref="HttpRequestException">On a network error or an unsuccessful response.</exception>
    Task<IReadOnlyList<string>> GetZonesAsync(Server server);

    /// <summary>The server's <c>today</c> list.</summary>
    /// <returns>Each zone's bonus key, or <see cref="BonusReport.NoBonus"/> or <see cref="BonusReport.Unconfirmed"/>.</returns>
    /// <exception cref="HttpRequestException">On a network error or an unsuccessful response.</exception>
    Task<IReadOnlyDictionary<string, string>> GetTodaysBonusesAsync(Server server);

    /// <exception cref="HttpRequestException">On a network error.</exception>
    Task<HttpStatusCode> ReportAsync(BonusReport report, string apiKey);
}
