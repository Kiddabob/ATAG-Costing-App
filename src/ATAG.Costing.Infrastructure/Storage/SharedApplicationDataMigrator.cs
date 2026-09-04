namespace ATAG.Costing.Infrastructure.Storage;

/// <summary>
/// Copies legacy per-PC business data into a new shared root once. Existing
/// shared files always win, and legacy files remain as a recoverable backup.
/// </summary>
public static class SharedApplicationDataMigrator
{
    public static IReadOnlyList<string> CopyMissingFiles(
        string sourceRoot,
        string destinationRoot,
        IEnumerable<string> fileNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRoot);
        ArgumentNullException.ThrowIfNull(fileNames);

        if (string.Equals(
            Path.GetFullPath(sourceRoot),
            Path.GetFullPath(destinationRoot),
            StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        Directory.CreateDirectory(destinationRoot);
        var copied = new List<string>();
        foreach (var fileName in fileNames)
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                Path.GetFileName(fileName) != fileName)
            {
                throw new ArgumentException(
                    "Migration entries must be simple file names.",
                    nameof(fileNames));
            }

            var source = Path.Combine(sourceRoot, fileName);
            var destination = Path.Combine(destinationRoot, fileName);
            if (!File.Exists(source) || File.Exists(destination))
            {
                continue;
            }

            using var gate = SharedFileGate.Acquire(destination);
            if (File.Exists(destination))
            {
                continue;
            }

            var temporary = $"{destination}.{Guid.NewGuid():N}.migration.tmp";
            try
            {
                File.Copy(source, temporary, overwrite: false);
                File.Move(temporary, destination, overwrite: false);
                copied.Add(fileName);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        return copied;
    }
}
