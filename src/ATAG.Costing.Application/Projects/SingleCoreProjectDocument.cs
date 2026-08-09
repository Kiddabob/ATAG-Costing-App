namespace ATAG.Costing.Application.Projects;

/// <summary>
/// Portable state for one single-core costing revision and its linked contract
/// review. Approved revisions include their calculated result and complete
/// trace so they can be reproduced without applying newer rules or reference
/// data.
/// </summary>
public sealed record SingleCoreProjectDocument
{
    public const int CurrentSchemaVersion = 2;
    public const int OldestSupportedSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public Guid ProjectId { get; init; }
    public Guid RevisionId { get; init; }
    public int RevisionNumber { get; init; } = 1;
    public CostingRevisionState RevisionState { get; init; } =
        CostingRevisionState.WorkingCopy;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public DateTimeOffset? ApprovedAtUtc { get; init; }
    public DateTimeOffset SavedAt { get; init; } = DateTimeOffset.UtcNow;
    public string CentralDataRevision { get; init; } = "";
    public string CopperId { get; init; } = "";
    public string CopperDescription { get; init; } = "";
    public string CopperSupplier { get; init; } = "";
    public string CompoundId { get; init; } = "";
    public string CompoundName { get; init; } = "";
    public string CompoundSupplier { get; init; } = "";
    public string MasterbatchCode { get; init; } = "";
    public string MasterbatchName { get; init; } = "";
    public string MasterbatchSupplier { get; init; } = "";
    public string? CustomerContactId { get; init; }
    public string? OperatorId { get; init; }
    public string ConductorMaterialType { get; init; } = "";
    public int ConductorSelectionModeIndex { get; init; }
    public double QuoteLengthMetres { get; init; }
    public double UsageAllowancePercent { get; init; }
    public double RiskPercent { get; init; }
    public double MarkupPercent { get; init; }
    public double TargetMarginPercent { get; init; }
    public double ConductorSupplierQuoteTotal { get; init; }
    public double ConductorSupplierQuotedKilograms { get; init; }
    public double ConductorYieldMetresPerKilogram { get; init; }
    public double ConductorOutsideDiameterMillimetres { get; init; }
    public double CompoundSupplierQuoteTotal { get; init; }
    public double CompoundSupplierQuotedKilograms { get; init; }
    public double CompoundSpecificGravity { get; init; }
    public double NominalFinishedCoreOutsideDiameterMillimetres { get; init; }
    public double FinishedCoreOutsideDiameterToleranceMillimetres { get; init; }
    public bool UseSeparateNegativeOutsideDiameterTolerance { get; init; }
    public double FinishedCoreNegativeOutsideDiameterToleranceMillimetres { get; init; }
    public double MasterbatchSupplierQuoteTotal { get; init; }
    public double MasterbatchSupplierQuotedKilograms { get; init; }
    public double MasterbatchAdditionPercent { get; init; }
    public bool HasCorePrint { get; init; }
    public string CorePrintText { get; init; } = "";
    public string CorePrintColourHex { get; init; } = "#FFFFFF";
    public double CorePrintHeightMillimetres { get; init; } = 0.6;
    public double CorePrintRepeatDistanceMillimetres { get; init; } = 250;
    public double CorePrintDotPitchHorizontalMillimetres { get; init; } = 0.25;
    public double CorePrintDotPitchVerticalMillimetres { get; init; } = 0.25;
    public bool UseManualLineSpeed { get; init; }
    public double ManualLineSpeedMetresPerHour { get; init; }
    public double ProductionSetupTimeHours { get; init; }
    public double ProductionOperatorCount { get; init; }
    public double HourlyLabourRate { get; init; }
    public string CustomerName { get; init; } = "";
    public string CustomerShortName { get; init; } = "";
    public bool IsCustomerSpecial { get; init; }
    public bool UseCustomCoreName { get; init; }
    public string CustomCoreName { get; init; } = "";
    public string DeliveryAddress { get; init; } = "";
    public string ScopeOfWork { get; init; } = "";
    public string CustomerSuppliedMaterials { get; init; } = "";
    public string SpecialRequirements { get; init; } = "";
    public string RisksAndOpportunities { get; init; } = "";
    public bool PurchaseOrderMatchesQuote { get; init; }
    public bool AdditionalRisksAtAcceptance { get; init; }
    public int OrderDecisionIndex { get; init; }
    public string ReviewApprovedBy { get; init; } = "";
    public DateTimeOffset ReviewDate { get; init; } = DateTimeOffset.Now;
    public DateTimeOffset AcknowledgementDate { get; init; } = DateTimeOffset.Now;
    public string ReviewNotes { get; init; } = "";
    public int AmendmentDecisionIndex { get; init; }
    public string AmendmentConcerns { get; init; } = "";
    public string QuoteNumber { get; init; } = "";
    public string QuoteCurrencyCode { get; init; } = "GBP";
    public double QuoteReelCount { get; init; } = 10;
    public double QuoteMetresPerReel { get; init; } = 500;
    public int QuoteConductorDisplayModeIndex { get; init; }
    public bool UseExactCustomerColourName { get; init; }
    public string QuoteDescription { get; init; } = "";
    public string QuotePackaging { get; init; } = "Reels";
    public string QuoteEstimatedDelivery { get; init; } = "To be confirmed";
    public string QuoteSpecialNotes { get; init; } = "";
    public string QuoteTermsAndConditions { get; init; } = "";
    public IReadOnlyList<string> RuleVersions { get; init; } = [];
    public SingleCoreCalculatedResultSnapshot? CalculatedResult { get; init; }

    public bool IsSupportedSchema =>
        SchemaVersion is >= OldestSupportedSchemaVersion and
            <= CurrentSchemaVersion;

    /// <summary>
    /// Supplies identity and timestamps to legacy schema-v1 documents without
    /// changing their user-entered values. The upgraded identity is persisted
    /// on the next save.
    /// </summary>
    public SingleCoreProjectDocument Upgrade()
    {
        if (!IsSupportedSchema)
        {
            return this;
        }

        var timestamp = SavedAt == default
            ? DateTimeOffset.UtcNow
            : SavedAt.ToUniversalTime();
        return this with
        {
            SchemaVersion = CurrentSchemaVersion,
            ProjectId = ProjectId == Guid.Empty ? Guid.NewGuid() : ProjectId,
            RevisionId = RevisionId == Guid.Empty ? Guid.NewGuid() : RevisionId,
            RevisionNumber = Math.Max(1, RevisionNumber),
            CreatedAtUtc = CreatedAtUtc == default
                ? timestamp
                : CreatedAtUtc.ToUniversalTime(),
            UpdatedAtUtc = UpdatedAtUtc == default
                ? timestamp
                : UpdatedAtUtc.ToUniversalTime(),
        };
    }
}

public enum CostingRevisionState
{
    WorkingCopy = 0,
    ApprovedRevision = 1,
}
