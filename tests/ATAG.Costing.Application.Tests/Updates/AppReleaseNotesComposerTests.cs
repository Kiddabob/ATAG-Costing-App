using ATAG.Costing.Application.Updates;
using Xunit;

namespace ATAG.Costing.Application.Tests.Updates;

public sealed class AppReleaseNotesComposerTests
{
    [Fact]
    public void Compose_IncludesEveryReleaseAfterInstalledVersion()
    {
        var notes = AppReleaseNotesComposer.Compose(
            "1.0.0",
            "1.3.0",
            AppUpdateChannel.Stable,
            [
                Release("v1.0.0", "Already installed."),
                Release("v1.1.0", "First change."),
                Release("v1.2.0", "Second change."),
                Release("v1.3.0", "Newest change."),
                Release("v2.0.0", "Future change."),
            ],
            "Fallback.");

        Assert.DoesNotContain("Already installed", notes);
        Assert.Contains("Version 1.1.0", notes);
        Assert.Contains("First change", notes);
        Assert.Contains("Version 1.2.0", notes);
        Assert.Contains("Second change", notes);
        Assert.Contains("Version 1.3.0", notes);
        Assert.Contains("Newest change", notes);
        Assert.DoesNotContain("Future change", notes);
        Assert.True(
            notes.IndexOf("Version 1.3.0", StringComparison.Ordinal) <
            notes.IndexOf("Version 1.2.0", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_StableExcludesPrereleases_BetaIncludesThem()
    {
        AppReleaseNotesEntry[] releases =
        [
            Release("1.1.0-beta.1", "Beta change.", prerelease: true),
            Release("1.1.0", "Stable change."),
        ];

        var stable = AppReleaseNotesComposer.Compose(
            "1.0.0",
            "1.1.0",
            AppUpdateChannel.Stable,
            releases,
            "Fallback.");
        var beta = AppReleaseNotesComposer.Compose(
            "1.0.0",
            "1.1.0",
            AppUpdateChannel.Beta,
            releases,
            "Fallback.");

        Assert.DoesNotContain("Beta change", stable);
        Assert.Contains("Stable change", stable);
        Assert.Contains("Beta change", beta);
        Assert.Contains("Stable change", beta);
    }

    [Fact]
    public void Compose_WhenFeedCannotBeMatched_UsesTargetPackageNotes()
    {
        var notes = AppReleaseNotesComposer.Compose(
            "development build",
            "1.0.0",
            AppUpdateChannel.Stable,
            [],
            "Target package notes.");

        Assert.Equal("Target package notes.", notes);
    }

    private static AppReleaseNotesEntry Release(
        string version,
        string notes,
        bool prerelease = false) =>
        new(
            version,
            notes,
            new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero),
            prerelease);
}
