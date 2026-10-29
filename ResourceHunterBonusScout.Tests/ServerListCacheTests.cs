using System.Net;
using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Reporting;

namespace ResourceHunterBonusScout.Tests;

public class ServerListCacheTests
{
    readonly FakeTimeProvider time = new();
    readonly FakeBonusApi api = new();
    readonly ErrorState errors = new();
    readonly ServerListCache cache;

    public ServerListCacheTests()
    {
        cache = new(api, errors, new ListLog(), time);
    }

    [Fact]
    public async Task LoadsOnceAnHour()
    {
        Assert.Equal("Crushbone", (await cache.GetAsync(Server.Fangbreaker))!.FindZone("Crushbone"));
        time.Advance(ServerListCache.MaxAge - TimeSpan.FromSeconds(1));
        await cache.GetAsync(Server.Fangbreaker);
        Assert.Equal(1, api.ListLoads);

        time.Advance(TimeSpan.FromSeconds(1));
        await cache.GetAsync(Server.Fangbreaker);
        Assert.Equal(2, api.ListLoads);
    }

    [Fact]
    public async Task EachServerHasItsOwnLists()
    {
        await cache.GetAsync(Server.Fangbreaker);
        await cache.GetAsync(Server.Frostreaver);

        Assert.Equal(2, api.ListLoads);
    }

    [Fact]
    public async Task RetriesOnceAfterAMinute()
    {
        api.ListFailure = new("network");

        Assert.Null(await cache.GetAsync(Server.Fangbreaker));
        Assert.True(errors.LastOperationFailed);

        // Waiting for the retry, so don't load again.
        await cache.GetAsync(Server.Fangbreaker);
        time.Advance(ServerListCache.RetryDelay - TimeSpan.FromSeconds(1));
        await cache.TickAsync();
        Assert.Equal(1, api.ListLoads);

        time.Advance(TimeSpan.FromSeconds(1));
        await cache.TickAsync();
        Assert.Equal(2, api.ListLoads);

        time.Advance(ServerListCache.RetryDelay);
        await cache.TickAsync();
        Assert.Equal(2, api.ListLoads);

        // After the retry fails, try again the next time the lists are needed.
        api.ListFailure = null;
        Assert.NotNull(await cache.GetAsync(Server.Fangbreaker));
        Assert.Equal(3, api.ListLoads);
        Assert.False(errors.LastOperationFailed);
    }

    [Fact]
    public async Task KeepsOldListsWhenAReloadFails()
    {
        await cache.GetAsync(Server.Fangbreaker);
        time.Advance(ServerListCache.MaxAge);
        api.ListFailure = new("network");

        Assert.NotNull(await cache.GetAsync(Server.Fangbreaker));
    }

    [Theory]
    [InlineData(null, LoadOutcome.CouldNotConnect)]
    [InlineData(HttpStatusCode.InternalServerError, LoadOutcome.CouldNotConnect)]
    [InlineData(HttpStatusCode.BadGateway, LoadOutcome.CouldNotConnect)]
    [InlineData(HttpStatusCode.NotFound, LoadOutcome.Failed)]
    public async Task LoadOutcomes(HttpStatusCode? status, LoadOutcome expected)
    {
        api.ListFailure = new("failed", null, status);

        Assert.Equal(expected, await cache.LoadAsync(Server.Fangbreaker));
    }
}
