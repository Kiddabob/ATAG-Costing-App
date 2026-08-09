namespace ATAG.Costing.Reporting.Templates;

/// <summary>
/// Metadata for a versioned printable output. Report layout is intentionally
/// separated from calculation and screen layout.
/// </summary>
public sealed record ReportTemplateDescriptor(
    string Id,
    string Name,
    int Version,
    string OutputKind);
