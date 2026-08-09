using ATAG.Costing.Application.CentralData;

namespace ATAG.Costing.Infrastructure.CentralData;

/// <summary>
/// Creates the source-controlled first-run state. Business database rows are
/// deliberately absent: an installed user imports them through the guided
/// Access/SQL setup and successful imports are retained only in LocalAppData.
/// </summary>
public static class InitialCentralDataState
{
    public static CentralDataState Create() =>
        new(
            CentralDataSourceConfiguration.Unconfigured,
            new CentralDataSnapshot(
                SchemaVersion: 2,
                Revision: "unconfigured",
                CapturedAt: DateTimeOffset.UnixEpoch,
                SourceLabel: "No central data linked",
                Copper: [],
                Compounds: [],
                Masterbatches: [],
                Contacts: [],
                Operators: []));
}
