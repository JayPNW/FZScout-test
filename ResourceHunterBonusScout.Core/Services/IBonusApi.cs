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

    /// <exception cref="HttpRequestException">On a network error.</exception>
    Task<HttpStatusCode> ReportAsync(BonusReport report, string apiKey);
}
