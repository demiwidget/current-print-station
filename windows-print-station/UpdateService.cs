using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace CurrentRmsPrintStation;

public sealed class UpdateService
{
    private const string RepositoryUrl = "https://github.com/demiwidget/current-print-station";

    private readonly UpdateManager _manager = new(new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));

    public string CurrentVersionText =>
        _manager.CurrentVersion?.ToString() ??
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ??
        "0.0.0";

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (!_manager.IsInstalled)
        {
            return UpdateCheckResult.NotInstalled(CurrentVersionText);
        }

        if (_manager.UpdatePendingRestart is { } pendingRestart)
        {
            return UpdateCheckResult.FromPendingRestart(pendingRestart);
        }

        try
        {
            var update = await _manager.CheckForUpdatesAsync();
            return update is null
                ? UpdateCheckResult.UpToDate(CurrentVersionText)
                : UpdateCheckResult.Available(update);
        }
        catch (Exception ex)
        {
            return UpdateCheckResult.Error($"Update check failed: {ex.Message}");
        }
    }

    public async Task DownloadAndRestartAsync(UpdateInfo update, Action<int> progress, CancellationToken cancellationToken = default)
    {
        await _manager.DownloadUpdatesAsync(update, progress, cancellationToken);
        _manager.ApplyUpdatesAndRestart(update);
    }

    public void RestartToApply(VelopackAsset pendingRestart)
    {
        _manager.ApplyUpdatesAndRestart(pendingRestart);
    }
}

public enum UpdateCheckState
{
    NotInstalled,
    UpToDate,
    Available,
    PendingRestart,
    Error
}

public sealed record UpdateCheckResult(
    UpdateCheckState State,
    string Message,
    UpdateInfo? Update,
    VelopackAsset? PendingRestart)
{
    public static UpdateCheckResult NotInstalled(string currentVersion) =>
        new(
            UpdateCheckState.NotInstalled,
            $"Running v{currentVersion}. Updates are enabled after installing a packaged release.",
            null,
            null);

    public static UpdateCheckResult UpToDate(string currentVersion) =>
        new(UpdateCheckState.UpToDate, $"Up to date: v{currentVersion}.", null, null);

    public static UpdateCheckResult Available(UpdateInfo update) =>
        new(
            UpdateCheckState.Available,
            $"Update available: v{update.TargetFullRelease.Version}.",
            update,
            null);

    public static UpdateCheckResult FromPendingRestart(VelopackAsset pendingRestart) =>
        new(
            UpdateCheckState.PendingRestart,
            $"Update v{pendingRestart.Version} is ready. Restart the app to finish.",
            null,
            pendingRestart);

    public static UpdateCheckResult Error(string message) =>
        new(UpdateCheckState.Error, message, null, null);
}
