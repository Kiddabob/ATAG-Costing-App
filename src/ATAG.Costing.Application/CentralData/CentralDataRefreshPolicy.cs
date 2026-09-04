namespace ATAG.Costing.Application.CentralData;

/// <summary>
/// Defines the non-interactive cadence for refreshing configured central-data
/// links. Manual refresh remains available independently of this policy.
/// </summary>
public static class CentralDataRefreshPolicy
{
    public static TimeSpan AutomaticRefreshInterval { get; } =
        TimeSpan.FromHours(1);
}
