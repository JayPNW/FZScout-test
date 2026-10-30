using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ResourceHunterBonusScout.Site;

/// <summary>Talks to fangbreaker.zone and frostreaver.zone. See InternalDocumentation/API.md.</summary>
sealed class FangbreakerZoneClient(HttpClient http)
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ApiResult<IReadOnlySet<BonusKey>>> GetTypesAsync(Server server, CancellationToken cancellationToken = default)
    {
        var result = await this.GetAsync<TypeDto[]>(server, "types", cancellationToken);
        return Map(result, types => (IReadOnlySet<BonusKey>)types.Select(t => new BonusKey(t.Key)).ToHashSet());
    }

    public async Task<ApiResult<IReadOnlySet<string>>> GetZonesAsync(Server server, CancellationToken cancellationToken = default)
    {
        var result = await this.GetAsync<ZoneDto[]>(server, "zones", cancellationToken);
        return Map(result, zones => (IReadOnlySet<string>)zones.Select(z => z.Name).ToHashSet());
    }

    /// <summary>Today's bonus for each zone, keyed by the site's zone name. Zones nobody has reported are <see cref="BonusKey.Unconfirmed"/>.</summary>
    public async Task<ApiResult<IReadOnlyDictionary<string, BonusKey>>> GetTodayAsync(Server server, CancellationToken cancellationToken = default)
    {
        var result = await this.GetAsync<TodayDto[]>(server, "today", cancellationToken);
        return Map(result, zones => (IReadOnlyDictionary<string, BonusKey>)zones.ToDictionary(z => z.Name, z => new BonusKey(z.Bonus)));
    }

    public async Task<ApiResult<bool>> ReportBonusAsync(BonusReport report, ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(report.Server.Site, "/api/client/bonuses"));
        request.Headers.Add("X-API-Key", apiKey.Value);
        request.Content = JsonContent.Create(new ReportDto(report.Server.Name, report.Zone, report.Bonus.Value), options: Json);
        var result = await this.SendAsync<ReportResponseDto>(request, cancellationToken);
        return Map(result, r => r.Updated);
    }

    async Task<ApiResult<T>> GetAsync<T>(Server server, string list, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(server.Site, $"/api/servers/{server.Slug}/bonuses/{list}"));
        return await this.SendAsync<T>(request, cancellationToken);
    }

    async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var description = $"{request.Method} {request.RequestUri}";
        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return ApiResult<T>.Failure(OutcomeFor(response.StatusCode), $"{description} returned {(int)response.StatusCode}: {body}");
            }

            var envelope = JsonSerializer.Deserialize<EnvelopeDto<T>>(body, Json);
            if (envelope is not { Ok: true, Data: not null })
            {
                return ApiResult<T>.Failure(ApiOutcome.Unavailable, $"{description} returned an unexpected body: {body}");
            }

            return ApiResult<T>.Success(envelope.Data);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return ApiResult<T>.Failure(ApiOutcome.Unavailable, $"{description} failed: {e.Message}");
        }
    }

    static ApiOutcome OutcomeFor(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => ApiOutcome.BadPayload,
        HttpStatusCode.Unauthorized => ApiOutcome.KeyRejected,
        HttpStatusCode.Forbidden => ApiOutcome.Banned,
        HttpStatusCode.NotFound => ApiOutcome.NotFound,
        _ => ApiOutcome.Unavailable,
    };

    static ApiResult<TOut> Map<TIn, TOut>(ApiResult<TIn> result, Func<TIn, TOut> map)
    {
        if (result.IsSuccess)
        {
            return ApiResult<TOut>.Success(map(result.Value!));
        }

        return ApiResult<TOut>.Failure(result.Outcome, result.Error!);
    }

    sealed record EnvelopeDto<T>(bool Ok, T? Data);

    sealed record TypeDto(string Key);

    sealed record ZoneDto(string Name);

    sealed record TodayDto(string Name, string Bonus);

    sealed record ReportDto(string ServerName, string ZoneName, string BonusKey);

    sealed record ReportResponseDto(bool Updated);
}
