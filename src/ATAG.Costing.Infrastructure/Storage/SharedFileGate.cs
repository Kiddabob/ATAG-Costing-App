namespace ATAG.Costing.Infrastructure.Storage;

internal sealed class SharedFileGate : IDisposable
{
    private const int RetryCount = 100;
    private const int RetryDelayMilliseconds = 50;
    private readonly FileStream _stream;

    private SharedFileGate(FileStream stream)
    {
        _stream = stream;
    }

    public static SharedFileGate Acquire(string targetPath)
    {
        var directory = Path.GetDirectoryName(targetPath)
            ?? throw new InvalidOperationException(
                "The shared data path has no parent directory.");
        Directory.CreateDirectory(directory);
        var lockPath = $"{targetPath}.lock";

        for (var attempt = 0; attempt < RetryCount; attempt++)
        {
            try
            {
                return new SharedFileGate(new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None));
            }
            catch (IOException) when (attempt < RetryCount - 1)
            {
                Thread.Sleep(RetryDelayMilliseconds);
            }
        }

        throw new TimeoutException(
            $"Timed out waiting for the shared data file '{targetPath}'.");
    }

    public void Dispose() => _stream.Dispose();
}
