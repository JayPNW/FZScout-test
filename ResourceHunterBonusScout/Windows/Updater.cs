using ResourceHunterBonusScout.Services;
using Velopack;
using Velopack.Sources;

namespace ResourceHunterBonusScout.Windows;

/// <summary>Downloads updates from GitHub Releases. Velopack applies them on the next start.</summary>
static class Updater
{
    const string RepositoryUrl = "https://github.com/JayBazuzi/ResourceHunterBonusScout";
    static readonly TimeSpan CheckInterval = TimeSpan.FromHours(4);

    public static async Task RunAsync(IAppLog log, CancellationToken stopping)
    {
        var manager = new UpdateManager(new GithubSource(RepositoryUrl, null, false));
        // Not installed by Velopack, like when running from Visual Studio.
        if (!manager.IsInstalled)
            return;

        while (!stopping.IsCancellationRequested)
        {
            try
            {
                var update = await manager.CheckForUpdatesAsync();
                if (update is not null && manager.UpdatePendingRestart?.Version != update.TargetFullRelease.Version)
                {
                    log.Write($"Downloading update {update.TargetFullRelease.Version}");
                    await manager.DownloadUpdatesAsync(update, null, stopping);
                    log.Write("Update downloaded. It applies the next time the app starts.");
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.Write($"Update check failed: {e.Message}");
            }

            try
            {
                await Task.Delay(CheckInterval, stopping);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
