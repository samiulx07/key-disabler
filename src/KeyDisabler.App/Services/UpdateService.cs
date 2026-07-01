using System.Diagnostics;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace KeyDisabler.App.Services;

public sealed class UpdateService
{
    private const string GitHubRepoUrl = "https://github.com/SamiulxHasanx07/key-disabler";
    private readonly UpdateManager _updateManager;
    private UpdateInfo? _cachedUpdate;

    public UpdateService()
    {
        var source = new GithubSource(GitHubRepoUrl, prerelease: true, accessToken: null);
        _updateManager = new UpdateManager(source);
    }

    /// <summary>
    /// Returns the currently installed version string, or null if this is a dev/debug build.
    /// </summary>
    public string? CurrentVersion => _updateManager.CurrentVersion?.ToString();

    /// <summary>
    /// Checks for available updates in the background.
    /// Returns null if no update is available.
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            _cachedUpdate = await _updateManager.CheckForUpdatesAsync();
            return _cachedUpdate;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdateService] Check failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Downloads the pending update. Must only be called after a successful check.
    /// </summary>
    public async Task DownloadUpdateAsync(Action<int>? progress = null)
    {
        if (_cachedUpdate is null)
        {
            throw new InvalidOperationException("Call CheckForUpdatesAsync first.");
        }

        await _updateManager.DownloadUpdatesAsync(_cachedUpdate, progress);
    }

    /// <summary>
    /// Applies the downloaded update and restarts the application.
    /// Must only be called after a successful download.
    /// </summary>
    public void ApplyUpdateAndRestart(string[]? arguments = null)
    {
        _updateManager.ApplyUpdatesAndRestart(_cachedUpdate!, arguments);
    }

    /// <summary>
    /// Returns a user-friendly string describing what's new.
    /// </summary>
    public static string FormatUpdateInfo(UpdateInfo info)
    {
        var version = info.TargetFullRelease.Version;
        return $"""
                Version {version} is available!

                Would you like to download and install it now?
                """;
    }
}
