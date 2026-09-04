using ATAG.Costing.Application.Preferences;
using Xunit;

namespace ATAG.Costing.Application.Tests.Preferences;

public sealed class ApplicationDataLocationPolicyTests
{
    [Fact]
    public void ResolveRoot_DetectedOrganisation_AlwaysUsesManagedShare()
    {
        var resolved = ApplicationDataLocationPolicy.ResolveRoot(
            hasDetectedOrganisationAccount: true,
            userSelectedRoot: @"C:\Ignored");

        Assert.Equal(
            @"\\atagdesign\database\ATAG Costing App",
            resolved);
    }

    [Fact]
    public void ResolveRoot_GenericApp_UsesSelectedLocation()
    {
        var selected = Path.Combine(Path.GetTempPath(), "Costing App Data");

        var resolved = ApplicationDataLocationPolicy.ResolveRoot(
            hasDetectedOrganisationAccount: false,
            selected);

        Assert.Equal(Path.GetFullPath(selected), resolved);
    }

    [Fact]
    public void PublicReview_NeverRequiresApplicationDataSelection()
    {
        Assert.False(ApplicationDataLocationPolicy.RequiresGenericSelection(
            isPublicReview: true,
            hasDetectedOrganisationAccount: false,
            userSelectedRoot: null));
    }
}
