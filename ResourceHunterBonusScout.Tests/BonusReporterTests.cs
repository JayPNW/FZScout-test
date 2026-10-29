using System.Net;
using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Reporting;

namespace ResourceHunterBonusScout.Tests;

public class BonusReporterTests
{
    static readonly BonusReport CrushboneLoot = new("Bowslinger", Server.Fangbreaker, "Crushbone", "loot");
    static readonly BonusReport CrushboneNone = CrushboneLoot with { BonusKey = BonusReport.NoBonus };

    readonly FakeTimeProvider _time = new();
    readonly FakeBonusApi _api = new();
    readonly InMemoryApiKeyStore _apiKeyStore = new() { ApiKey = "fz_live_abc" };
    readonly ErrorState _errors = new();
    readonly FakeNotifier _notifier = new();
    readonly ListLog _log = new();
    readonly BonusReporter _reporter;

    public BonusReporterTests()
    {
        this._reporter = new(this._api, this._apiKeyStore, new(this._api, this._errors, this._log, this._time), this._errors, this._notifier, this._log, this._time);
    }

    async Task WaitForRetry()
    {
        this._time.Advance(BonusReporter.RetryDelay);
        await this._reporter.TickAsync();
    }

    [Fact]
    public async Task SendsTheReportWithTheKey()
    {
        this._errors.Failed();

        await this._reporter.ReportAsync(CrushboneLoot);

        Assert.Equal([(CrushboneLoot, "fz_live_abc")], this._api.Reports);
        Assert.False(this._errors.LastOperationFailed);
    }

    [Fact]
    public async Task NeverLogsTheKey()
    {
        this._api.Respond = _ => HttpStatusCode.Unauthorized;

        await this._reporter.ReportAsync(CrushboneLoot);
        await this._reporter.ReportAsync(CrushboneLoot);

        Assert.NotEmpty(this._log.Lines);
        Assert.DoesNotContain(this._log.Lines, line => line.Contains("fz_live_abc"));
    }

    [Fact]
    public async Task NothingIsSentWithoutAKey()
    {
        this._apiKeyStore.ApiKey = null;

        await this._reporter.ReportAsync(CrushboneLoot);

        Assert.Empty(this._api.Reports);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task ServerErrorRetriesOnceAfterAMinute(HttpStatusCode status)
    {
        this._api.Respond = _ => status;

        await this._reporter.ReportAsync(CrushboneLoot);
        Assert.True(this._errors.LastOperationFailed);

        this._time.Advance(BonusReporter.RetryDelay - TimeSpan.FromSeconds(1));
        await this._reporter.TickAsync();
        Assert.Single(this._api.Reports);

        this._time.Advance(TimeSpan.FromSeconds(1));
        await this._reporter.TickAsync();
        Assert.Equal(2, this._api.Reports.Count);

        await WaitForRetry();
        Assert.Equal(2, this._api.Reports.Count);
    }

    [Fact]
    public async Task NetworkErrorRetries()
    {
        this._api.Respond = _ => throw new HttpRequestException("network");

        await this._reporter.ReportAsync(CrushboneLoot);
        this._api.Respond = _ => HttpStatusCode.OK;
        await WaitForRetry();

        Assert.Equal(2, this._api.Reports.Count);
        Assert.False(this._errors.LastOperationFailed);
    }

    [Fact]
    public async Task NewerReportReplacesAWaitingRetry()
    {
        this._api.Respond = _ => HttpStatusCode.InternalServerError;
        await this._reporter.ReportAsync(CrushboneLoot with { BonusKey = "none" });
        await this._reporter.ReportAsync(CrushboneLoot with { Zone = "Thurgadin" });
        await this._reporter.ReportAsync(CrushboneLoot);

        this._api.Reports.Clear();
        await WaitForRetry();

        Assert.Equal(["Crushbone loot", "Thurgadin loot"], this._api.Reports.Select(r => $"{r.Report.Zone} {r.Report.BonusKey}").Order());
    }

    [Fact]
    public async Task BadRequestIsNotRetried()
    {
        this._api.Respond = _ => HttpStatusCode.BadRequest;

        await this._reporter.ReportAsync(CrushboneLoot);
        await WaitForRetry();

        Assert.Single(this._api.Reports);
        Assert.True(this._errors.LastOperationFailed);
    }

    [Fact]
    public async Task RejectedKeyStopsReportingUntilTheKeyChanges()
    {
        this._api.Respond = _ => HttpStatusCode.Unauthorized;

        await this._reporter.ReportAsync(CrushboneLoot);
        await WaitForRetry();
        await this._reporter.ReportAsync(CrushboneLoot with { Server = Server.Frostreaver });

        Assert.Single(this._api.Reports);
        Assert.Equal(["ApiKeyRejected"], this._notifier.Notifications);

        this._apiKeyStore.ApiKey = "fz_live_new";
        this._api.Respond = _ => HttpStatusCode.OK;
        await this._reporter.ReportAsync(CrushboneLoot);

        Assert.Equal("fz_live_new", this._api.Reports[^1].ApiKey);
    }

    [Fact]
    public async Task BannedStopsReportingToThatSite()
    {
        this._api.Respond = r => r.Server == Server.Fangbreaker ? HttpStatusCode.Forbidden : HttpStatusCode.OK;

        await this._reporter.ReportAsync(CrushboneLoot);
        await this._reporter.ReportAsync(CrushboneLoot);
        await this._reporter.ReportAsync(CrushboneLoot with { Server = Server.Frostreaver });

        Assert.Equal([Server.Fangbreaker, Server.Frostreaver], this._api.Reports.Select(r => r.Report.Server));
        Assert.Equal(["Banned Fangbreaker"], this._notifier.Notifications);
    }

    [Fact]
    public async Task ZoneNotFoundReloadsTheLists()
    {
        this._api.Respond = _ => HttpStatusCode.NotFound;

        await this._reporter.ReportAsync(CrushboneLoot);
        await WaitForRetry();

        Assert.Single(this._api.Reports);
        Assert.Equal(1, this._api.ListLoads);
        Assert.True(this._errors.LastOperationFailed);
    }

    [Theory]
    [InlineData(BonusReport.NoBonus)]
    [InlineData(BonusReport.Unconfirmed)]
    public async Task NoBonusIsSentWhenTheSiteHasNoBonus(string siteBonus)
    {
        this._api.TodaysBonuses["Crushbone"] = siteBonus;

        await this._reporter.ReportAsync(CrushboneNone);

        Assert.Equal([(CrushboneNone, "fz_live_abc")], this._api.Reports);
    }

    [Fact]
    public async Task NoBonusIsNotSentWhenTheSiteShowsABonus()
    {
        this._api.TodaysBonuses["Crushbone"] = "loot";

        await this._reporter.ReportAsync(CrushboneNone);

        Assert.Empty(this._api.Reports);
        Assert.Contains("Not reporting none in Crushbone for Bowslinger (Fangbreaker): the site shows loot", this._log.Lines);
    }

    [Fact]
    public async Task ABonusIsSentEvenWhenTheSiteShowsAnother()
    {
        this._api.TodaysBonuses["Crushbone"] = "coin";

        await this._reporter.ReportAsync(CrushboneLoot);

        Assert.Single(this._api.Reports);
    }

    [Fact]
    public async Task FailingToCheckTheSiteSkipsNoBonus()
    {
        this._api.TodaysBonusesFailure = new("network");

        await this._reporter.ReportAsync(CrushboneNone);
        this._api.TodaysBonusesFailure = null;
        await WaitForRetry();

        Assert.Empty(this._api.Reports);
        Assert.False(this._errors.LastOperationFailed);
        Assert.Contains("Not reporting none in Crushbone for Bowslinger (Fangbreaker): checking the site failed: network", this._log.Lines);
    }
}
