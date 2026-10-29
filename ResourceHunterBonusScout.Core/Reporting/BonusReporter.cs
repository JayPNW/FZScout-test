using System.Net;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Reporting;

/// <summary>Sends reports and handles the responses.</summary>
public class BonusReporter(IBonusApi api, IApiKeyStore apiKeyStore, ServerListCache lists, ErrorState errors, INotifier notifier, IAppLog log, TimeProvider time)
{
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    readonly Dictionary<(string Character, Server Server, string Zone), (BonusReport Report, DateTimeOffset At)> retries = [];
    readonly HashSet<Server> banned = [];
    string? rejectedApiKey;

    public async Task ReportAsync(BonusReport report)
    {
        // A newer report replaces a waiting retry.
        retries.Remove(RetryKey(report));
        await SendAsync(report, isRetry: false);
    }

    /// <summary>Call regularly, to send waiting retries.</summary>
    public async Task TickAsync()
    {
        var now = time.GetUtcNow();
        foreach (var (key, (report, _)) in retries.Where(r => r.Value.At <= now).ToList())
        {
            retries.Remove(key);
            await SendAsync(report, isRetry: true);
        }
    }

    async Task SendAsync(BonusReport report, bool isRetry)
    {
        var description = $"{report.BonusKey} in {report.Zone} for {report.Character} ({report.Server})";

        var apiKey = apiKeyStore.Read();
        var skipReason =
            apiKey is null ? "no API key"
            : apiKey == rejectedApiKey ? "the API key was rejected"
            : banned.Contains(report.Server) ? $"banned on {report.Server}"
            : null;
        if (skipReason is not null)
        {
            log.Write($"Not reporting {description}: {skipReason}");
            return;
        }

        log.Write($"Reporting {description}{(isRetry ? " (retry)" : "")}");
        HttpStatusCode? status = null;
        try
        {
            status = await api.ReportAsync(report, apiKey!);
            log.Write($"Response: {(int)status} {status}");
        }
        catch (HttpRequestException e)
        {
            log.Write($"Report failed: {e.Message}");
        }

        if ((int?)status is >= 200 and < 300)
        {
            errors.Succeeded();
            return;
        }

        switch (status)
        {
            case HttpStatusCode.BadRequest:
                break;
            case HttpStatusCode.Unauthorized:
                rejectedApiKey = apiKey;
                notifier.ApiKeyRejected();
                break;
            case HttpStatusCode.Forbidden:
                banned.Add(report.Server);
                notifier.Banned(report.Server);
                break;
            case HttpStatusCode.NotFound:
                await lists.LoadAsync(report.Server);
                break;
            default:
                if (!isRetry)
                    retries[RetryKey(report)] = (report, time.GetUtcNow() + RetryDelay);
                break;
        }
        // After the zones reload, so its success doesn't hide this failure.
        errors.Failed();
    }

    static (string, Server, string) RetryKey(BonusReport report) => (report.Character, report.Server, report.Zone);
}
