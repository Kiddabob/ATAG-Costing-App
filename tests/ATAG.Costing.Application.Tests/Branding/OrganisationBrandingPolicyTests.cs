using ATAG.Costing.Application.Branding;
using Xunit;

namespace ATAG.Costing.Application.Tests.Branding;

public sealed class OrganisationBrandingPolicyTests
{
    [Theory]
    [InlineData("Business1", "person@atagcables.com", null)]
    [InlineData("business2", " PERSON@ATAGCABLES.COM ", null)]
    [InlineData("Business3", null, "legacy@atagcables.com")]
    public void ShouldUseAtagBranding_AcceptsAtagBusinessAccounts(
        string accountName,
        string? userEmail,
        string? legacyEmail)
    {
        var accounts = new[]
        {
            new OneDriveAccountRegistration(
                accountName,
                userEmail,
                legacyEmail),
        };

        Assert.True(
            OrganisationBrandingPolicy.ShouldUseAtagBranding(accounts));
    }

    [Theory]
    [InlineData("Personal", "person@atagcables.com")]
    [InlineData("Business1", "person@example.com")]
    [InlineData("Business1", "person@atagcables.com.example")]
    [InlineData("Business1", "")]
    public void ShouldUseAtagBranding_RejectsNonMatchingRegistrations(
        string accountName,
        string? userEmail)
    {
        var accounts = new[]
        {
            new OneDriveAccountRegistration(accountName, userEmail, null),
        };

        Assert.False(
            OrganisationBrandingPolicy.ShouldUseAtagBranding(accounts));
    }

    [Fact]
    public void ShouldUseAtagBranding_FindsMatchAmongMultipleAccounts()
    {
        var accounts = new[]
        {
            new OneDriveAccountRegistration(
                "Personal",
                "personal@example.com",
                null),
            new OneDriveAccountRegistration(
                "Business1",
                "work@example.com",
                null),
            new OneDriveAccountRegistration(
                "Business2",
                "person@atagcables.com",
                null),
        };

        Assert.True(
            OrganisationBrandingPolicy.ShouldUseAtagBranding(accounts));
    }
}
