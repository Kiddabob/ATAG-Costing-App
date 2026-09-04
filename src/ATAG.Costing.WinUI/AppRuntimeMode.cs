using ATAG.Costing.Application.Preferences;

namespace ATAG.Costing.WinUI;

internal enum AppSessionMode
{
    Automatic,
    Organisation,
    BlankReview,
}

internal static class AppRuntimeMode
{
    public const string AppIconRelativePath = @"Assets\AppIcon.ico";
    public const string OrganisationLongLogoDarkTextRelativePath =
        @"Assets\Organisation\ATAGDesignLongLogoDarkText.png";
    public const string OrganisationLongLogoLightTextRelativePath =
        @"Assets\Organisation\ATAGDesignLongLogoLightText.png";

    private static AppSessionMode _sessionMode = AppSessionMode.Automatic;
    private static bool? _hasOrganisationAccount;
    private static string? _applicationDataRoot;

    public static bool IsPublicReview
    {
        get
        {
#if ATAG_PUBLIC_REVIEW
            return true;
#else
            return _sessionMode == AppSessionMode.BlankReview;
#endif
        }
    }

    public static bool HasDetectedOrganisationAccount =>
        _hasOrganisationAccount ??=
            LocalBrandingService.HasOrganisationOneDriveAccount();

    public static bool UsesOrganisationSharedData =>
        !IsPublicReview && HasDetectedOrganisationAccount;

    public static string ApplicationDataRoot =>
        _applicationDataRoot ?? throw new InvalidOperationException(
            "The application-data location has not been configured.");

    public static string CentralDataPath =>
        ApplicationDataLocationPolicy.GetDataFilePath(
            ApplicationDataRoot,
            ApplicationDataLocationPolicy.CentralDataFileName);

    public static string ProductionSpeedLibraryPath =>
        ApplicationDataLocationPolicy.GetDataFilePath(
            ApplicationDataRoot,
            ApplicationDataLocationPolicy.ProductionSpeedLibraryFileName);

    public static bool ShouldOfferLaunchModeChoice =>
        !IsPublicReview &&
        !SkipLaunchModeChoiceForDevelopment &&
        LocalLaunchModeChoiceService.IsEnabledForCurrentWindowsUser();

    private static bool SkipLaunchModeChoiceForDevelopment
    {
        get
        {
#if DEBUG
            return string.Equals(
                Environment.GetEnvironmentVariable(
                    "ATAG_COSTING_SKIP_LAUNCH_MODE_CHOICE"),
                "1",
                StringComparison.Ordinal);
#else
            return false;
#endif
        }
    }

    public static bool IsOrganisationBranded =>
        !IsPublicReview &&
        (_sessionMode == AppSessionMode.Organisation ||
         HasDetectedOrganisationAccount);

    public static string ProductName => IsOrganisationBranded
        ? "ATAG Costing App"
        : "Costing App";

    public static string QuotationPrefix => IsOrganisationBranded
        ? "ATAG"
        : "COST";

    public static string QuotationIssuerName => IsOrganisationBranded
        ? "ATAG Design Ltd"
        : "Costing App";

    public static IReadOnlyList<string> QuotationIssuerAddressLines =>
        IsOrganisationBranded
            ?
            [
                "Unit 18, Longfield Road",
                "South Church Enterprise Park",
                "Bishop Auckland, DL14 6XB | 01325 314128",
            ]
            : [];

    public static void SelectSessionMode(AppSessionMode mode)
    {
#if ATAG_PUBLIC_REVIEW
        _sessionMode = AppSessionMode.BlankReview;
#else
        if (mode == AppSessionMode.Organisation &&
            !HasDetectedOrganisationAccount &&
            !LocalLaunchModeChoiceService.IsEnabledForCurrentWindowsUser())
        {
            throw new InvalidOperationException(
                "ATAG mode requires either a detected ATAG OneDrive business account or the current-user testing opt-in.");
        }

        _sessionMode = mode;
#endif
    }

    public static void ConfigureApplicationDataRoot(string? userSelectedRoot)
    {
        _applicationDataRoot = ApplicationDataLocationPolicy.ResolveRoot(
            UsesOrganisationSharedData,
            userSelectedRoot);
    }
}
