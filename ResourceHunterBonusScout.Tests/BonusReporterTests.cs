using System.Net;
using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Reporting;

namespace ResourceHunterBonusScout.Tests;

public class BonusReporterTests
{
    static readonly BonusReport CrushboneLoot = new("Bowslinger", Server.Fangbreaker, "Crushbone", "loot");

    readonly FakeTimeProvider time = new();
    readonly FakeBonusApi api = new();
    readonly InMemoryApiKeyStore apiKeyStore = new() { ApiKey = "fz_live_abc" };
    readonly ErrorState errors = new();
    readonly FakeNotifier notifier = new();
    readonly ListLog log = new();
    readonly BonusReporter reporter;

    public BonusReporterTests()
    {
        reporter = new(api, apiKeyStore, new(api, errors, log, time), errors, notifier, log, time);
    }

    async Task WaitForRetry()
    {
        time.Advance(BonusReporter.RetryDelay);
        await reporter.TickAsync();
    }

    [Fact]
    public async Task SendsTheReportWithTheKey()
    {
        errors.Failed();

        await reporter.ReportAsync(CrushboneLoot);

        Assert.Equal([(CrushboneLoot, "fz_live_abc")], api.Reports);
        Assert.False(errors.LastOperationFailed);
    }

    [Fact]
    public async Task NeverLogsTheKey()
    {
        api.Respond = _ => HttpStatusCode.Unauthorized;

        await reporter.ReportAsync(CrushboneLoot);
        await reporter.ReportAsync(CrushboneLoot);

        Assert.NotEmpty(log.Lines);
        Assert.DoesNotContain(log.Lines, line => line.Contains("fz_live_abc"));
    }

    [Fact]
    public async Task NothingIsSentWithoutAKey()
    {
        apiKeyStore.ApiKey = null;

        await reporter.ReportAsync(CrushboneLoot);

        Assert.Empty(api.Reports);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task ServerErrorRetriesOnceAfterAMinute(HttpStatusCode status)
    {
        api.Respond = _ => status;

        await reporter.ReportAsync(CrushboneLoot);
        Assert.True(errors.LastOperationFailed);

        time.Advance(BonusReporter.RetryDelay - TimeSpan.FromSeconds(1));
        await reporter.TickAsync();
        Assert.Single(api.Reports);

        time.Advance(TimeSpan.FromSeconds(1));
        await reporter.TickAsync();
        Assert.Equal(2, api.Reports.Count);

        await WaitForRetry();
        Assert.Equal(2, api.Reports.Count);
    }

    [Fact]
    public async Task NetworkErrorRetries()
    {
        api.Respond = _ => throw new HttpRequestException("network");

        await reporter.ReportAsync(CrushboneLoot);
        api.Respond = _ => HttpStatusCode.OK;
        await WaitForRetry();

        Assert.Equal(2, api.Reports.Count);
        Assert.False(errors.LastOperationFailed);
    }

    [Fact]
    public async Task NewerReportReplacesAWaitingRetry()
    {
        api.Respond = _ => HttpStatusCode.InternalServerError;
        await reporter.ReportAsync(CrushboneLoot with { BonusKey = "none" });
        await reporter.ReportAsync(CrushboneLoot with { Zone = "Thurgadin" });
        await reporter.ReportAsync(CrushboneLoot);

        api.Reports.Clear();
        await WaitForRetry();

        Assert.Equal(["Crushbone loot", "Thurgadin loot"], api.Reports.Select(r => $"{r.Report.Zone} {r.Report.BonusKey}").Order());
    }

    [Fact]
    public async Task BadRequestIsNotRetried()
    {
        api.Respond = _ => HttpStatusCode.BadRequest;

        await reporter.ReportAsync(CrushboneLoot);
        await WaitForRetry();

        Assert.Single(api.Reports);
        Assert.True(errors.LastOperationFailed);
    }

    [Fact]
    public async Task RejectedKeyStopsReportingUntilTheKeyChanges()
    {
        api.Respond = _ => HttpStatusCode.Unauthorized;

        await reporter.ReportAsync(CrushboneLoot);
        await WaitForRetry();
        await reporter.ReportAsync(CrushboneLoot with { Server = Server.Frostreaver });

        Assert.Single(api.Reports);
        Assert.Equal(["ApiKeyRejected"], notifier.Notifications);

        apiKeyStore.ApiKey = "fz_live_new";
        api.Respond = _ => HttpStatusCode.OK;
        await reporter.ReportAsync(CrushboneLoot);

        Assert.Equal("fz_live_new", api.Reports[^1].ApiKey);
    }

    [Fact]
    public async Task BannedStopsReportingToThatSite()
    {
        api.Respond = r => r.Server == Server.Fangbreaker ? HttpStatusCode.Forbidden : HttpStatusCode.OK;

        await reporter.ReportAsync(CrushboneLoot);
        await reporter.ReportAsync(CrushboneLoot);
        await reporter.ReportAsync(CrushboneLoot with { Server = Server.Frostreaver });

        Assert.Equal([Server.Fangbreaker, Server.Frostreaver], api.Reports.Select(r => r.Report.Server));
        Assert.Equal(["Banned Fangbreaker"], notifier.Notifications);
    }

    [Fact]
    public async Task ZoneNotFoundReloadsTheLists()
    {
        api.Respond = _ => HttpStatusCode.NotFound;

        await reporter.ReportAsync(CrushboneLoot);
        await WaitForRetry();

        Assert.Single(api.Reports);
        Assert.Equal(1, api.ListLoads);
        Assert.True(errors.LastOperationFailed);
    }
}
