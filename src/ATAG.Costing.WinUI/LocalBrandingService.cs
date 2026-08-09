using System.Security;
using Microsoft.Win32;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Applies organisation branding only from the current Windows user's local
/// OneDrive account registration. The account value is never retained,
/// displayed, logged, or sent anywhere.
/// </summary>
internal static class LocalBrandingService
{
    private const string OrganisationDomain = "@atagcables.com";
    private const string OneDriveAccountsKey =
        @"Software\Microsoft\OneDrive\Accounts";

    public static bool HasOrganisationOneDriveAccount()
    {
        try
        {
            using var accounts = Registry.CurrentUser.OpenSubKey(
                OneDriveAccountsKey,
                writable: false);
            if (accounts is null)
            {
                return false;
            }

            foreach (var accountName in accounts.GetSubKeyNames())
            {
                if (!accountName.StartsWith(
                        "Business",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                using var account = accounts.OpenSubKey(
                    accountName,
                    writable: false);
                if (IsOrganisationEmail(account?.GetValue("UserEmail")) ||
                    IsOrganisationEmail(account?.GetValue("Email")))
                {
                    return true;
                }
            }
        }
        catch (IOException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        return false;
    }

    private static bool IsOrganisationEmail(object? value) =>
        value is string email &&
        email.Trim().EndsWith(
            OrganisationDomain,
            StringComparison.OrdinalIgnoreCase);
}
