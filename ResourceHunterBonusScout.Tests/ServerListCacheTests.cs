using System.Net;
using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Reporting;

namespace ResourceHunterBonusScout.Tests;

public class ServerListCacheTests
{
    readonly FakeTimeProvider _time = new();
    readonly FakeBonusApi _api = new();
    readonly ErrorState _errors = new();
    readonly ServerListCache _cache;

    public ServerListCacheTests()
    {
        this._cache = new(this._api, this._errors, new ListLog(), this._time);
    }

    [Fact]
    public async Task LoadsOnceAnHour()
    {
        Assert.Equal("Crushbone", (await this._cache.GetAsync(Server.Fangbreaker))!.FindZone("Crushbone"));
        this._time.Advance(ServerListCache.MaxAge - TimeSpan.FromSeconds(1));
        await this._cache.GetAsync(Server.Fangbreaker);
        Assert.Equal(1, this._api.ListLoads);

        this._time.Advance(TimeSpan.FromSeconds(1));
        await this._cache.GetAsync(Server.Fangbreaker);
        Assert.Equal(2, this._api.ListLoads);
    }

    [Fact]
    public async Task EachServerHasItsOwnLists()
    {
        await this._cache.GetAsync(Server.Fangbreaker);
        await this._cache.GetAsync(Server.Frostreaver);

        Assert.Equal(2, this._api.ListLoads);
    }

    [Fact]
    public async Task RetriesOnceAfterAMinute()
    {
        this._api.ListFailure = new("network");

        Assert.Null(await this._cache.GetAsync(Server.Fangbreaker));
        Assert.True(this._errors.LastOperationFailed);

        // Waiting for the retry, so don't load again.
        await this._cache.GetAsync(Server.Fangbreaker);
        this._time.Advance(ServerListCache.RetryDelay - TimeSpan.FromSeconds(1));
        await this._cache.TickAsync();
        Assert.Equal(1, this._api.ListLoads);

        this._time.Advance(TimeSpan.FromSeconds(1));
        await this._cache.TickAsync();
        Assert.Equal(2, this._api.ListLoads);

        this._time.Advance(ServerListCache.RetryDelay);
        await this._cache.TickAsync();
        Assert.Equal(2, this._api.ListLoads);

        // After the retry fails, try again the next time the lists are needed.
        this._api.ListFailure = null;
        Assert.NotNull(await this._cache.GetAsync(Server.Fangbreaker));
        Assert.Equal(3, this._api.ListLoads);
        Assert.False(this._errors.LastOperationFailed);
    }

    [Fact]
    public async Task KeepsOldListsWhenAReloadFails()
    {
        await this._cache.GetAsync(Server.Fangbreaker);
        this._time.Advance(ServerListCache.MaxAge);
        this._api.ListFailure = new("network");

        Assert.NotNull(await this._cache.GetAsync(Server.Fangbreaker));
    }

    [Theory]
    [InlineData(null, LoadOutcome.CouldNotConnect)]
    [InlineData(HttpStatusCode.InternalServerError, LoadOutcome.CouldNotConnect)]
    [InlineData(HttpStatusCode.BadGateway, LoadOutcome.CouldNotConnect)]
    [InlineData(HttpStatusCode.NotFound, LoadOutcome.Failed)]
    public async Task LoadOutcomes(HttpStatusCode? status, LoadOutcome expected)
    {
        this._api.ListFailure = new("failed", null, status);

        Assert.Equal(expected, await this._cache.LoadAsync(Server.Fangbreaker));
    }
}
