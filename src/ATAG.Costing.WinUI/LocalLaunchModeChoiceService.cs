using System.Security;
using Microsoft.Win32;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Reads a deliberately local, per-Windows-user test opt-in. No user name,
/// email address, SID, or identity hash is compiled into the application.
/// </summary>
internal static class LocalLaunchModeChoiceService
{
    internal const string RegistryKeyPath =
        @"Software\Costing App\Developer Options";
    internal const string RegistryValueName = "ShowLaunchModeChooser";

    public static bool IsEnabledForCurrentWindowsUser()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                RegistryKeyPath,
                writable: false);
            return key?.GetValue(RegistryValueName) switch
            {
                int value => value == 1,
                long value => value == 1L,
                string value => value.Equals(
                    "true",
                    StringComparison.OrdinalIgnoreCase) || value == "1",
                _ => false,
            };
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
