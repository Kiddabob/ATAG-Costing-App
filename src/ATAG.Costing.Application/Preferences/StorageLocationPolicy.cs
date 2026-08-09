namespace ATAG.Costing.Application.Preferences;

public static class StorageLocationPolicy
{
    public static bool IsAvailable(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            return Directory.Exists(path);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
}
