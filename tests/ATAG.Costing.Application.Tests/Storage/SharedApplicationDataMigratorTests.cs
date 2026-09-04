using ATAG.Costing.Infrastructure.Storage;
using Xunit;

namespace ATAG.Costing.Application.Tests.Storage;

public sealed class SharedApplicationDataMigratorTests
{
    [Fact]
    public void CopyMissingFiles_CopiesOnceWithoutOverwritingSharedData()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "ATAG-Costing-Tests",
            Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        try
        {
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "data.json"), "legacy");

            var first = SharedApplicationDataMigrator.CopyMissingFiles(
                source,
                destination,
                ["data.json"]);
            File.WriteAllText(Path.Combine(source, "data.json"), "changed");
            var second = SharedApplicationDataMigrator.CopyMissingFiles(
                source,
                destination,
                ["data.json"]);

            Assert.Equal(["data.json"], first);
            Assert.Empty(second);
            Assert.Equal(
                "legacy",
                File.ReadAllText(Path.Combine(destination, "data.json")));
            Assert.Equal("changed", File.ReadAllText(Path.Combine(source, "data.json")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
