using ATAG.Costing.Application.CentralData;
using Xunit;

namespace ATAG.Costing.Application.Tests.CentralData;

public sealed class CentralDataRefreshPolicyTests
{
    [Fact]
    public void AutomaticRefresh_UsesTheApprovedHourlyCadence()
    {
        Assert.Equal(
            TimeSpan.FromHours(1),
            CentralDataRefreshPolicy.AutomaticRefreshInterval);
    }
}
