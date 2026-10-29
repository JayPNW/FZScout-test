using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ResourceHunterBonusScout.Reporting;

namespace ResourceHunterBonusScout.Services;

public class BonusApi(HttpClient http) : IBonusApi
{
    record Response<T>(bool Ok, T[]? Data);
    record BonusType(string Key);
    record Zone(string Name);
    record Bonus(string ServerName, string ZoneName, string BonusKey);

    public async Task<IReadOnlyList<string>> GetBonusKeysAsync(Server server) =>
        (await GetAsync<BonusType>(server, "types")).Select(t => t.Key).ToList();

    public async Task<IReadOnlyList<string>> GetZonesAsync(Server server) =>
        (await GetAsync<Zone>(server, "zones")).Select(z => z.Name).ToList();

    async Task<T[]> GetAsync<T>(Server server, string list)
    {
        var uri = new Uri(server.Site, $"api/servers/{server.Name.ToLowerInvariant()}/bonuses/{list}");
        try
        {
            var response = await http.GetFromJsonAsync<Response<T>>(uri);
            return response?.Data ?? throw new HttpRequestException($"{uri} returned no data");
        }
        // A timeout counts as a network error.
        catch (Exception e) when (e is JsonException or TaskCanceledException)
        {
            throw new HttpRequestException($"{uri}: {e.Message}", e);
        }
    }

    public async Task<HttpStatusCode> ReportAsync(BonusReport report, string apiKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(report.Server.Site, "api/client/bonuses"))
        {
            Content = JsonContent.Create(new Bonus(report.Server.Name, report.Zone, report.BonusKey)),
        };
        request.Headers.Add("X-API-Key", apiKey);
        try
        {
            using var response = await http.SendAsync(request);
            return response.StatusCode;
        }
        catch (TaskCanceledException e)
        {
            throw new HttpRequestException($"{request.RequestUri}: {e.Message}", e);
        }
    }
}
