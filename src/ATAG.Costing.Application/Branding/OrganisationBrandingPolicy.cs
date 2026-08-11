namespace ATAG.Costing.Application.Branding;

/// <summary>
/// A privacy-minimised view of a OneDrive account registration. It contains
/// only the fields needed to decide whether organisation branding may be used.
/// </summary>
public sealed record OneDriveAccountRegistration(
    string AccountName,
    string? UserEmail,
    string? LegacyEmail);

/// <summary>
/// Decides whether the ATAG organisation branding is appropriate without
/// retaining, displaying, logging, or transmitting an account address.
/// </summary>
public static class OrganisationBrandingPolicy
{
    private const string OrganisationDomain = "@atagcables.com";

    public static bool ShouldUseAtagBranding(
        IEnumerable<OneDriveAccountRegistration> accounts)
    {
        ArgumentNullException.ThrowIfNull(accounts);

        return accounts.Any(account =>
            account.AccountName.StartsWith(
                "Business",
                StringComparison.OrdinalIgnoreCase) &&
            (IsOrganisationEmail(account.UserEmail) ||
             IsOrganisationEmail(account.LegacyEmail)));
    }

    private static bool IsOrganisationEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) &&
        email.Trim().EndsWith(
            OrganisationDomain,
            StringComparison.OrdinalIgnoreCase);
}
