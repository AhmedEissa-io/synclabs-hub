using SyncLabsHub.Core;
using Velopack;
using Velopack.Sources;

namespace SyncLabsHub.App.Services;

/// <summary>
/// Wraps Velopack update checks against a GitHub Releases feed.
/// All methods are safe no-ops when <see cref="SyncLabsConfig.UpdateFeedUrl"/> is empty.
/// Set UpdateFeedUrl to the GitHub repo URL: "https://github.com/owner/repo"
/// </summary>
public sealed class UpdateService
{
    private UpdateManager? _manager;
    private UpdateInfo? _pending;

    public string? AvailableVersion => _pending?.TargetFullRelease?.Version?.ToString();

    /// <summary>Returns the available version string, or null if none / not configured. Never throws.</summary>
    public async Task<string?> CheckAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SyncLabsConfig.UpdateFeedUrl))
                return null;

            // GithubSource queries the GitHub API for releases — no PAT needed for public repos.
            if (_manager is null)
            {
                var source  = new GithubSource(SyncLabsConfig.UpdateFeedUrl, accessToken: null, prerelease: false);
                _manager = new UpdateManager(source);
            }

            if (!_manager.IsInstalled)
                return null;

            _pending = await _manager.CheckForUpdatesAsync();
            return AvailableVersion;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Downloads + applies the pending update and restarts. No-op when nothing is pending.</summary>
    public async Task ApplyAsync()
    {
        if (_manager is null || _pending is null) return;
        await _manager.DownloadUpdatesAsync(_pending);
        _manager.ApplyUpdatesAndRestart(_pending);
    }
}
