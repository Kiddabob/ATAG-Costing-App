using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using ATAG.Costing.Application.Updates;
using Velopack;
using Velopack.Sources;

namespace ATAG.Costing.WinUI;

internal sealed class VelopackAppUpdateService : IAppUpdateService
{
    private const string RepositoryUrl =
        "https://github.com/Kiddabob/ATAG-Costing-App";
    private const string ReleasesApiUrl =
        "https://api.github.com/repos/Kiddabob/ATAG-Costing-App/releases?per_page=100";
    private static readonly HttpClient ReleaseNotesClient =
        CreateReleaseNotesClient();

    private UpdateManager? _manager;
    private UpdateInfo? _pendingUpdate;

    public string CurrentVersion
    {
        get
        {
            var installedVersion = CreateManager(AppUpdateChannel.Stable).CurrentVersion;
            if (installedVersion is not null)
            {
                return installedVersion.ToString();
            }

            var informationalVersion = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            return string.IsNullOrWhiteSpace(informationalVersion)
                ? "development build"
                : informationalVersion.Split('+', 2)[0];
        }
    }

    public bool IsInstalled =>
        CreateManager(AppUpdateChannel.Stable).IsInstalled;

    public async Task<AppUpdateRelease?> CheckForUpdatesAsync(
        AppUpdateChannel channel,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _manager = CreateManager(channel);
        if (!_manager.IsInstalled)
        {
            _pendingUpdate = null;
            return null;
        }

        _pendingUpdate = await _manager.CheckForUpdatesAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (_pendingUpdate is null)
        {
            return null;
        }

        var release = _pendingUpdate.TargetFullRelease;
        var releaseNotes = await GetCumulativeReleaseNotesAsync(
            CurrentVersion,
            release.Version.ToString(),
            channel,
            release.NotesMarkdown ?? string.Empty,
            cancellationToken);
        return new AppUpdateRelease(
            release.Version.ToString(),
            releaseNotes,
            release.Size,
            release.SHA256 ?? string.Empty);
    }

    public async Task DownloadUpdateAsync(
        IProgress<int> progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(progress);
        if (_manager is null || _pendingUpdate is null)
        {
            throw new InvalidOperationException(
                "Check for an update before downloading it.");
        }

        await _manager.DownloadUpdatesAsync(
            _pendingUpdate,
            value => progress.Report(value),
            cancellationToken);
    }

    public void ApplyUpdateAndRestart()
    {
        if (_manager is null || _pendingUpdate is null)
        {
            throw new InvalidOperationException(
                "No downloaded update is ready to install.");
        }

        _manager.ApplyUpdatesAndRestart(_pendingUpdate);
    }

    private static UpdateManager CreateManager(AppUpdateChannel channel) =>
        new(
            new GithubSource(
                RepositoryUrl,
                accessToken: null,
                prerelease: channel == AppUpdateChannel.Beta));

    private static HttpClient CreateReleaseNotesClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Costing-App-Update-Notes/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd(
            "application/vnd.github+json");
        return client;
    }

    private static async Task<string> GetCumulativeReleaseNotesAsync(
        string installedVersion,
        string targetVersion,
        AppUpdateChannel channel,
        string targetFallbackNotes,
        CancellationToken cancellationToken)
    {
        try
        {
            var releases = await ReleaseNotesClient.GetFromJsonAsync<
                GithubReleaseDto[]>(
                ReleasesApiUrl,
                cancellationToken) ?? [];
            return AppReleaseNotesComposer.Compose(
                installedVersion,
                targetVersion,
                channel,
                releases
                    .Where(release => !release.IsDraft)
                    .Select(release => new AppReleaseNotesEntry(
                        release.TagName,
                        release.Body ?? string.Empty,
                        release.PublishedAt,
                        release.IsPrerelease)),
                targetFallbackNotes);
        }
        catch (Exception exception) when (
            (exception is HttpRequestException or
                NotSupportedException or
                System.Text.Json.JsonException) ||
            (exception is OperationCanceledException &&
             !cancellationToken.IsCancellationRequested))
        {
            Program.Log(
                $"Cumulative release-note lookup failed; using target notes: {exception.Message}");
            return targetFallbackNotes;
        }
    }

    private sealed class GithubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; init; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset PublishedAt { get; init; }

        [JsonPropertyName("prerelease")]
        public bool IsPrerelease { get; init; }

        [JsonPropertyName("draft")]
        public bool IsDraft { get; init; }
    }
}
