namespace ATAG.Costing.Application.Preferences;

/// <summary>
/// Keeps shared application data separate from user-selected costing documents
/// and from machine-specific preferences such as theme and window placement.
/// </summary>
public static class ApplicationDataLocationPolicy
{
    public const string OrganisationSharedRoot =
        @"\\atagdesign\database\ATAG Costing App";

    public const string CentralDataFileName = "central-data-state.json";

    public const string ProductionSpeedLibraryFileName =
        "production-speed-library.json";

    public static string LocalDefaultRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ATAG Design Ltd",
        "ATAG Costing");

    public static string ResolveRoot(
        bool hasDetectedOrganisationAccount,
        string? userSelectedRoot) =>
        hasDetectedOrganisationAccount
            ? OrganisationSharedRoot
            : string.IsNullOrWhiteSpace(userSelectedRoot)
                ? LocalDefaultRoot
                : Path.GetFullPath(userSelectedRoot.Trim());

    public static bool RequiresGenericSelection(
        bool isPublicReview,
        bool hasDetectedOrganisationAccount,
        string? userSelectedRoot) =>
        !isPublicReview &&
        !hasDetectedOrganisationAccount &&
        !StorageLocationPolicy.IsAvailable(userSelectedRoot);

    public static string GetDataFilePath(string root, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return Path.Combine(root, fileName);
    }
}
