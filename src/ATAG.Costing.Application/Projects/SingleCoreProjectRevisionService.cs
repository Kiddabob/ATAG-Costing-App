namespace ATAG.Costing.Application.Projects;

/// <summary>
/// Identity and lifecycle rules for a single-core project. The service contains
/// no filesystem or UI concerns.
/// </summary>
public sealed class SingleCoreProjectRevisionService
{
    public SingleCoreProjectDocument CreateWorkingCopy(
        SingleCoreProjectDocument document,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(document);

        var normalized = document.Upgrade();
        if (normalized.RevisionState == CostingRevisionState.ApprovedRevision)
        {
            throw new InvalidOperationException(
                "An approved revision cannot be changed. Create the next working revision first.");
        }

        return normalized with
        {
            SchemaVersion = SingleCoreProjectDocument.CurrentSchemaVersion,
            SavedAt = now,
            UpdatedAtUtc = now.ToUniversalTime(),
        };
    }

    public SingleCoreProjectDocument Approve(
        SingleCoreProjectDocument document,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(document);

        var normalized = document.Upgrade();
        if (normalized.RevisionState == CostingRevisionState.ApprovedRevision)
        {
            throw new InvalidOperationException(
                "This revision is already approved.");
        }

        if (normalized.CalculatedResult is null ||
            normalized.CalculatedResult.Trace.Count == 0)
        {
            throw new InvalidOperationException(
                "A valid calculated result and trace are required before approval.");
        }

        return normalized with
        {
            SchemaVersion = SingleCoreProjectDocument.CurrentSchemaVersion,
            RevisionState = CostingRevisionState.ApprovedRevision,
            ApprovedAtUtc = now.ToUniversalTime(),
            SavedAt = now,
            UpdatedAtUtc = now.ToUniversalTime(),
        };
    }

    public SingleCoreProjectDocument CreateNextRevision(
        SingleCoreProjectDocument approvedRevision,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(approvedRevision);

        var normalized = approvedRevision.Upgrade();
        if (normalized.RevisionState != CostingRevisionState.ApprovedRevision)
        {
            throw new InvalidOperationException(
                "Only an approved revision can be promoted to a new revision.");
        }

        return normalized with
        {
            RevisionId = Guid.NewGuid(),
            RevisionNumber = normalized.RevisionNumber + 1,
            RevisionState = CostingRevisionState.WorkingCopy,
            CreatedAtUtc = now.ToUniversalTime(),
            UpdatedAtUtc = now.ToUniversalTime(),
            ApprovedAtUtc = null,
            SavedAt = now,
        };
    }

    public SingleCoreProjectDocument Duplicate(
        SingleCoreProjectDocument source,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Upgrade() with
        {
            ProjectId = Guid.NewGuid(),
            RevisionId = Guid.NewGuid(),
            RevisionNumber = 1,
            RevisionState = CostingRevisionState.WorkingCopy,
            CreatedAtUtc = now.ToUniversalTime(),
            UpdatedAtUtc = now.ToUniversalTime(),
            ApprovedAtUtc = null,
            SavedAt = now,
        };
    }
}
