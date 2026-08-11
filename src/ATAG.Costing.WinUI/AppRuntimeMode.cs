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

    public static bool ShouldOfferLaunchModeChoice =>
        !IsPublicReview &&
        LocalLaunchModeChoiceService.IsEnabledForCurrentWindowsUser();

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
}
