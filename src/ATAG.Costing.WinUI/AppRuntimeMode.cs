namespace ATAG.Costing.WinUI;

internal static class AppRuntimeMode
{
#if ATAG_PUBLIC_REVIEW
    public static bool IsPublicReview { get; } = true;
#else
    public static bool IsPublicReview { get; } = false;
#endif

    public static bool IsOrganisationBranded { get; } =
        LocalBrandingService.HasOrganisationOneDriveAccount();

    public static string ProductName { get; } = IsOrganisationBranded
        ? "ATAG Costing App"
        : "Costing App";

    public static string QuotationPrefix { get; } = IsOrganisationBranded
        ? "ATAG"
        : "COST";

#if ATAG_PUBLIC_REVIEW
    public const string QuotationIssuerName = "Costing App";

    public static IReadOnlyList<string> QuotationIssuerAddressLines { get; } = [];
#else
    public static string QuotationIssuerName { get; } = IsOrganisationBranded
        ? "ATAG Design Ltd"
        : "Costing App";

    public static IReadOnlyList<string> QuotationIssuerAddressLines { get; } =
        IsOrganisationBranded
            ?
            [
                "Unit 18, Longfield Road",
                "South Church Enterprise Park",
                "Bishop Auckland, DL14 6XB | 01325 314128",
            ]
            : [];
#endif
}
