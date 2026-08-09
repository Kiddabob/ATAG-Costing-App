namespace ATAG.Costing.Application.Updates;

public enum AppUpdateChannel
{
    Stable,
    Beta,
}

public sealed record AppUpdateRelease(
    string Version,
    string ReleaseNotes,
    long DownloadSizeBytes,
    string Sha256);

/// <summary>
/// Coordinates app-package updates without exposing the packaging framework to
/// the application or domain layers.
/// </summary>
public interface IAppUpdateService
{
    string CurrentVersion { get; }

    bool IsInstalled { get; }

    Task<AppUpdateRelease?> CheckForUpdatesAsync(
        AppUpdateChannel channel,
        CancellationToken cancellationToken = default);

    Task DownloadUpdateAsync(
        IProgress<int> progress,
        CancellationToken cancellationToken = default);

    void ApplyUpdateAndRestart();
}
