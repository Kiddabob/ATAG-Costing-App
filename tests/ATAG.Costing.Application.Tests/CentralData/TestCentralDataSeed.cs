using ATAG.Costing.Application.CentralData;

namespace ATAG.Costing.Infrastructure.CentralData;

/// <summary>
/// Small, fictional test fixture. Production builds intentionally contain no
/// customer, supplier or material rows.
/// </summary>
internal static class TestCentralDataSeed
{
    public static CentralDataState Create() => new(
        CentralDataSourceConfiguration.Unconfigured,
        new CentralDataSnapshot(
            SchemaVersion: 2,
            Revision: "synthetic-test-data",
            CapturedAt: DateTimeOffset.UnixEpoch,
            SourceLabel: "Synthetic test data",
            Copper:
            [
                new CopperReference(
                    "test-copper",
                    "7/0.20 TCW",
                    "Example conductor supplier",
                    10m,
                    500m,
                    0.6m),
            ],
            Compounds:
            [
                new CompoundReference(
                    "test-compound",
                    "Example PVC",
                    "Example compound supplier",
                    2m,
                    1.3m,
                    "PVC",
                    "Synthetic test compound"),
            ],
            Masterbatches:
            [
                new MasterbatchReference(
                    "TEST-RED",
                    "Example Red",
                    "Example colour supplier",
                    12m,
                    "PVC",
                    "#CC3344"),
            ],
            Contacts:
            [
                new ContactReference(
                    "test-contact",
                    "Example Customer Ltd",
                    "Example",
                    "1 Test Way",
                    "",
                    "",
                    "",
                    "TE1 1ST",
                    "",
                    "",
                    "",
                    "",
                    false,
                    true,
                    false,
                    false,
                    false,
                    false,
                    false),
            ],
            Operators:
            [
                new OperatorReference(
                    "test-operator",
                    "Operator",
                    "",
                    "Example",
                    "EO",
                    false,
                    false,
                    true,
                    false,
                    false,
                    false,
                    true),
            ]));
}
