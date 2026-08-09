using System.Globalization;

namespace ATAG.Costing.Application.Updates;

public sealed record AppReleaseNotesEntry(
    string Version,
    string Notes,
    DateTimeOffset PublishedAt,
    bool IsPrerelease);

/// <summary>
/// Builds one update summary from every applicable release after the installed
/// version, rather than showing only the newest release's notes.
/// </summary>
public static class AppReleaseNotesComposer
{
    public static string Compose(
        string installedVersion,
        string targetVersion,
        AppUpdateChannel channel,
        IEnumerable<AppReleaseNotesEntry> releases,
        string targetFallbackNotes)
    {
        ArgumentNullException.ThrowIfNull(releases);

        if (!SemanticVersion.TryParse(installedVersion, out var installed) ||
            !SemanticVersion.TryParse(targetVersion, out var target))
        {
            return targetFallbackNotes;
        }

        var applicable = releases
            .Select(release => new
            {
                Release = release,
                Parsed = SemanticVersion.TryParse(
                    release.Version,
                    out var parsed)
                    ? parsed
                    : (SemanticVersion?)null,
            })
            .Where(item => item.Parsed.HasValue)
            .Where(item =>
                item.Parsed!.Value.CompareTo(installed) > 0 &&
                item.Parsed.Value.CompareTo(target) <= 0)
            .Where(item =>
                channel == AppUpdateChannel.Beta ||
                !item.Release.IsPrerelease)
            .OrderByDescending(item => item.Parsed!.Value)
            .Select(item => FormatRelease(item.Release))
            .ToArray();

        return applicable.Length == 0
            ? targetFallbackNotes
            : string.Join(
                Environment.NewLine + Environment.NewLine,
                applicable);
    }

    private static string FormatRelease(AppReleaseNotesEntry release)
    {
        var date = release.PublishedAt.ToLocalTime().ToString(
            "dd MMM yyyy",
            CultureInfo.GetCultureInfo("en-GB"));
        var notes = string.IsNullOrWhiteSpace(release.Notes)
            ? "No additional notes were supplied for this version."
            : release.Notes.Trim();
        return $"Version {NormalizeVersion(release.Version)} · {date}" +
               Environment.NewLine + notes;
    }

    private static string NormalizeVersion(string version) =>
        version.Trim().TrimStart('v', 'V');

    private readonly record struct SemanticVersion(
        int Major,
        int Minor,
        int Patch,
        IReadOnlyList<string> Prerelease) : IComparable<SemanticVersion>
    {
        public static bool TryParse(
            string? value,
            out SemanticVersion version)
        {
            version = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = NormalizeVersion(value);
            var withoutMetadata = normalized.Split('+', 2)[0];
            var parts = withoutMetadata.Split('-', 2);
            var numbers = parts[0].Split('.');
            var minor = 0;
            var patch = 0;
            if (numbers.Length is < 1 or > 3 ||
                !int.TryParse(numbers[0], out var major) ||
                (numbers.Length > 1 &&
                 !int.TryParse(numbers[1], out minor)) ||
                (numbers.Length > 2 &&
                 !int.TryParse(numbers[2], out patch)))
            {
                return false;
            }

            var prerelease = parts.Length == 2
                ? parts[1].Split('.', StringSplitOptions.RemoveEmptyEntries)
                : [];
            version = new SemanticVersion(
                major,
                numbers.Length > 1 ? minor : 0,
                numbers.Length > 2 ? patch : 0,
                prerelease);
            return true;
        }

        public int CompareTo(SemanticVersion other)
        {
            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;
            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;
            result = Patch.CompareTo(other.Patch);
            if (result != 0) return result;

            if (Prerelease.Count == 0 && other.Prerelease.Count == 0) return 0;
            if (Prerelease.Count == 0) return 1;
            if (other.Prerelease.Count == 0) return -1;

            var common = Math.Min(Prerelease.Count, other.Prerelease.Count);
            for (var index = 0; index < common; index++)
            {
                var left = Prerelease[index];
                var right = other.Prerelease[index];
                var leftIsNumber = int.TryParse(left, out var leftNumber);
                var rightIsNumber = int.TryParse(right, out var rightNumber);
                result = (leftIsNumber, rightIsNumber) switch
                {
                    (true, true) => leftNumber.CompareTo(rightNumber),
                    (true, false) => -1,
                    (false, true) => 1,
                    _ => string.Compare(left, right, StringComparison.Ordinal),
                };
                if (result != 0) return result;
            }

            return Prerelease.Count.CompareTo(other.Prerelease.Count);
        }
    }
}
