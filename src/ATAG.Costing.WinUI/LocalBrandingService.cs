using System.Security;
using ATAG.Costing.Application.Branding;
using Microsoft.Win32;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Applies organisation branding only from the current Windows user's local
/// OneDrive account registration. The account value is never retained,
/// displayed, logged, or sent anywhere.
/// </summary>
internal static class LocalBrandingService
{
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

            var registrations = new List<OneDriveAccountRegistration>();
            foreach (var accountName in accounts.GetSubKeyNames())
            {
                using var account = accounts.OpenSubKey(
                    accountName,
                    writable: false);
                registrations.Add(new OneDriveAccountRegistration(
                    accountName,
                    account?.GetValue("UserEmail") as string,
                    account?.GetValue("Email") as string));
            }

            return OrganisationBrandingPolicy.ShouldUseAtagBranding(
                registrations);
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
    }
}
