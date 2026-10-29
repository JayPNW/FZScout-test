using System.Net;
using System.Text;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

public class BonusApiTests
{
    [Fact]
    public async Task BonusKeys()
    {
        var handler = new Handler(HttpStatusCode.OK, """{"ok":true,"data":[{"id":"1","key":"experience","label":"Experience","sortOrder":10},{"id":"2","key":"none","label":"No Bonus","sortOrder":999}]}""");

        var keys = await new BonusApi(new(handler)).GetBonusKeysAsync(Server.Frostreaver);

        Assert.Equal(["experience", "none"], keys);
        Assert.Equal("https://frostreaver.zone/api/servers/frostreaver/bonuses/types", handler.Request!.RequestUri!.ToString());
    }

    [Fact]
    public async Task Zones()
    {
        var handler = new Handler(HttpStatusCode.OK, """{"ok":true,"data":[{"id":"1","slug":"akanon","name":"Ak'Anon","isActive":true}]}""");

        var zones = await new BonusApi(new(handler)).GetZonesAsync(Server.Fangbreaker);

        Assert.Equal(["Ak'Anon"], zones);
        Assert.Equal("https://fangbreaker.zone/api/servers/fangbreaker/bonuses/zones", handler.Request!.RequestUri!.ToString());
    }

    [Fact]
    public async Task TodaysBonuses()
    {
        var handler = new Handler(HttpStatusCode.OK, """{"ok":true,"data":[{"id":null,"name":"Abysmal Sea","bonus":"unconfirmed","state":"UNCONFIRMED"},{"id":"1","name":"Bastion of Thunder","bonus":"experience","state":"ACTIVE"}]}""");

        var bonuses = await new BonusApi(new(handler)).GetTodaysBonusesAsync(Server.Fangbreaker);

        Assert.Equal(new Dictionary<string, string> { ["Abysmal Sea"] = "unconfirmed", ["Bastion of Thunder"] = "experience" }, bonuses);
        Assert.Equal("https://fangbreaker.zone/api/servers/fangbreaker/bonuses/today", handler.Request!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListFailureHasTheStatus()
    {
        var api = new BonusApi(new(new Handler(HttpStatusCode.BadGateway, "")));

        var e = await Assert.ThrowsAsync<HttpRequestException>(() => api.GetZonesAsync(Server.Fangbreaker));

        Assert.Equal(HttpStatusCode.BadGateway, e.StatusCode);
    }

    [Fact]
    public async Task BadJsonIsAFailure()
    {
        var api = new BonusApi(new(new Handler(HttpStatusCode.OK, "<html>")));

        await Assert.ThrowsAsync<HttpRequestException>(() => api.GetZonesAsync(Server.Fangbreaker));
    }

    [Fact]
    public async Task Report()
    {
        var handler = new Handler(HttpStatusCode.Unauthorized, """{"ok":false}""");

        var status = await new BonusApi(new(handler)).ReportAsync(new("Bowslinger", Server.Fangbreaker, "Halls of Honor", "item-exp"), "fz_live_abc");

        Assert.Equal(HttpStatusCode.Unauthorized, status);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://fangbreaker.zone/api/client/bonuses", handler.Request.RequestUri!.ToString());
        Assert.Equal(["fz_live_abc"], handler.Request.Headers.GetValues("X-API-Key"));
        Assert.Equal("application/json", handler.Request.Content!.Headers.ContentType!.MediaType);
        Assert.Equal("""{"serverName":"Fangbreaker","zoneName":"Halls of Honor","bonusKey":"item-exp"}""", handler.RequestBody);
    }

    class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request;
        public string? RequestBody;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Request = request;
            this.RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
