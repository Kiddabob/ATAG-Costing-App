using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using ATAG.Costing.Application.CentralData;
using ATAG.Costing.Application.Currency;
using ATAG.Costing.Application.Projects;
using ATAG.Costing.Domain.Calculations;
using ATAG.Costing.Domain.Costing;
using ATAG.Costing.Domain.Materials;
using ATAG.Costing.Reporting.Quotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATAG.Costing.WinUI.ViewModels;

public sealed record CalculationStepRow(
    string Label,
    string BusinessMeaning,
    string Formula,
    string SubstitutedFormula,
    string Result,
    string RoundingRule,
    string RuleVersion,
    string? Warning);

public sealed record CalculationFlowNode(
    string Label,
    string BusinessMeaning,
    string Formula,
    string SubstitutedFormula,
    string Result,
    string InputSummary,
    string? Warning);

public sealed record CalculationFlowStage(
    string Header,
    string Description,
    string BackgroundHex,
    IReadOnlyList<CalculationFlowNode> Nodes,
    string ConnectorText);

public sealed record QuoteCurrencyOption(
    string Code,
    string Name,
    string Symbol)
{
    public string Display => $"{Code} - {Name}";
}

public sealed record MasterbatchCompatibilityRow(
    string MaterialFamily,
    bool IsCompatible,
    bool IsCompatibilityKnown,
    string MaximumTemperature)
{
    public string StatusLabel =>
        !IsCompatibilityKnown
            ? "Not recorded"
            : IsCompatible
                ? "Compatible"
                : "Not listed";
    public string BackgroundHex =>
        !IsCompatibilityKnown
            ? "#4A4029"
            : IsCompatible
                ? "#21402C"
                : "#482D30";
    public string TemperatureDisplay =>
        IsCompatibilityKnown && !IsCompatible
            ? string.Empty
            : string.IsNullOrWhiteSpace(MaximumTemperature)
            ? "Not recorded"
            : MaximumTemperature;
}

public enum CentralDataConnectionState
{
    Cached = 0,
    Checking = 1,
    Online = 2,
    Offline = 3,
}

public sealed record CentralDataLinkStatusRow(
    string AreaName,
    string Status,
    string Detail,
    string BackgroundHex);

internal enum CentralDataAreaConnectionState
{
    CachedOnly,
    ReadyToCheck,
    Checking,
    Live,
    Offline,
}

public partial class SingleCoreCostingViewModel : ObservableObject
{
    private static readonly CultureInfo PoundCulture =
        CultureInfo.GetCultureInfo("en-GB");
    private static readonly IReadOnlyList<QuoteCurrencyOption>
        PreferredQuoteCurrencies =
        [
            new("GBP", "Pound sterling", "£"),
            new("EUR", "Euro", "€"),
            new("USD", "US dollar", "$"),
            new("CAD", "Canadian dollar", "CA$"),
            new("AUD", "Australian dollar", "A$"),
            new("NZD", "New Zealand dollar", "NZ$"),
            new("CHF", "Swiss franc", "CHF "),
            new("SEK", "Swedish krona", "SEK "),
            new("NOK", "Norwegian krone", "NOK "),
            new("DKK", "Danish krone", "DKK "),
            new("JPY", "Japanese yen", "¥"),
        ];
    private static readonly CentralDataArea[] CentralDataAreas =
    [
        CentralDataArea.Copper,
        CentralDataArea.Compounds,
        CentralDataArea.Masterbatch,
        CentralDataArea.Contacts,
        CentralDataArea.Operators,
    ];

    private static class InitialWorkingValues
    {
#if ATAG_PUBLIC_REVIEW
        public static double QuoteLength => double.NaN;
        public static double UsageAllowancePercent => double.NaN;
        public static double RiskPercent => double.NaN;
        public static double MarkupPercent => double.NaN;
        public static double TargetMarginPercent => double.NaN;
        public static double ManualLineSpeed => double.NaN;
        public static double ProductionSetupTime => double.NaN;
        public static double ProductionOperatorCount => double.NaN;
        public static double HourlyLabourRate => double.NaN;
        public static double ConductorQuoteTotal => double.NaN;
        public static double ConductorQuotedKilograms => double.NaN;
        public static double ConductorYield => double.NaN;
        public static double ConductorOutsideDiameter => double.NaN;
        public static double CompoundQuoteTotal => double.NaN;
        public static double CompoundQuotedKilograms => double.NaN;
        public static double CompoundSpecificGravity => double.NaN;
        public static double FinishedCoreOutsideDiameter => double.NaN;
        public static double OutsideDiameterTolerance => double.NaN;
        public static double MasterbatchQuoteTotal => double.NaN;
        public static double MasterbatchQuotedKilograms => double.NaN;
        public static double MasterbatchAdditionPercent => double.NaN;
        public static double CorePrintHeight => double.NaN;
        public static double CorePrintRepeatDistance => double.NaN;
        public static double CorePrintDotPitch => double.NaN;
        public static double QuoteReelCount => double.NaN;
        public static double QuoteMetresPerReel => double.NaN;
        public static int QuoteConductorDisplayMode => -1;
        public static QuoteCurrencyOption? QuoteCurrency => null;
        public const string QuoteNumber = "";
        public const string CorePrintColour = "";
        public const string QuotePackaging = "";
        public const string QuoteEstimatedDelivery = "";
        public const string QuoteTermsAndConditions = "";
        public const string SupplierUnitPriceDisplay = "—";
        public const string FinishedCoreOutsideDiameterRangeDisplay = "—";
        public const string ConvertedQuoteDisplay = "—";
        public const string QuoteReelPlanDisplay = "—";
        public const string ExchangeRateStatus =
            "Currency rates are unavailable in this interface-only edition.";
#else
        public const double QuoteLength = 5000;
        public const double UsageAllowancePercent = 3;
        public const double RiskPercent = 0;
        public const double MarkupPercent = 45;
        public const double TargetMarginPercent = 45;
        public const double ManualLineSpeed = 13000;
        public const double ProductionSetupTime = 0;
        public const double ProductionOperatorCount = 1;
        public const double HourlyLabourRate = 35;
        public const double ConductorQuoteTotal = 10398.41;
        public const double ConductorQuotedKilograms = 1000;
        public const double ConductorYield = 0;
        public const double ConductorOutsideDiameter = 0;
        public const double CompoundQuoteTotal = 1.63;
        public const double CompoundQuotedKilograms = 1;
        public const double CompoundSpecificGravity = 0;
        public const double FinishedCoreOutsideDiameter = 1.2;
        public const double OutsideDiameterTolerance = 0.025;
        public const double MasterbatchQuoteTotal = 14.83;
        public const double MasterbatchQuotedKilograms = 1;
        public const double MasterbatchAdditionPercent = 1;
        public const double CorePrintHeight = 0.6;
        public const double CorePrintRepeatDistance = 250;
        public const double CorePrintDotPitch = 0.25;
        public const double QuoteReelCount = 10;
        public const double QuoteMetresPerReel = 500;
        public const int QuoteConductorDisplayMode = 0;
        public static QuoteCurrencyOption QuoteCurrency =>
            PreferredQuoteCurrencies[0];
        public static string QuoteNumber =>
            $"{AppRuntimeMode.QuotationPrefix}-{DateTimeOffset.Now:yyyyMMdd-HHmm}";
        public const string CorePrintColour = "#FFFFFF";
        public const string QuotePackaging = "Reels";
        public const string QuoteEstimatedDelivery = "To be confirmed";
        public const string QuoteTermsAndConditions =
            "This quotation is based on current material prices and is ex-works, " +
            "exclusive of duties, carriage and VAT unless stated. Cable lengths and " +
            "packaging remain subject to agreement. Price is valid for 14 days from " +
            "the quotation date and copper may be adjusted at contract.";
        public const string SupplierUnitPriceDisplay = "—";
        public const string FinishedCoreOutsideDiameterRangeDisplay =
            "1.175 mm to 1.225 mm";
        public const string ConvertedQuoteDisplay = "£0.00 GBP";
        public const string QuoteReelPlanDisplay =
            "10 reels × 500 m = 5,000 m";
        public const string ExchangeRateStatus =
            "GBP is the costing currency. Refresh rates to quote in another currency.";
#endif
    }

    private static readonly HashSet<string> PersistedInputPropertyNames =
    [
        nameof(SelectedCopper),
        nameof(SelectedCompound),
        nameof(SelectedMasterbatch),
        nameof(SelectedCustomerContact),
        nameof(SelectedOperator),
        nameof(SelectedConductorMaterialType),
        nameof(ConductorSelectionModeIndex),
        nameof(SelectedConductorSize),
        nameof(SelectedConductorClass),
        nameof(SelectedConductorSupplier),
        nameof(QuoteLengthMetres),
        nameof(UsageAllowancePercent),
        nameof(RiskPercent),
        nameof(MarkupPercent),
        nameof(TargetMarginPercent),
        nameof(ConductorSupplierQuoteTotal),
        nameof(ConductorSupplierQuotedKilograms),
        nameof(ConductorYieldMetresPerKilogram),
        nameof(ConductorOutsideDiameterMillimetres),
        nameof(CompoundSupplierQuoteTotal),
        nameof(CompoundSupplierQuotedKilograms),
        nameof(CompoundSpecificGravity),
        nameof(NominalFinishedCoreOutsideDiameterMillimetres),
        nameof(FinishedCoreOutsideDiameterToleranceMillimetres),
        nameof(UseSeparateNegativeOutsideDiameterTolerance),
        nameof(FinishedCoreNegativeOutsideDiameterToleranceMillimetres),
        nameof(MasterbatchSupplierQuoteTotal),
        nameof(MasterbatchSupplierQuotedKilograms),
        nameof(MasterbatchAdditionPercent),
        nameof(HasCorePrint),
        nameof(CorePrintText),
        nameof(CorePrintColourHex),
        nameof(CorePrintHeightMillimetres),
        nameof(CorePrintRepeatDistanceMillimetres),
        nameof(CorePrintDotPitchHorizontalMillimetres),
        nameof(CorePrintDotPitchVerticalMillimetres),
        nameof(UseManualLineSpeed),
        nameof(ManualLineSpeedMetresPerHour),
        nameof(ProductionSetupTimeHours),
        nameof(ProductionOperatorCount),
        nameof(HourlyLabourRate),
        nameof(CustomerName),
        nameof(CustomerShortName),
        nameof(IsCustomerSpecial),
        nameof(UseCustomCoreName),
        nameof(CustomCoreName),
        nameof(DeliveryAddress),
        nameof(ScopeOfWork),
        nameof(CustomerSuppliedMaterials),
        nameof(SpecialRequirements),
        nameof(RisksAndOpportunities),
        nameof(PurchaseOrderMatchesQuote),
        nameof(AdditionalRisksAtAcceptance),
        nameof(OrderDecisionIndex),
        nameof(ReviewApprovedBy),
        nameof(ReviewDate),
        nameof(AcknowledgementDate),
        nameof(ReviewNotes),
        nameof(AmendmentDecisionIndex),
        nameof(AmendmentConcerns),
        nameof(QuoteNumber),
        nameof(SelectedQuoteCurrency),
        nameof(QuoteReelCount),
        nameof(QuoteMetresPerReel),
        nameof(QuoteConductorDisplayModeIndex),
        nameof(UseExactCustomerColourName),
        nameof(QuoteDescription),
        nameof(QuotePackaging),
        nameof(QuoteEstimatedDelivery),
        nameof(QuoteSpecialNotes),
        nameof(QuoteTermsAndConditions),
    ];

    private readonly CentralDataService _centralDataService;
    private readonly IExchangeRateService? _exchangeRateService;
    private readonly SingleCoreProjectRevisionService _revisionService = new();
    private bool _isApplyingCentralData;
    private bool _isUpdatingConductorOptions;
    private bool _isUpdatingReelPlan;
    private bool _isUsingSavedMaterialValues;
    private bool _suppressDocumentTracking = true;
    private IReadOnlyList<CopperReference> _allCopperMaterials = [];
    private ExchangeRateSnapshot? _exchangeRateSnapshot;
    private SingleCoreCalculatedResultSnapshot? _currentCalculatedResult;
    private decimal _recommendedQuotePrice;
    private string _centralDataRevision = "";
    private readonly Dictionary<CentralDataArea, CentralDataAreaConnectionState>
        _centralDataAreaConnectionStates = [];

    public Guid CurrentProjectId { get; private set; } = Guid.NewGuid();

    public Guid CurrentRevisionId { get; private set; } = Guid.NewGuid();

    public DateTimeOffset CurrentRevisionCreatedAtUtc { get; private set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset CurrentRevisionUpdatedAtUtc { get; private set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? CurrentRevisionApprovedAtUtc { get; private set; }

    public string? CurrentDocumentPath { get; private set; }

    [ObservableProperty]
    public partial int CurrentRevisionNumber { get; set; } = 1;

    [ObservableProperty]
    public partial CostingRevisionState CurrentRevisionState { get; set; } =
        CostingRevisionState.WorkingCopy;

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; set; } = true;

    [ObservableProperty]
    public partial bool HasValidationErrors { get; set; }

    [ObservableProperty]
    public partial string RevisionStatusDisplay { get; set; } =
        "Working copy · revision 1";

    [ObservableProperty]
    public partial string SaveStateDisplay { get; set; } =
        "Unsaved changes";

    public IReadOnlyList<string> ConductorSelectionModes { get; } =
    [
        "Strand construction",
        "Nominal cross-section (mm²)",
        "American Wire Gauge (AWG)",
    ];

    [ObservableProperty]
    public partial IReadOnlyList<string> ConductorMaterialTypeOptions { get; set; } = [];

    [ObservableProperty]
    public partial string? SelectedConductorMaterialType { get; set; }

    [ObservableProperty]
    public partial bool IsConductorGeometrySelectionAvailable { get; set; } = true;

    [ObservableProperty]
    public partial IReadOnlyList<CopperReference> CopperMaterials { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CopperReference> CopperTableRows { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CentralDataRetainedTable> RetainedSourceTables { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CompoundReference> CompoundMaterials { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CompoundReference> CompoundTableRows { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<MasterbatchReference> MasterbatchMaterials { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<MasterbatchReference> MasterbatchTableRows { get; set; } = [];

    public IReadOnlyList<string> MasterbatchColourGroupOptions { get; } =
        MasterbatchColourSearch.GroupOptions;

    [ObservableProperty]
    public partial IReadOnlyList<string> MasterbatchColourTypeOptions { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<ContactReference> ContactMaterials { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<OperatorReference> OperatorMaterials { get; set; } = [];

    [ObservableProperty]
    public partial CopperReference? SelectedCopper { get; set; }

    [ObservableProperty]
    public partial CompoundReference? SelectedCompound { get; set; }

    [ObservableProperty]
    public partial MasterbatchReference? SelectedMasterbatch { get; set; }

    [ObservableProperty]
    public partial ContactReference? SelectedCustomerContact { get; set; }

    [ObservableProperty]
    public partial OperatorReference? SelectedOperator { get; set; }

    [ObservableProperty]
    public partial int ConductorSelectionModeIndex { get; set; }

    [ObservableProperty]
    public partial string ConductorSizeHeader { get; set; } = "Strand construction";

    [ObservableProperty]
    public partial IReadOnlyList<string> ConductorSizeOptions { get; set; } = [];

    [ObservableProperty]
    public partial string? SelectedConductorSize { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> ConductorClassOptions { get; set; } = [];

    [ObservableProperty]
    public partial string? SelectedConductorClass { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> ConductorSupplierOptions { get; set; } = [];

    [ObservableProperty]
    public partial string? SelectedConductorSupplier { get; set; }

    [ObservableProperty]
    public partial string ConductorConstructionDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string ConductorSupplierDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string ConductorNominalAreaDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string ConductorCalculatedAreaDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string ConductorAwgDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string ConductorClassDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string ConductorClassReasonDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string ConductorVerificationMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsConductorVerificationOpen { get; set; }

    [ObservableProperty]
    public partial string CompoundDetailsDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string MasterbatchDetailsDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string MasterbatchCompatibilityStatus { get; set; } =
        "Choose a colour and compound to check compatibility.";

    [ObservableProperty]
    public partial string MasterbatchCompatibilityNotes { get; set; } =
        "* Suitable for use in all PVC except plasticised PVC. " +
        "** Suitable for use in all PVC except tin-stabilised PVC.";

    public ObservableCollection<MasterbatchCompatibilityRow>
        MasterbatchCompatibilityRows { get; } = [];

    [ObservableProperty]
    public partial double QuoteLengthMetres { get; set; } =
        InitialWorkingValues.QuoteLength;

    [ObservableProperty]
    public partial double UsageAllowancePercent { get; set; } =
        InitialWorkingValues.UsageAllowancePercent;

    [ObservableProperty]
    public partial double RiskPercent { get; set; } =
        InitialWorkingValues.RiskPercent;

    [ObservableProperty]
    public partial double MarkupPercent { get; set; } =
        InitialWorkingValues.MarkupPercent;

    [ObservableProperty]
    public partial double TargetMarginPercent { get; set; } =
        InitialWorkingValues.TargetMarginPercent;

    [ObservableProperty]
    public partial bool UseManualLineSpeed { get; set; }

    [ObservableProperty]
    public partial double ManualLineSpeedMetresPerHour { get; set; } =
        InitialWorkingValues.ManualLineSpeed;

    [ObservableProperty]
    public partial double ProductionSetupTimeHours { get; set; } =
        InitialWorkingValues.ProductionSetupTime;

    [ObservableProperty]
    public partial double ProductionOperatorCount { get; set; } =
        InitialWorkingValues.ProductionOperatorCount;

    [ObservableProperty]
    public partial double HourlyLabourRate { get; set; } =
        InitialWorkingValues.HourlyLabourRate;

    [ObservableProperty]
    public partial double ConductorSupplierQuoteTotal { get; set; } =
        InitialWorkingValues.ConductorQuoteTotal;

    [ObservableProperty]
    public partial double ConductorSupplierQuotedKilograms { get; set; } =
        InitialWorkingValues.ConductorQuotedKilograms;

    [ObservableProperty]
    public partial string ConductorPricePerKilogramDisplay { get; set; } =
        InitialWorkingValues.SupplierUnitPriceDisplay;

    [ObservableProperty]
    public partial double ConductorYieldMetresPerKilogram { get; set; } =
        InitialWorkingValues.ConductorYield;

    [ObservableProperty]
    public partial double ConductorOutsideDiameterMillimetres { get; set; } =
        InitialWorkingValues.ConductorOutsideDiameter;

    [ObservableProperty]
    public partial double CompoundSupplierQuoteTotal { get; set; } =
        InitialWorkingValues.CompoundQuoteTotal;

    [ObservableProperty]
    public partial double CompoundSupplierQuotedKilograms { get; set; } =
        InitialWorkingValues.CompoundQuotedKilograms;

    [ObservableProperty]
    public partial string CompoundPricePerKilogramDisplay { get; set; } =
        InitialWorkingValues.SupplierUnitPriceDisplay;

    [ObservableProperty]
    public partial double CompoundSpecificGravity { get; set; } =
        InitialWorkingValues.CompoundSpecificGravity;

    [ObservableProperty]
    public partial double NominalFinishedCoreOutsideDiameterMillimetres { get; set; } =
        InitialWorkingValues.FinishedCoreOutsideDiameter;

    [ObservableProperty]
    public partial double FinishedCoreOutsideDiameterToleranceMillimetres { get; set; } =
        InitialWorkingValues.OutsideDiameterTolerance;

    [ObservableProperty]
    public partial bool UseSeparateNegativeOutsideDiameterTolerance { get; set; }

    [ObservableProperty]
    public partial double FinishedCoreNegativeOutsideDiameterToleranceMillimetres { get; set; } =
        InitialWorkingValues.OutsideDiameterTolerance;

    [ObservableProperty]
    public partial string FinishedCoreOutsideDiameterRangeDisplay { get; set; } =
        InitialWorkingValues.FinishedCoreOutsideDiameterRangeDisplay;

    [ObservableProperty]
    public partial double PreviewConductorDiameterPixels { get; set; } = 88;

    [ObservableProperty]
    public partial double PreviewSideConductorHeightPixels { get; set; } = 44;

    [ObservableProperty]
    public partial double PreviewToleranceDiameterPixels { get; set; } = 180;

    [ObservableProperty]
    public partial string PreviewConductorColourHex { get; set; } = "#C7782E";

    [ObservableProperty]
    public partial string PreviewInsulationColourHex { get; set; } = "#6C7A89";

    [ObservableProperty]
    public partial string PreviewDimensionsDisplay { get; set; } =
        "Waiting for conductor and finished-core dimensions";

    [ObservableProperty]
    public partial string PreviewMaterialDisplay { get; set; } =
        "Copper conductor · insulation colour not selected";

    [ObservableProperty]
    public partial string PreviewStrandDetailDisplay { get; set; } =
        "Choose a parsed strand construction for detailed view";

    [ObservableProperty]
    public partial string PreviewCalculatedRadialWallDisplay { get; set; } =
        "—";

    [ObservableProperty]
    public partial string PreviewReferenceWallDisplay { get; set; } =
        "—";

    [ObservableProperty]
    public partial string PreviewWallAssessmentDisplay { get; set; } =
        "Choose complete dimensions to compare the wall.";

    [ObservableProperty]
    public partial string PreviewWallStatusColourHex { get; set; } = "#8A6D1D";

    [ObservableProperty]
    public partial string PreviewWallSourceDisplay { get; set; } =
        "No comparator selected";

    [ObservableProperty]
    public partial string PreviewWallSourceUrl { get; set; } = "";

    [ObservableProperty]
    public partial bool HasCorePrint { get; set; }

    [ObservableProperty]
    public partial string CorePrintText { get; set; } = "";

    [ObservableProperty]
    public partial string CorePrintColourHex { get; set; } =
        InitialWorkingValues.CorePrintColour;

    [ObservableProperty]
    public partial double CorePrintHeightMillimetres { get; set; } =
        InitialWorkingValues.CorePrintHeight;

    [ObservableProperty]
    public partial double CorePrintRepeatDistanceMillimetres { get; set; } =
        InitialWorkingValues.CorePrintRepeatDistance;

    [ObservableProperty]
    public partial double CorePrintDotPitchHorizontalMillimetres { get; set; } =
        InitialWorkingValues.CorePrintDotPitch;

    [ObservableProperty]
    public partial double CorePrintDotPitchVerticalMillimetres { get; set; } =
        InitialWorkingValues.CorePrintDotPitch;

    [ObservableProperty]
    public partial string PreviewPrintTextDisplay { get; set; } = "CORE PRINT";

    [ObservableProperty]
    public partial double PreviewPrintFontSizePixels { get; set; } = 11;

    [ObservableProperty]
    public partial double PreviewPrintCanvasWidthPixels { get; set; } = 570;

    [ObservableProperty]
    public partial double PreviewPrintCylinderWidthPixels { get; set; } = 534;

    [ObservableProperty]
    public partial double PreviewPrintSecondTextLeftPixels { get; set; } = 284;

    [ObservableProperty]
    public partial double PreviewPrintDimensionEndPixels { get; set; } = 284;

    [ObservableProperty]
    public partial double PreviewPrintRightFaceLeftPixels { get; set; } = 538;

    [ObservableProperty]
    public partial double PreviewPrintTextTopPixels { get; set; } = 34;

    [ObservableProperty]
    public partial string PreviewPrintScaleDisplay { get; set; } =
        "Axial preview scale: 1 px = 1 mm";

    [ObservableProperty]
    public partial double PreviewPrintOpacity { get; set; }

    [ObservableProperty]
    public partial string PreviewPrintSpecificationDisplay { get; set; } =
        "Print disabled";

    [ObservableProperty]
    public partial double MasterbatchSupplierQuoteTotal { get; set; } =
        InitialWorkingValues.MasterbatchQuoteTotal;

    [ObservableProperty]
    public partial double MasterbatchSupplierQuotedKilograms { get; set; } =
        InitialWorkingValues.MasterbatchQuotedKilograms;

    [ObservableProperty]
    public partial string MasterbatchPricePerKilogramDisplay { get; set; } =
        InitialWorkingValues.SupplierUnitPriceDisplay;

    [ObservableProperty]
    public partial double MasterbatchAdditionPercent { get; set; } =
        InitialWorkingValues.MasterbatchAdditionPercent;

    [ObservableProperty]
    public partial string ConductorCostPerMetreDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CompoundCostPerMetreDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string MasterbatchCostPerMetreDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CoreMaterialCostPerMetreDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CoreMaterialQuoteDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string RiskAdjustedCostPerMetreDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string RiskAdjustedQuoteDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string MarkedUpCostPerMetreDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string MarkedUpQuoteDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial IReadOnlyList<QuoteCurrencyOption> QuoteCurrencyOptions { get; set; } =
        PreferredQuoteCurrencies;

    [ObservableProperty]
    public partial QuoteCurrencyOption? SelectedQuoteCurrency { get; set; } =
        InitialWorkingValues.QuoteCurrency;

    [ObservableProperty]
    public partial string ConvertedQuoteDisplay { get; set; } =
        InitialWorkingValues.ConvertedQuoteDisplay;

    [ObservableProperty]
    public partial string ExchangeRateStatus { get; set; } =
        InitialWorkingValues.ExchangeRateStatus;

    [ObservableProperty]
    public partial bool IsRefreshingExchangeRates { get; set; }

    [ObservableProperty]
    public partial string QuoteNumber { get; set; } =
        InitialWorkingValues.QuoteNumber;

    public IReadOnlyList<string> QuoteConductorDisplayOptions { get; } =
    [
        "Construction only",
        "Construction and nominal mm²",
        "Construction and AWG",
        "Construction, AWG and nominal mm²",
    ];

    [ObservableProperty]
    public partial int QuoteConductorDisplayModeIndex { get; set; } =
        InitialWorkingValues.QuoteConductorDisplayMode;

    [ObservableProperty]
    public partial double QuoteReelCount { get; set; } =
        InitialWorkingValues.QuoteReelCount;

    [ObservableProperty]
    public partial double QuoteMetresPerReel { get; set; } =
        InitialWorkingValues.QuoteMetresPerReel;

    [ObservableProperty]
    public partial string QuoteReelPlanDisplay { get; set; } =
        InitialWorkingValues.QuoteReelPlanDisplay;

    [ObservableProperty]
    public partial bool UseExactCustomerColourName { get; set; }

    [ObservableProperty]
    public partial string QuoteConductorDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string QuoteInsulationDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string QuoteColourDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string QuoteDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string QuotePackaging { get; set; } =
        InitialWorkingValues.QuotePackaging;

    [ObservableProperty]
    public partial string QuoteEstimatedDelivery { get; set; } =
        InitialWorkingValues.QuoteEstimatedDelivery;

    [ObservableProperty]
    public partial string QuoteSpecialNotes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string QuoteTermsAndConditions { get; set; } =
        InitialWorkingValues.QuoteTermsAndConditions;

    [ObservableProperty]
    public partial string RecommendedLineSpeedDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string EffectiveLineSpeedDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string ProductionRunningTimeDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TotalProductionTimeDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string ChargeableLabourHoursDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string LabourCostDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string LabourCostPerMetreDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TotalEstimatedCostDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string RiskValueDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string MarkupValueDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CombinedRatePriceDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TargetMarginPriceDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CustomerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CustomerShortName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsCustomerSpecial { get; set; }

    [ObservableProperty]
    public partial bool UseCustomCoreName { get; set; }

    [ObservableProperty]
    public partial string CustomCoreName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GeneratedCoreNameDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string EffectiveCoreNameDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string DeliveryAddress { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ScopeOfWork { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CustomerSuppliedMaterials { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SpecialRequirements { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RisksAndOpportunities { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool PurchaseOrderMatchesQuote { get; set; }

    [ObservableProperty]
    public partial bool AdditionalRisksAtAcceptance { get; set; }

    [ObservableProperty]
    public partial int OrderDecisionIndex { get; set; }

    [ObservableProperty]
    public partial string ReviewApprovedBy { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTimeOffset ReviewDate { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    public partial DateTimeOffset AcknowledgementDate { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    public partial string ReviewNotes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int AmendmentDecisionIndex { get; set; }

    [ObservableProperty]
    public partial string AmendmentConcerns { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConductorQuoteMassDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CompoundQuoteMassDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string MasterbatchQuoteMassDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string ConductorQuotePriceDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CompoundQuotePriceDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string MasterbatchQuotePriceDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CalculationStatus { get; set; } =
        "Link and choose the required materials to calculate a one-core costing.";

    [ObservableProperty]
    public partial string CentralDataSourceDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CentralDataUpdatedDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CentralDataCountsDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CentralDataStatus { get; set; } =
        "Loading the last-known material snapshot.";

    [ObservableProperty]
    public partial bool IsRefreshingCentralData { get; set; }

    [ObservableProperty]
    public partial CentralDataConnectionState ConnectionState { get; set; }

    [ObservableProperty]
    public partial string CentralDataConnectionDisplay { get; set; } =
        "Live Data · cached";

    [ObservableProperty]
    public partial string CentralDataConnectionDetail { get; set; } =
        "No live tables are linked yet. A clean installation contains no customer or material data.";

    public ObservableCollection<CalculationStepRow> CalculationSteps { get; } = [];

    public ObservableCollection<CalculationStepRow> ConductorCalculationSteps { get; } = [];

    public ObservableCollection<CalculationStepRow> CompoundCalculationSteps { get; } = [];

    public ObservableCollection<CalculationStepRow> MasterbatchCalculationSteps { get; } = [];

    public ObservableCollection<CalculationStepRow> LabourCalculationSteps { get; } = [];

    public ObservableCollection<CalculationFlowStage> ConductorCalculationFlow { get; } = [];

    public ObservableCollection<CalculationFlowStage> CompoundCalculationFlow { get; } = [];

    public ObservableCollection<CalculationFlowStage> MasterbatchCalculationFlow { get; } = [];

    public ObservableCollection<CalculationFlowStage> LabourCalculationFlow { get; } = [];

    public IReadOnlyList<CentralDataTableLink> DatabaseTableLinks { get; private set; } = [];

    public ObservableCollection<CentralDataLinkStatusRow>
        CentralDataLinkStatuses { get; } = [];

    [ObservableProperty]
    public partial string CentralDataLinkSummaryDisplay { get; set; } =
        "Live Data · cached only";

    public bool HasConfiguredLiveLink => DatabaseTableLinks.Count > 0;

    public bool ShouldAttemptAutomaticRefresh =>
        HasConfiguredLiveLink &&
        ConnectionState != CentralDataConnectionState.Offline &&
        !IsRefreshingCentralData;

    public SingleCoreCostingViewModel(
        CentralDataService centralDataService,
        IExchangeRateService? exchangeRateService = null)
    {
        _centralDataService =
            centralDataService ?? throw new ArgumentNullException(nameof(centralDataService));
        _exchangeRateService = exchangeRateService;
        ApplyCentralDataState(_centralDataService.Load());
        UpdateQuotationDisplays();
        UpdateQuoteReelPlan();
        UpdateFinishedCoreOutsideDiameterRange();
        UpdateSingleCorePreviewAppearance();
        Recalculate();
        _suppressDocumentTracking = false;
        PropertyChanged += TrackPersistedInputChange;
        UpdateRevisionStatus();
    }

    public void Recalculate()
    {
        if (SelectedCopper is null ||
            SelectedCompound is null ||
            SelectedMasterbatch is null)
        {
            _currentCalculatedResult = null;
            HasValidationErrors = true;
            CalculationStatus =
                "The retained central-data snapshot does not contain all three required material types.";
            return;
        }

        try
        {
            var result = SingleCoreCostingCalculator.Calculate(
                new SingleCoreCostingInputs(
                    SelectedCopper.Description,
                    new MaterialSupplierQuote(
                        new SupplierQuoteTotal(
                            ToDecimal(ConductorSupplierQuoteTotal)),
                        new MassKilograms(
                            ToDecimal(ConductorSupplierQuotedKilograms))),
                    new YieldMetresPerKilogram(
                        ToDecimal(ConductorYieldMetresPerKilogram)),
                    new Millimetres(
                        ToDecimal(ConductorOutsideDiameterMillimetres)),
                    SelectedCompound.CompoundName,
                    new MaterialSupplierQuote(
                        new SupplierQuoteTotal(
                            ToDecimal(CompoundSupplierQuoteTotal)),
                        new MassKilograms(
                            ToDecimal(CompoundSupplierQuotedKilograms))),
                    new SpecificGravity(ToDecimal(CompoundSpecificGravity)),
                    new Millimetres(
                        ToDecimal(NominalFinishedCoreOutsideDiameterMillimetres)),
                    new Millimetres(
                        ToDecimal(FinishedCoreOutsideDiameterToleranceMillimetres)),
                    $"{SelectedMasterbatch.ColourName} ({SelectedMasterbatch.ColourCode})",
                    new MaterialSupplierQuote(
                        new SupplierQuoteTotal(
                            ToDecimal(MasterbatchSupplierQuoteTotal)),
                        new MassKilograms(
                            ToDecimal(MasterbatchSupplierQuotedKilograms))),
                    new AdditionRateFraction(
                        ToDecimal(MasterbatchAdditionPercent) / 100m),
                    new LengthMetres(ToDecimal(QuoteLengthMetres)),
                    new UsageAllowanceRateFraction(
                        ToDecimal(UsageAllowancePercent) / 100m),
                    new RiskRateFraction(ToDecimal(RiskPercent) / 100m),
                    new MarkupRateFraction(ToDecimal(MarkupPercent) / 100m)));

            var quoteLength = new LengthMetres(ToDecimal(QuoteLengthMetres));
            var labour = ProductionLabourCalculator.Calculate(
                new ProductionLabourInputs(
                    "Insulation",
                    quoteLength,
                    new Millimetres(
                        ToDecimal(NominalFinishedCoreOutsideDiameterMillimetres)),
                    UseManualLineSpeed
                        ? new LineSpeedMetresPerHour(
                            ToDecimal(ManualLineSpeedMetresPerHour))
                        : null,
                    new LabourHours(ToDecimal(ProductionSetupTimeHours)),
                    new OperatorCount(ToDecimal(ProductionOperatorCount)),
                    new HourlyLabourRate(ToDecimal(HourlyLabourRate))));
            var commercial = CommercialPricingCalculator.Calculate(
                new CommercialPricingInputs(
                    result.CoreMaterialPriceForQuote,
                    labour.LabourCost,
                    new RiskRateFraction(ToDecimal(RiskPercent) / 100m),
                    new MarkupRateFraction(ToDecimal(MarkupPercent) / 100m),
                    new TargetMarginRateFraction(
                        ToDecimal(TargetMarginPercent) / 100m)));

            ConductorCostPerMetreDisplay =
                PoundPerMetre(result.Conductor.PricePerMetre.Value);
            CompoundCostPerMetreDisplay =
                PoundPerMetre(result.Compound.Material.PricePerMetre.Value);
            MasterbatchCostPerMetreDisplay =
                PoundPerMetre(result.Masterbatch.MasterbatchPricePerMetre.Value);
            CoreMaterialCostPerMetreDisplay =
                PoundPerMetre(result.CoreMaterialPricePerMetre.Value);
            CoreMaterialQuoteDisplay = Pounds(result.CoreMaterialPriceForQuote);
            RiskAdjustedCostPerMetreDisplay =
                PoundPerMetre(
                    commercial.RiskAdjustedCost / quoteLength.Value);
            RiskAdjustedQuoteDisplay =
                Pounds(commercial.RiskAdjustedCost);
            MarkedUpCostPerMetreDisplay =
                PoundPerMetre(
                    commercial.SequentialRiskThenMarkupPrice / quoteLength.Value);
            MarkedUpQuoteDisplay =
                Pounds(commercial.SequentialRiskThenMarkupPrice);
            _recommendedQuotePrice =
                commercial.SequentialRiskThenMarkupPrice;
            UpdateConvertedQuoteDisplay();

            RecommendedLineSpeedDisplay =
                $"{labour.RecommendedLineSpeed.Value:N0} m/h";
            EffectiveLineSpeedDisplay =
                $"{labour.EffectiveLineSpeed.Value:N0} m/h";
            ProductionRunningTimeDisplay =
                Duration(labour.RunningTime.Value);
            TotalProductionTimeDisplay =
                Duration(labour.TotalProcessTime.Value);
            ChargeableLabourHoursDisplay =
                $"{labour.ChargeableLabourHours.Value:N4} operator h";
            LabourCostDisplay = Pounds(labour.LabourCost);
            LabourCostPerMetreDisplay =
                PoundPerMetre(labour.LabourCostPerMetre.Value);
            TotalEstimatedCostDisplay = Pounds(commercial.EstimatedCost);
            RiskValueDisplay = Pounds(commercial.RiskValue);
            MarkupValueDisplay = Pounds(commercial.MarkupValue);
            CombinedRatePriceDisplay =
                Pounds(commercial.CombinedRiskAndMarkupPrice);
            TargetMarginPriceDisplay =
                Pounds(commercial.TargetGrossMarginPrice);

            ConductorQuoteMassDisplay =
                $"{result.Conductor.QuoteMass.Value:N6} kg";
            CompoundQuoteMassDisplay =
                $"{result.Compound.Material.QuoteMass.Value:N6} kg";
            MasterbatchQuoteMassDisplay =
                $"{result.Masterbatch.MasterbatchMassForQuote.Value:N6} kg";
            ConductorQuotePriceDisplay =
                Pounds(result.Conductor.QuotePrice);
            CompoundQuotePriceDisplay =
                Pounds(result.Compound.Material.QuotePrice);
            MasterbatchQuotePriceDisplay =
                Pounds(
                    result.Masterbatch.MasterbatchPricePerMetre.Value *
                    quoteLength.Value);

            CalculationSteps.Clear();
            foreach (var step in result.Steps.Where(
                         step => !IsMaterialCommercialStep(step)))
            {
                CalculationSteps.Add(ToRow(step));
            }
            foreach (var step in labour.Steps)
            {
                CalculationSteps.Add(ToRow(step));
            }
            foreach (var step in commercial.Steps)
            {
                CalculationSteps.Add(ToRow(step));
            }

            ReplaceRows(
                ConductorCalculationSteps,
                result.Conductor.Steps);
            ReplaceFlowStages(
                ConductorCalculationFlow,
                result.Conductor.Steps);
            ReplaceRows(
                CompoundCalculationSteps,
                result.Compound.Steps);
            ReplaceFlowStages(
                CompoundCalculationFlow,
                result.Compound.Steps);
            ReplaceRows(
                MasterbatchCalculationSteps,
                result.Steps.Where(
                    step => step.Id.StartsWith(
                        "masterbatch-",
                        StringComparison.Ordinal)));
            ReplaceFlowStages(
                MasterbatchCalculationFlow,
                result.Steps.Where(
                    step => step.Id.StartsWith(
                        "masterbatch-",
                        StringComparison.Ordinal)));
            ReplaceRows(
                LabourCalculationSteps,
                labour.Steps);
            ReplaceFlowStages(
                LabourCalculationFlow,
                labour.Steps);
            UpdateCoreName();
            _currentCalculatedResult =
                CaptureCalculatedResult(result, labour, commercial);
            HasValidationErrors = false;

            CalculationStatus =
                "Live costing updated. Material and labour form the estimated cost; risk is applied before markup. Alternative combined-rate and target-margin prices are comparison methods.";
        }
        catch (ArgumentException exception)
        {
            _currentCalculatedResult = null;
            HasValidationErrors = true;
            ClearMaterialCalculationSteps();
            CalculationStatus = exception.Message;
        }
        catch (OverflowException)
        {
            _currentCalculatedResult = null;
            HasValidationErrors = true;
            ClearMaterialCalculationSteps();
            CalculationStatus =
                "One or more values are too large for a costing calculation.";
        }
    }

    public async Task RefreshExchangeRatesAsync()
    {
        if (IsRefreshingExchangeRates)
        {
            return;
        }

        if (_exchangeRateService is null)
        {
            ExchangeRateStatus =
                "Live exchange rates are not configured. GBP costing remains available.";
            return;
        }

        IsRefreshingExchangeRates = true;
        ExchangeRateStatus =
            "Refreshing European Central Bank reference rates…";
        try
        {
            var selectedCode = SelectedQuoteCurrency?.Code ?? "GBP";
            _exchangeRateSnapshot =
                await _exchangeRateService.GetLatestAsync();
            QuoteCurrencyOptions = PreferredQuoteCurrencies
                .Where(option =>
                    option.Code == "GBP" ||
                    _exchangeRateSnapshot.RatesPerEuro.ContainsKey(option.Code))
                .ToArray();
            SelectedQuoteCurrency =
                QuoteCurrencyOptions.FirstOrDefault(option =>
                    string.Equals(
                        option.Code,
                        selectedCode,
                        StringComparison.OrdinalIgnoreCase)) ??
                QuoteCurrencyOptions[0];
            ExchangeRateStatus =
                $"{(_exchangeRateSnapshot.IsRetainedCache ? "Retained" : "Live")} ECB " +
                $"reference rates dated {_exchangeRateSnapshot.RateDate:dd MMM yyyy}. " +
                "Reference rates are informational; confirm the commercial rate before issue.";
            UpdateConvertedQuoteDisplay();
        }
        catch (InvalidOperationException exception)
        {
            _exchangeRateSnapshot = null;
            SelectedQuoteCurrency = QuoteCurrencyOptions[0];
            ExchangeRateStatus =
                $"{exception.Message} GBP costing and PDF generation remain available.";
            UpdateConvertedQuoteDisplay();
        }
        finally
        {
            IsRefreshingExchangeRates = false;
        }
    }

    public A4QuotationDocument CreateA4QuotationDocument()
    {
#if !ATAG_PUBLIC_REVIEW
        if (SelectedCopper is null ||
            SelectedCompound is null ||
            SelectedMasterbatch is null)
        {
            throw new InvalidOperationException(
                "Choose all three material records before generating a quotation.");
        }
#endif

        var currency = SelectedQuoteCurrency ?? PreferredQuoteCurrencies[0];
        var goodsTotal = AppRuntimeMode.IsPublicReview
            ? 0m
            : ConvertRecommendedPrice(_recommendedQuotePrice, currency.Code);
        var reelCount = FiniteNonNegativeDecimal(QuoteReelCount);
        var metresPerReel = FiniteNonNegativeDecimal(QuoteMetresPerReel);
        var quoteLength = reelCount * metresPerReel;
        var unitPrice = quoteLength > 0m
            ? goodsTotal / quoteLength
            : 0m;

        return new A4QuotationDocument(
            string.IsNullOrWhiteSpace(QuoteNumber)
                ? $"{AppRuntimeMode.QuotationPrefix}-" +
                  $"{DateTimeOffset.Now:yyyyMMdd-HHmm}"
                : QuoteNumber.Trim(),
            DateOnly.FromDateTime(DateTime.Today),
            CustomerName,
            DeliveryAddress
                .Split(
                    [Environment.NewLine, "\r", "\n"],
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries),
            SelectedOperator?.DisplayName ?? ReviewApprovedBy,
            string.IsNullOrWhiteSpace(QuoteDescription)
                ? AppRuntimeMode.IsPublicReview
                    ? "Item not specified"
                    : EffectiveCoreNameDisplay
                : QuoteDescription.Trim(),
            reelCount,
            metresPerReel,
            quoteLength,
            currency.Code,
            currency.Symbol,
            unitPrice,
            goodsTotal,
            0m,
            string.IsNullOrWhiteSpace(QuoteConductorDisplay)
                ? "Not specified"
                : QuoteConductorDisplay,
            string.IsNullOrWhiteSpace(QuoteInsulationDisplay)
                ? "Not specified"
                : QuoteInsulationDisplay,
            string.IsNullOrWhiteSpace(QuoteColourDisplay)
                ? "Not specified"
                : QuoteColourDisplay,
            string.IsNullOrWhiteSpace(QuotePackaging)
                ? "Not specified"
                : QuotePackaging,
            reelCount > 0m && metresPerReel > 0m
                ? $"{metresPerReel:N0} m per reel ({reelCount:N0} reels)"
                : "Not specified",
            string.IsNullOrWhiteSpace(QuoteEstimatedDelivery)
                ? "Not specified"
                : QuoteEstimatedDelivery,
            QuoteSpecialNotes,
            QuoteTermsAndConditions)
        {
            IssuerName = AppRuntimeMode.QuotationIssuerName,
            IssuerAddressLines = AppRuntimeMode.QuotationIssuerAddressLines,
        };
    }

    public SingleCoreProjectDocument CreateProjectDocument()
    {
        var now = DateTimeOffset.UtcNow;
        return new SingleCoreProjectDocument
        {
            ConstructionKind = CostingConstructionKind.SingleInsulatedCore,
            ProjectId = CurrentProjectId,
            RevisionId = CurrentRevisionId,
            RevisionNumber = CurrentRevisionNumber,
            RevisionState = CurrentRevisionState,
            CreatedAtUtc = CurrentRevisionCreatedAtUtc,
            UpdatedAtUtc = now,
            ApprovedAtUtc = CurrentRevisionApprovedAtUtc,
            SavedAt = now,
            CentralDataRevision = _centralDataRevision,
            CopperId = SelectedCopper?.Id ?? "",
            CopperDescription = SelectedCopper?.Description ?? "",
            CopperSupplier = SelectedCopper?.Supplier ?? "",
            CompoundId = SelectedCompound?.Id ?? "",
            CompoundName = SelectedCompound?.CompoundName ?? "",
            CompoundSupplier = SelectedCompound?.Supplier ?? "",
            MasterbatchCode = SelectedMasterbatch?.ColourCode ?? "",
            MasterbatchName = SelectedMasterbatch?.ColourName ?? "",
            MasterbatchSupplier = SelectedMasterbatch?.Supplier ?? "",
            CustomerContactId = SelectedCustomerContact?.Id,
            OperatorId = SelectedOperator?.Id,
            ConductorMaterialType = SelectedConductorMaterialType ?? "",
            ConductorSelectionModeIndex = ConductorSelectionModeIndex,
            QuoteLengthMetres = QuoteLengthMetres,
            UsageAllowancePercent = UsageAllowancePercent,
            RiskPercent = RiskPercent,
            MarkupPercent = MarkupPercent,
            TargetMarginPercent = TargetMarginPercent,
            ConductorSupplierQuoteTotal = ConductorSupplierQuoteTotal,
            ConductorSupplierQuotedKilograms =
                ConductorSupplierQuotedKilograms,
            ConductorYieldMetresPerKilogram =
                ConductorYieldMetresPerKilogram,
            ConductorOutsideDiameterMillimetres =
                ConductorOutsideDiameterMillimetres,
            CompoundSupplierQuoteTotal = CompoundSupplierQuoteTotal,
            CompoundSupplierQuotedKilograms =
                CompoundSupplierQuotedKilograms,
            CompoundSpecificGravity = CompoundSpecificGravity,
            NominalFinishedCoreOutsideDiameterMillimetres =
                NominalFinishedCoreOutsideDiameterMillimetres,
            FinishedCoreOutsideDiameterToleranceMillimetres =
                FinishedCoreOutsideDiameterToleranceMillimetres,
            UseSeparateNegativeOutsideDiameterTolerance =
                UseSeparateNegativeOutsideDiameterTolerance,
            FinishedCoreNegativeOutsideDiameterToleranceMillimetres =
                FinishedCoreNegativeOutsideDiameterToleranceMillimetres,
            MasterbatchSupplierQuoteTotal = MasterbatchSupplierQuoteTotal,
            MasterbatchSupplierQuotedKilograms =
                MasterbatchSupplierQuotedKilograms,
            MasterbatchAdditionPercent = MasterbatchAdditionPercent,
            HasCorePrint = HasCorePrint,
            CorePrintText = CorePrintText,
            CorePrintColourHex = CorePrintColourHex,
            CorePrintHeightMillimetres = CorePrintHeightMillimetres,
            CorePrintRepeatDistanceMillimetres =
                CorePrintRepeatDistanceMillimetres,
            CorePrintDotPitchHorizontalMillimetres =
                CorePrintDotPitchHorizontalMillimetres,
            CorePrintDotPitchVerticalMillimetres =
                CorePrintDotPitchVerticalMillimetres,
            UseManualLineSpeed = UseManualLineSpeed,
            ManualLineSpeedMetresPerHour = ManualLineSpeedMetresPerHour,
            ProductionSetupTimeHours = ProductionSetupTimeHours,
            ProductionOperatorCount = ProductionOperatorCount,
            HourlyLabourRate = HourlyLabourRate,
            CustomerName = CustomerName,
            CustomerShortName = CustomerShortName,
            IsCustomerSpecial = IsCustomerSpecial,
            UseCustomCoreName = UseCustomCoreName,
            CustomCoreName = CustomCoreName,
            DeliveryAddress = DeliveryAddress,
            ScopeOfWork = ScopeOfWork,
            CustomerSuppliedMaterials = CustomerSuppliedMaterials,
            SpecialRequirements = SpecialRequirements,
            RisksAndOpportunities = RisksAndOpportunities,
            PurchaseOrderMatchesQuote = PurchaseOrderMatchesQuote,
            AdditionalRisksAtAcceptance = AdditionalRisksAtAcceptance,
            OrderDecisionIndex = OrderDecisionIndex,
            ReviewApprovedBy = ReviewApprovedBy,
            ReviewDate = ReviewDate,
            AcknowledgementDate = AcknowledgementDate,
            ReviewNotes = ReviewNotes,
            AmendmentDecisionIndex = AmendmentDecisionIndex,
            AmendmentConcerns = AmendmentConcerns,
            QuoteNumber = QuoteNumber,
            QuoteCurrencyCode = SelectedQuoteCurrency?.Code ?? "GBP",
            QuoteReelCount = QuoteReelCount,
            QuoteMetresPerReel = QuoteMetresPerReel,
            QuoteConductorDisplayModeIndex =
                QuoteConductorDisplayModeIndex,
            UseExactCustomerColourName =
                UseExactCustomerColourName,
            QuoteDescription = QuoteDescription,
            QuotePackaging = QuotePackaging,
            QuoteEstimatedDelivery = QuoteEstimatedDelivery,
            QuoteSpecialNotes = QuoteSpecialNotes,
            QuoteTermsAndConditions = QuoteTermsAndConditions,
            RuleVersions = CalculationSteps
                .Select(step => step.RuleVersion)
                .Where(version => !string.IsNullOrWhiteSpace(version))
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            CalculatedResult = _currentCalculatedResult,
        };
    }

    public SingleCoreProjectDocument CreateApprovedProjectDocument()
    {
        if (HasValidationErrors || _currentCalculatedResult is null)
        {
            throw new InvalidOperationException(
                "Resolve the current calculation validation error before approving this revision.");
        }

        return _revisionService.Approve(
            CreateProjectDocument(),
            DateTimeOffset.UtcNow);
    }

    public SingleCoreProjectDocument CreateDuplicateProjectDocument() =>
        _revisionService.Duplicate(
            CreateProjectDocument(),
            DateTimeOffset.UtcNow);

    public bool TryApplyProjectDocument(
        SingleCoreProjectDocument document,
        out string message)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!document.IsSupportedSchema)
        {
            message =
                $"This costing uses schema {document.SchemaVersion}; this app supports schema " +
                $"{SingleCoreProjectDocument.OldestSupportedSchemaVersion} to " +
                $"{SingleCoreProjectDocument.CurrentSchemaVersion}.";
            return false;
        }

        document = document.Upgrade();
        if (document.ConstructionKind !=
            CostingConstructionKind.SingleInsulatedCore)
        {
            message = "The selected revision is a dual-insulation costing.";
            return false;
        }

        var copper = _allCopperMaterials.FirstOrDefault(
            item => item.Id == document.CopperId) ??
            CreateSavedCopperReference(document);
        var compound = CompoundMaterials.FirstOrDefault(
            item => item.Id == document.CompoundId) ??
            CreateSavedCompoundReference(document);
        var masterbatch = MasterbatchMaterials.FirstOrDefault(
            item => item.ColourCode == document.MasterbatchCode) ??
            CreateSavedMasterbatchReference(document);
        if (copper is null || compound is null || masterbatch is null)
        {
            message =
                "The costing does not contain enough saved material evidence to reopen it. No values were changed.";
            return false;
        }

        var previousTrackingSuppression = _suppressDocumentTracking;
        _suppressDocumentTracking = true;
        _isApplyingCentralData = true;
        try
        {
            if (!_allCopperMaterials.Any(item => item.Id == copper.Id))
            {
                _allCopperMaterials = [copper, .. _allCopperMaterials];
                ConductorMaterialTypeOptions = _allCopperMaterials
                    .Select(item => item.MaterialTypeDisplay)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(item => item)
                    .ToArray();
            }

            if (!CompoundMaterials.Any(item => item.Id == compound.Id))
            {
                CompoundMaterials = [compound, .. CompoundMaterials];
            }

            if (!MasterbatchMaterials.Any(
                    item => item.ColourCode == masterbatch.ColourCode))
            {
                MasterbatchMaterials = [masterbatch, .. MasterbatchMaterials];
            }

            SelectedConductorMaterialType =
                ConductorMaterialTypeOptions.FirstOrDefault(
                    item => string.Equals(
                        item,
                        document.ConductorMaterialType,
                        StringComparison.OrdinalIgnoreCase)) ??
                copper.MaterialTypeDisplay;
            ConductorSelectionModeIndex =
                Math.Clamp(document.ConductorSelectionModeIndex, 0, 2);
            PopulateConductorOptions(copper);
            SelectedCompound = compound;
            SelectedMasterbatch = masterbatch;
            SelectedCustomerContact = ContactMaterials.FirstOrDefault(
                item => item.Id == document.CustomerContactId);
            SelectedOperator = OperatorMaterials.FirstOrDefault(
                item => item.Id == document.OperatorId);

            ApplyCopperDefaults(copper);
            ApplyCompoundDefaults(compound);
            ApplyMasterbatchDefaults(masterbatch);

            QuoteLengthMetres = document.QuoteLengthMetres;
            UsageAllowancePercent = document.UsageAllowancePercent;
            RiskPercent = document.RiskPercent;
            MarkupPercent = document.MarkupPercent;
            TargetMarginPercent = document.TargetMarginPercent;
            ConductorSupplierQuoteTotal =
                document.ConductorSupplierQuoteTotal;
            ConductorSupplierQuotedKilograms =
                document.ConductorSupplierQuotedKilograms;
            ConductorYieldMetresPerKilogram =
                document.ConductorYieldMetresPerKilogram > 0
                    ? document.ConductorYieldMetresPerKilogram
                    : ConductorYieldMetresPerKilogram;
            ConductorOutsideDiameterMillimetres =
                document.ConductorOutsideDiameterMillimetres > 0
                    ? document.ConductorOutsideDiameterMillimetres
                    : ConductorOutsideDiameterMillimetres;
            CompoundSupplierQuoteTotal =
                document.CompoundSupplierQuoteTotal;
            CompoundSupplierQuotedKilograms =
                document.CompoundSupplierQuotedKilograms;
            CompoundSpecificGravity =
                document.CompoundSpecificGravity > 0
                    ? document.CompoundSpecificGravity
                    : CompoundSpecificGravity;
            NominalFinishedCoreOutsideDiameterMillimetres =
                document.NominalFinishedCoreOutsideDiameterMillimetres;
            FinishedCoreOutsideDiameterToleranceMillimetres =
                document.FinishedCoreOutsideDiameterToleranceMillimetres;
            UseSeparateNegativeOutsideDiameterTolerance =
                document.UseSeparateNegativeOutsideDiameterTolerance;
            FinishedCoreNegativeOutsideDiameterToleranceMillimetres =
                document.FinishedCoreNegativeOutsideDiameterToleranceMillimetres > 0
                    ? document.FinishedCoreNegativeOutsideDiameterToleranceMillimetres
                    : document.FinishedCoreOutsideDiameterToleranceMillimetres;
            MasterbatchSupplierQuoteTotal =
                document.MasterbatchSupplierQuoteTotal;
            MasterbatchSupplierQuotedKilograms =
                document.MasterbatchSupplierQuotedKilograms;
            MasterbatchAdditionPercent = document.MasterbatchAdditionPercent;
            HasCorePrint = document.HasCorePrint;
            CorePrintText = document.CorePrintText;
            CorePrintColourHex =
                string.IsNullOrWhiteSpace(document.CorePrintColourHex)
                    ? "#FFFFFF"
                    : document.CorePrintColourHex;
            CorePrintHeightMillimetres =
                document.CorePrintHeightMillimetres > 0
                    ? document.CorePrintHeightMillimetres
                    : 0.6;
            CorePrintRepeatDistanceMillimetres =
                document.CorePrintRepeatDistanceMillimetres > 0
                    ? document.CorePrintRepeatDistanceMillimetres
                    : 250;
            CorePrintDotPitchHorizontalMillimetres =
                document.CorePrintDotPitchHorizontalMillimetres > 0
                    ? document.CorePrintDotPitchHorizontalMillimetres
                    : 0.25;
            CorePrintDotPitchVerticalMillimetres =
                document.CorePrintDotPitchVerticalMillimetres > 0
                    ? document.CorePrintDotPitchVerticalMillimetres
                    : 0.25;
            UseManualLineSpeed = document.UseManualLineSpeed;
            ManualLineSpeedMetresPerHour =
                document.ManualLineSpeedMetresPerHour;
            ProductionSetupTimeHours = document.ProductionSetupTimeHours;
            ProductionOperatorCount = document.ProductionOperatorCount;
            HourlyLabourRate = document.HourlyLabourRate;
            CustomerName = document.CustomerName;
            CustomerShortName = document.CustomerShortName;
            IsCustomerSpecial = document.IsCustomerSpecial;
            UseCustomCoreName = document.UseCustomCoreName;
            CustomCoreName = document.CustomCoreName;
            DeliveryAddress = document.DeliveryAddress;
            ScopeOfWork = document.ScopeOfWork;
            CustomerSuppliedMaterials = document.CustomerSuppliedMaterials;
            SpecialRequirements = document.SpecialRequirements;
            RisksAndOpportunities = document.RisksAndOpportunities;
            PurchaseOrderMatchesQuote =
                document.PurchaseOrderMatchesQuote;
            AdditionalRisksAtAcceptance =
                document.AdditionalRisksAtAcceptance;
            OrderDecisionIndex = document.OrderDecisionIndex;
            ReviewApprovedBy = document.ReviewApprovedBy;
            ReviewDate = document.ReviewDate;
            AcknowledgementDate = document.AcknowledgementDate;
            ReviewNotes = document.ReviewNotes;
            AmendmentDecisionIndex = document.AmendmentDecisionIndex;
            AmendmentConcerns = document.AmendmentConcerns;
            QuoteNumber = string.IsNullOrWhiteSpace(document.QuoteNumber)
                ? QuoteNumber
                : document.QuoteNumber;
            SelectedQuoteCurrency =
                QuoteCurrencyOptions.FirstOrDefault(
                    item => string.Equals(
                        item.Code,
                        document.QuoteCurrencyCode,
                        StringComparison.OrdinalIgnoreCase)) ??
                QuoteCurrencyOptions[0];
            QuoteReelCount = document.QuoteReelCount > 0
                ? document.QuoteReelCount
                : 1;
            QuoteMetresPerReel = document.QuoteMetresPerReel > 0
                ? document.QuoteMetresPerReel
                : document.QuoteLengthMetres;
            QuoteConductorDisplayModeIndex =
                Math.Clamp(document.QuoteConductorDisplayModeIndex, 0, 3);
            UseExactCustomerColourName =
                document.UseExactCustomerColourName;
            QuoteDescription = document.QuoteDescription;
            QuotePackaging = string.IsNullOrWhiteSpace(document.QuotePackaging)
                ? "Reels"
                : document.QuotePackaging;
            QuoteEstimatedDelivery =
                string.IsNullOrWhiteSpace(document.QuoteEstimatedDelivery)
                    ? "To be confirmed"
                    : document.QuoteEstimatedDelivery;
            QuoteSpecialNotes = document.QuoteSpecialNotes;
            QuoteTermsAndConditions =
                string.IsNullOrWhiteSpace(document.QuoteTermsAndConditions)
                    ? QuoteTermsAndConditions
                    : document.QuoteTermsAndConditions;
            _isUsingSavedMaterialValues = true;
            UpdateSupplierUnitPriceDisplays();
            UpdateCoreName();
            UpdateQuotationDisplays();
            UpdateQuoteReelPlan();
            UpdateFinishedCoreOutsideDiameterRange();
        }
        finally
        {
            _isApplyingCentralData = false;
            _suppressDocumentTracking = previousTrackingSuppression;
        }

        ApplyRevisionIdentity(document);
        if (document.RevisionState ==
                CostingRevisionState.ApprovedRevision &&
            document.CalculatedResult is not null)
        {
            ApplyCalculatedResult(document.CalculatedResult);
            HasValidationErrors = false;
            message =
                $"Reopened approved revision {document.RevisionNumber}, saved " +
                $"{document.SavedAt.ToLocalTime():dd MMM yyyy HH:mm}. " +
                "Its stored outputs and calculation trace are shown exactly as approved.";
        }
        else
        {
            Recalculate();
            message =
                $"Reopened working revision {document.RevisionNumber}, saved " +
                $"{document.SavedAt.ToLocalTime():dd MMM yyyy HH:mm}. " +
                "Calculated outputs were rebuilt from the saved material values and current shared rules.";
        }

        message +=
            string.IsNullOrWhiteSpace(document.CentralDataRevision) ||
            document.CentralDataRevision == _centralDataRevision
                ? ""
                : $" The saved central-data revision was {document.CentralDataRevision}; " +
                  $"the available catalogue is {_centralDataRevision}, but the saved locked values remain active.";
        HasUnsavedChanges = false;
        CurrentDocumentPath = null;
        UpdateRevisionStatus();
        CalculationStatus = message;
        return true;
    }

    public void MarkDocumentPersisted(
        SingleCoreProjectDocument document,
        string fullPath)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);

        ApplyRevisionIdentity(document.Upgrade());
        _currentCalculatedResult = document.CalculatedResult;
        CurrentDocumentPath = Path.GetFullPath(fullPath);
        HasUnsavedChanges = false;
        UpdateRevisionStatus();
    }

    public void MarkDocumentNeedsIndexing(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        CurrentDocumentPath = Path.GetFullPath(sourcePath);
        HasUnsavedChanges = true;
        UpdateRevisionStatus();
        SaveStateDisplay = "Not yet indexed · save to selected folder";
    }

    public bool TryDuplicateCurrentProject(out string message)
    {
        var sourceRevisionNumber = CurrentRevisionNumber;
        var duplicate = CreateDuplicateProjectDocument();
        if (!TryApplyProjectDocument(duplicate, out message))
        {
            return false;
        }

        CurrentDocumentPath = null;
        HasUnsavedChanges = true;
        UpdateRevisionStatus();
        message =
            $"Created a new unsaved project from revision {sourceRevisionNumber}. " +
            "The source file and approval state were not changed.";
        CalculationStatus = message;
        return true;
    }

    public CentralDataTableLink? GetDatabaseTableLink(CentralDataArea area) =>
        DatabaseTableLinks.FirstOrDefault(link => link.Area == area);

    public CentralDataState SaveDatabaseTableLink(CentralDataTableLink link)
    {
        var state = _centralDataService.SaveTableLink(link);
        _centralDataAreaConnectionStates[link.Area] =
            CentralDataAreaConnectionState.ReadyToCheck;
        ApplyCentralDataState(state, preserveSelections: true);
        CentralDataStatus =
            $"{AreaName(link.Area)} database table link saved. " +
            "The retained material data remains active until a database refresh succeeds.";
        SetConnectionState(
            CentralDataConnectionState.Cached,
            "Material link · ready to check",
            "The link is saved. Cached tables remain active until a live refresh succeeds.");
        return state;
    }

    public CentralDataState RemoveDatabaseTableLink(CentralDataArea area)
    {
        var state = _centralDataService.RemoveTableLink(area);
        _centralDataAreaConnectionStates.Remove(area);
        ApplyCentralDataState(state, preserveSelections: true);
        CentralDataStatus =
            $"{AreaName(area)} refresh link removed. " +
            "Its last transformed table and validated costing data remain available offline.";
        SetConnectionState(
            CentralDataConnectionState.Cached,
            HasConfiguredLiveLink ? "Material link · cached" : "Live Data · cached",
            HasConfiguredLiveLink
                ? "Other configured links can still refresh; the removed area continues from retained data."
                : "No live refresh link is configured. Retained tables and manual project save remain available.");
        return state;
    }

    public CentralDataTableImportResult ImportDatabaseTable(
        CentralDataTableLink link,
        CentralDataTablePreview preview)
    {
        var result = _centralDataService.ImportTable(link, preview);
        if (!result.Succeeded)
        {
            CentralDataStatus = result.Message;
            return result;
        }

        ApplyCentralDataState(
            _centralDataService.Load(),
            preserveSelections: true);
        _centralDataAreaConnectionStates[link.Area] =
            CentralDataAreaConnectionState.Live;
        UpdateCentralDataLinkStatuses();
        CentralDataStatus = string.Join(
            " ",
            new[] { result.Message }.Concat(result.Warnings));
        SetConnectionState(
            CentralDataConnectionState.Online,
            "Material link · LIVE",
            $"{AreaName(link.Area)} was imported from {link.TableName}. The retained local table is current.");
        Recalculate();
        return result;
    }

    public async Task RefreshCentralDataAsync(bool isAutomatic = false)
    {
        if (IsRefreshingCentralData)
        {
            return;
        }

        IsRefreshingCentralData = true;
        foreach (var link in DatabaseTableLinks)
        {
            _centralDataAreaConnectionStates[link.Area] =
                CentralDataAreaConnectionState.Checking;
        }
        UpdateCentralDataLinkStatuses();
        SetConnectionState(
            CentralDataConnectionState.Checking,
            "Material link · checking",
            "Trying the configured central-data source now.");

        try
        {
            var result = await _centralDataService.RefreshAsync();
            ApplyCentralDataState(result.State, preserveSelections: true);
            var refreshedAreas = result.EffectiveAreaResults
                .ToDictionary(areaResult => areaResult.Area);
            foreach (var link in DatabaseTableLinks)
            {
                _centralDataAreaConnectionStates[link.Area] =
                    refreshedAreas.TryGetValue(link.Area, out var areaResult) &&
                    areaResult.Updated &&
                    !areaResult.UsedRetainedSnapshot
                        ? CentralDataAreaConnectionState.Live
                        : CentralDataAreaConnectionState.Offline;
            }
            UpdateCentralDataLinkStatuses();
            CentralDataStatus = result.Message;

            if (result.Updated && !result.UsedRetainedSnapshot)
            {
                SetConnectionState(
                    CentralDataConnectionState.Online,
                    "Material link · LIVE",
                    $"All configured linked tables were refreshed at " +
                    $"{DateTimeOffset.Now:HH:mm:ss}. The local snapshot is current.");
                Recalculate();
            }
            else if (result.Updated)
            {
                SetConnectionState(
                    CentralDataConnectionState.Offline,
                    "Material link · PARTIAL",
                    $"Some linked tables refreshed at {DateTimeOffset.Now:HH:mm:ss}, but at least one link failed. " +
                    "Automatic attempts are paused; retained rows remain available for the failed link. Use Refresh link to try again.");
                Recalculate();
            }
            else if (HasConfiguredLiveLink)
            {
                SetConnectionState(
                    CentralDataConnectionState.Offline,
                    "Material link · OFFLINE",
                    $"{result.Message} Automatic attempts are paused; use Refresh link to try again. " +
                    "The last loaded tables remain available for costing, and manual project save remains available.");
            }
            else
            {
                SetConnectionState(
                    CentralDataConnectionState.Cached,
                    "Live Data · cached",
                    "No live Access or SQL table link is configured. Previously imported local tables, if any, and manual project save remain available.");
            }
        }
        catch (OperationCanceledException)
        {
            foreach (var link in DatabaseTableLinks)
            {
                _centralDataAreaConnectionStates[link.Area] =
                    CentralDataAreaConnectionState.Offline;
            }
            UpdateCentralDataLinkStatuses();
            CentralDataStatus =
                "The update was cancelled. The last successful snapshot remains available.";
            SetConnectionState(
                HasConfiguredLiveLink
                    ? CentralDataConnectionState.Offline
                    : CentralDataConnectionState.Cached,
                HasConfiguredLiveLink
                    ? "Material link · OFFLINE"
                    : "Live Data · cached",
                isAutomatic
                    ? "The automatic check was cancelled. Use Refresh link to try again."
                    : "The refresh was cancelled. Cached tables remain available.");
        }
        finally
        {
            IsRefreshingCentralData = false;
        }
    }

    partial void OnSelectedCopperChanged(CopperReference? value)
    {
        if (_isApplyingCentralData ||
            _isUpdatingConductorOptions ||
            value is null)
        {
            return;
        }

        _isUsingSavedMaterialValues = false;
        _isApplyingCentralData = true;
        try
        {
            ApplyCopperDefaults(value);
        }
        finally
        {
            _isApplyingCentralData = false;
        }

        UpdateQuotationDisplays();
        UpdateSingleCorePreviewAppearance();
        RecalculateIfReady();
    }

    partial void OnHasUnsavedChangesChanged(bool value) =>
        UpdateRevisionStatus();

    partial void OnHasValidationErrorsChanged(bool value) =>
        UpdateRevisionStatus();

    partial void OnSelectedConductorMaterialTypeChanged(string? value)
    {
        if (_isApplyingCentralData || _isUpdatingConductorOptions)
        {
            return;
        }

        _isUsingSavedMaterialValues = false;
        PopulateConductorOptions(SelectedCopper);
        UpdateSingleCorePreviewAppearance();
    }

    partial void OnConductorSelectionModeIndexChanged(int value)
    {
        if (_isApplyingCentralData || _isUpdatingConductorOptions)
        {
            return;
        }

        _isUsingSavedMaterialValues = false;
        PopulateConductorOptions(SelectedCopper);
    }

    partial void OnSelectedConductorSizeChanged(string? value)
    {
        if (_isApplyingCentralData || _isUpdatingConductorOptions)
        {
            return;
        }

        _isUsingSavedMaterialValues = false;
        PopulateConductorClassesAndMatches(SelectedCopper);
    }

    partial void OnSelectedConductorClassChanged(string? value)
    {
        if (_isApplyingCentralData || _isUpdatingConductorOptions)
        {
            return;
        }

        _isUsingSavedMaterialValues = false;
        PopulateConductorSuppliersAndMatches(SelectedCopper);
    }

    partial void OnSelectedConductorSupplierChanged(string? value)
    {
        if (_isApplyingCentralData || _isUpdatingConductorOptions)
        {
            return;
        }

        _isUsingSavedMaterialValues = false;
        PopulateConductorMatches(SelectedCopper);
    }

    partial void OnSelectedCompoundChanged(CompoundReference? value)
    {
        if (_isApplyingCentralData || value is null)
        {
            return;
        }

        _isUsingSavedMaterialValues = false;
        _isApplyingCentralData = true;
        try
        {
            ApplyCompoundDefaults(value);
        }
        finally
        {
            _isApplyingCentralData = false;
        }

        UpdateQuotationDisplays();
        UpdateSingleCorePreviewAppearance();
        RecalculateIfReady();
    }

    partial void OnSelectedMasterbatchChanged(MasterbatchReference? value)
    {
        if (_isApplyingCentralData || value is null)
        {
            return;
        }

        _isUsingSavedMaterialValues = false;
        _isApplyingCentralData = true;
        try
        {
            ApplyMasterbatchDefaults(value);
        }
        finally
        {
            _isApplyingCentralData = false;
        }

        UpdateQuotationDisplays();
        UpdateSingleCorePreviewAppearance();
        RecalculateIfReady();
    }

    partial void OnSelectedCustomerContactChanged(ContactReference? value)
    {
        if (_isApplyingCentralData || value is null)
        {
            return;
        }

        CustomerName = value.AccountName;
        CustomerShortName = string.IsNullOrWhiteSpace(value.ShortName)
            ? value.AccountName
            : value.ShortName;
        DeliveryAddress = string.Join(
            Environment.NewLine,
            new[]
            {
                value.AddressLine1,
                value.AddressLine2,
                value.AddressLine3,
                value.AddressLine4,
                value.PostCode,
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    partial void OnSelectedOperatorChanged(OperatorReference? value)
    {
        if (_isApplyingCentralData || value is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ReviewApprovedBy))
        {
            ReviewApprovedBy = value.DisplayName;
        }
    }

    partial void OnSelectedQuoteCurrencyChanged(QuoteCurrencyOption? value) =>
        UpdateConvertedQuoteDisplay();

    partial void OnQuoteLengthMetresChanged(double value)
    {
        if (!_isUpdatingReelPlan &&
            QuoteReelCount > 0 &&
            double.IsFinite(value))
        {
            _isUpdatingReelPlan = true;
            try
            {
                QuoteMetresPerReel = value / QuoteReelCount;
            }
            finally
            {
                _isUpdatingReelPlan = false;
            }
        }

        UpdateQuoteReelPlan();
        RecalculateIfReady();
    }

    partial void OnQuoteReelCountChanged(double value) =>
        UpdateQuoteLengthFromReels();

    partial void OnQuoteMetresPerReelChanged(double value) =>
        UpdateQuoteLengthFromReels();

    partial void OnQuoteConductorDisplayModeIndexChanged(int value) =>
        UpdateQuotationDisplays();

    partial void OnUseExactCustomerColourNameChanged(bool value) =>
        UpdateQuotationDisplays();

    partial void OnUsageAllowancePercentChanged(double value) =>
        RecalculateIfReady();

    partial void OnRiskPercentChanged(double value) =>
        RecalculateIfReady();

    partial void OnMarkupPercentChanged(double value) =>
        RecalculateIfReady();

    partial void OnTargetMarginPercentChanged(double value) =>
        RecalculateIfReady();

    partial void OnUseManualLineSpeedChanged(bool value) =>
        RecalculateIfReady();

    partial void OnManualLineSpeedMetresPerHourChanged(double value) =>
        RecalculateIfReady();

    partial void OnProductionSetupTimeHoursChanged(double value) =>
        RecalculateIfReady();

    partial void OnProductionOperatorCountChanged(double value) =>
        RecalculateIfReady();

    partial void OnHourlyLabourRateChanged(double value) =>
        RecalculateIfReady();

    partial void OnConductorSupplierQuoteTotalChanged(double value)
    {
        UpdateSupplierUnitPriceDisplays();
        RecalculateIfReady();
    }

    partial void OnConductorSupplierQuotedKilogramsChanged(double value)
    {
        UpdateSupplierUnitPriceDisplays();
        RecalculateIfReady();
    }

    partial void OnConductorYieldMetresPerKilogramChanged(double value) =>
        RecalculateIfReady();

    partial void OnConductorOutsideDiameterMillimetresChanged(double value)
    {
        UpdateSingleCorePreviewAppearance();
        RecalculateIfReady();
    }

    partial void OnCompoundSupplierQuoteTotalChanged(double value)
    {
        UpdateSupplierUnitPriceDisplays();
        RecalculateIfReady();
    }

    partial void OnCompoundSupplierQuotedKilogramsChanged(double value)
    {
        UpdateSupplierUnitPriceDisplays();
        RecalculateIfReady();
    }

    partial void OnCompoundSpecificGravityChanged(double value) =>
        RecalculateIfReady();

    partial void OnNominalFinishedCoreOutsideDiameterMillimetresChanged(double value)
    {
        UpdateFinishedCoreOutsideDiameterRange();
        UpdateSingleCorePreviewAppearance();
        RecalculateIfReady();
    }

    partial void OnFinishedCoreOutsideDiameterToleranceMillimetresChanged(double value)
    {
        if (!UseSeparateNegativeOutsideDiameterTolerance)
        {
            FinishedCoreNegativeOutsideDiameterToleranceMillimetres = value;
        }

        UpdateFinishedCoreOutsideDiameterRange();
        UpdateSingleCorePreviewAppearance();
        RecalculateIfReady();
    }

    partial void OnUseSeparateNegativeOutsideDiameterToleranceChanged(bool value)
    {
        if (!value)
        {
            FinishedCoreNegativeOutsideDiameterToleranceMillimetres =
                FinishedCoreOutsideDiameterToleranceMillimetres;
        }

        UpdateFinishedCoreOutsideDiameterRange();
    }

    partial void OnFinishedCoreNegativeOutsideDiameterToleranceMillimetresChanged(
        double value) =>
        UpdateFinishedCoreOutsideDiameterRange();

    partial void OnHasCorePrintChanged(bool value) =>
        UpdateSingleCorePreviewAppearance();

    partial void OnCorePrintTextChanged(string value) =>
        UpdateSingleCorePreviewAppearance();

    partial void OnCorePrintColourHexChanged(string value) =>
        UpdateSingleCorePreviewAppearance();

    partial void OnCorePrintHeightMillimetresChanged(double value) =>
        UpdateSingleCorePreviewAppearance();

    partial void OnCorePrintRepeatDistanceMillimetresChanged(double value) =>
        UpdateSingleCorePreviewAppearance();

    partial void OnCorePrintDotPitchHorizontalMillimetresChanged(double value) =>
        UpdateSingleCorePreviewAppearance();

    partial void OnCorePrintDotPitchVerticalMillimetresChanged(double value) =>
        UpdateSingleCorePreviewAppearance();

    partial void OnMasterbatchSupplierQuoteTotalChanged(double value)
    {
        UpdateSupplierUnitPriceDisplays();
        RecalculateIfReady();
    }

    partial void OnMasterbatchSupplierQuotedKilogramsChanged(double value)
    {
        UpdateSupplierUnitPriceDisplays();
        RecalculateIfReady();
    }

    partial void OnMasterbatchAdditionPercentChanged(double value) =>
        RecalculateIfReady();

    partial void OnCustomerShortNameChanged(string value) =>
        UpdateCoreName();

    partial void OnIsCustomerSpecialChanged(bool value) =>
        UpdateCoreName();

    partial void OnUseCustomCoreNameChanged(bool value) =>
        UpdateCoreName();

    partial void OnCustomCoreNameChanged(string value) =>
        UpdateCoreName();

    private void ApplyCentralDataState(
        CentralDataState state,
        bool preserveSelections = false)
    {
        var copperId = preserveSelections ? SelectedCopper?.Id : null;
        var compoundId = preserveSelections ? SelectedCompound?.Id : null;
        var masterbatchCode = preserveSelections
            ? SelectedMasterbatch?.ColourCode
            : null;
        var contactId = preserveSelections
            ? SelectedCustomerContact?.Id
            : null;
        var operatorId = preserveSelections
            ? SelectedOperator?.Id
            : null;
        var retainedConductorQuoteTotal = ConductorSupplierQuoteTotal;
        var retainedConductorQuotedKilograms =
            ConductorSupplierQuotedKilograms;
        var retainedCompoundQuoteTotal = CompoundSupplierQuoteTotal;
        var retainedCompoundQuotedKilograms =
            CompoundSupplierQuotedKilograms;
        var retainedMasterbatchQuoteTotal = MasterbatchSupplierQuoteTotal;
        var retainedMasterbatchQuotedKilograms =
            MasterbatchSupplierQuotedKilograms;
        var retainedConductorYield = ConductorYieldMetresPerKilogram;
        var retainedConductorOutsideDiameter =
            ConductorOutsideDiameterMillimetres;
        var retainedCompoundSpecificGravity = CompoundSpecificGravity;

        _isApplyingCentralData = true;
        try
        {
            CopperTableRows = state.Snapshot.Copper
                .OrderBy(item => item.DisplayDescription)
                .ThenBy(item => item.Supplier)
                .ToArray();
            CompoundTableRows = state.Snapshot.Compounds
                .OrderBy(item => item.CompoundName)
                .ThenBy(item => item.Supplier)
                .ToArray();
            MasterbatchTableRows = state.Snapshot.Masterbatches
                .OrderBy(item => item.ColourName)
                .ThenBy(item => item.Supplier)
                .ToArray();
            RetainedSourceTables = state.EffectiveRetainedTables
                .OrderBy(item => item.Area)
                .ToArray();
            _allCopperMaterials = CopperTableRows
                .Where(item => item.IsSelectableForCosting)
                .ToArray();
            ConductorMaterialTypeOptions = _allCopperMaterials
                .Select(item => item.MaterialTypeDisplay)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(MaterialTypeSortOrder)
                .ThenBy(item => item)
                .ToArray();
            CompoundMaterials = CompoundTableRows
                .Where(item => item.IsCostingReady)
                .ToArray();
            MasterbatchMaterials = MasterbatchTableRows
                .Where(item =>
                    !string.IsNullOrWhiteSpace(item.ColourCode) &&
                    !string.IsNullOrWhiteSpace(item.ColourName))
                .ToArray();
            MasterbatchColourTypeOptions = MasterbatchMaterials
                .Select(item => item.ColourType?.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .ToArray();
            ContactMaterials = state.Snapshot.EffectiveContacts
                .OrderBy(item => item.AccountName)
                .ToArray();
            OperatorMaterials = state.Snapshot.EffectiveOperators
                .Where(item => item.Office)
                .OrderBy(item => item.DisplayName)
                .ToArray();

            var preferredCopper =
                _allCopperMaterials.FirstOrDefault(item => item.Id == copperId) ??
                _allCopperMaterials.FirstOrDefault(
                    item =>
                        item.Description.StartsWith(
                            "7/0.196 TCW (H)",
                            StringComparison.OrdinalIgnoreCase) &&
                        item.Supplier.Contains(
                            "Hayo",
                            StringComparison.OrdinalIgnoreCase)) ??
                _allCopperMaterials.FirstOrDefault();
            SelectedConductorMaterialType =
                ConductorMaterialTypeOptions.FirstOrDefault(
                    item => string.Equals(
                        item,
                        preferredCopper?.MaterialTypeDisplay,
                        StringComparison.OrdinalIgnoreCase)) ??
                ConductorMaterialTypeOptions.FirstOrDefault();
            PopulateConductorOptions(preferredCopper);
            SelectedCompound =
                CompoundMaterials.FirstOrDefault(item => item.Id == compoundId) ??
                CompoundMaterials.FirstOrDefault(
                    item => item.CompoundName == "FC1530CSI (XN78927)") ??
                CompoundMaterials.FirstOrDefault();
            SelectedMasterbatch =
                MasterbatchMaterials.FirstOrDefault(
                    item => item.ColourCode == masterbatchCode) ??
                MasterbatchMaterials.FirstOrDefault(
                    item => item.ColourCode == "CUS3872") ??
                MasterbatchMaterials.FirstOrDefault();
            SelectedCustomerContact = ContactMaterials.FirstOrDefault(
                item => item.Id == contactId);
            SelectedOperator =
                OperatorMaterials.FirstOrDefault(item => item.Id == operatorId) ??
                FindDefaultOfficeOperator(OperatorMaterials);
            if (string.IsNullOrWhiteSpace(ReviewApprovedBy) &&
                SelectedOperator is not null)
            {
                ReviewApprovedBy = SelectedOperator.DisplayName;
            }

            if (SelectedCopper is not null)
            {
                ApplyCopperDefaults(SelectedCopper);
            }

            if (SelectedCompound is not null)
            {
                ApplyCompoundDefaults(SelectedCompound);
            }

            if (SelectedMasterbatch is not null)
            {
                ApplyMasterbatchDefaults(SelectedMasterbatch);
            }

            if (preserveSelections)
            {
                ConductorSupplierQuoteTotal = retainedConductorQuoteTotal;
                ConductorSupplierQuotedKilograms =
                    retainedConductorQuotedKilograms;
                CompoundSupplierQuoteTotal = retainedCompoundQuoteTotal;
                CompoundSupplierQuotedKilograms =
                    retainedCompoundQuotedKilograms;
                MasterbatchSupplierQuoteTotal =
                    retainedMasterbatchQuoteTotal;
                MasterbatchSupplierQuotedKilograms =
                    retainedMasterbatchQuotedKilograms;

                if (_isUsingSavedMaterialValues)
                {
                    ConductorYieldMetresPerKilogram =
                        retainedConductorYield;
                    ConductorOutsideDiameterMillimetres =
                        retainedConductorOutsideDiameter;
                    CompoundSpecificGravity =
                        retainedCompoundSpecificGravity;
                }

                UpdateSupplierUnitPriceDisplays();
            }

            var hasRetainedData =
                state.Snapshot.Copper.Count > 0 ||
                state.Snapshot.Compounds.Count > 0 ||
                state.Snapshot.Masterbatches.Count > 0 ||
                state.Snapshot.EffectiveContacts.Count > 0 ||
                state.Snapshot.EffectiveOperators.Count > 0;
            _centralDataRevision = state.Snapshot.Revision;
            CentralDataSourceDisplay = hasRetainedData
                ? state.Snapshot.SourceLabel
                : "No central data linked";
            CentralDataUpdatedDisplay = hasRetainedData
                ? state.Snapshot.CapturedAt.ToLocalTime().ToString(
                    "dd MMM yyyy HH:mm",
                    CultureInfo.CurrentCulture)
                : "Not imported";
            CentralDataCountsDisplay =
                $"{state.Snapshot.Copper.Count:N0} copper · " +
                $"{state.Snapshot.Compounds.Count:N0} compounds · " +
                $"{state.Snapshot.Masterbatches.Count:N0} masterbatches · " +
                $"{state.Snapshot.EffectiveContacts.Count:N0} contacts · " +
                $"{state.Snapshot.EffectiveOperators.Count:N0} operators";

            DatabaseTableLinks = state.EffectiveTableLinks;
            UpdateCentralDataLinkStatuses();
            CentralDataStatus = DatabaseTableLinks.Count > 0
                ? $"{DatabaseTableLinks.Count} database table link(s) configured. " +
                  "The last successfully imported local tables remain active."
                : RetainedSourceTables.Count > 0
                    ? "Using retained local tables. No Access or SQL refresh link is configured."
                    : "No central data is installed. Link and import the required Access or SQL tables to begin costing.";
            if (DatabaseTableLinks.Count == 0)
            {
                SetConnectionState(
                    CentralDataConnectionState.Cached,
                    RetainedSourceTables.Count > 0
                        ? "Live Data · cached"
                        : "Live Data · setup required",
                    RetainedSourceTables.Count > 0
                        ? "Previously imported tables and manual project save remain available offline. Configure a link to enable refresh."
                        : "No customer or material tables are installed. Use Live Data setup to import the five required tables.");
            }
        }
        finally
        {
            _isApplyingCentralData = false;
        }

        UpdateQuotationDisplays();
        UpdateSingleCorePreviewAppearance();
    }

    private void UpdateSingleCorePreviewAppearance()
    {
        const double crossSectionOuterPixels = 176d;
        const double sideProfileOuterPixels = 82d;
        var conductorDiameter =
            double.IsFinite(ConductorOutsideDiameterMillimetres)
                ? Math.Max(0d, ConductorOutsideDiameterMillimetres)
                : 0d;
        var nominalFinishedDiameter =
            double.IsFinite(NominalFinishedCoreOutsideDiameterMillimetres)
                ? Math.Max(0d, NominalFinishedCoreOutsideDiameterMillimetres)
                : 0d;
        var positiveTolerance =
            double.IsFinite(FinishedCoreOutsideDiameterToleranceMillimetres)
                ? Math.Max(
                    0d,
                    FinishedCoreOutsideDiameterToleranceMillimetres)
                : 0d;
        var finishedDiameter =
            Math.Max(conductorDiameter, nominalFinishedDiameter);
        var ratio = finishedDiameter <= 0d
            ? 0.5d
            : Math.Clamp(conductorDiameter / finishedDiameter, 0.02d, 1d);

        PreviewConductorDiameterPixels =
            crossSectionOuterPixels * ratio;
        PreviewSideConductorHeightPixels =
            sideProfileOuterPixels * ratio;
        PreviewToleranceDiameterPixels =
            finishedDiameter <= 0d
                ? crossSectionOuterPixels
                : Math.Clamp(
                    crossSectionOuterPixels *
                    ((finishedDiameter +
                          positiveTolerance) /
                     finishedDiameter),
                    crossSectionOuterPixels,
                    208d);
        PreviewInsulationColourHex =
            string.IsNullOrWhiteSpace(SelectedMasterbatch?.ColourHex)
                ? "#6C7A89"
                : SelectedMasterbatch.ColourHex!;
        PreviewConductorColourHex =
            SelectedCopper?.MaterialTypeCode switch
            {
                "TCW" or "TI" or "SILVER" or "STAINLESS" => "#C8D0D6",
                "H72" => "#B07A3C",
                "TINSEL" => "#D6A33C",
                _ => "#C7782E",
            };
        PreviewDimensionsDisplay =
            finishedDiameter <= 0d
                ? "Waiting for conductor and finished-core dimensions"
                : $"{conductorDiameter:N3} mm conductor inside " +
                  $"{finishedDiameter:N3} mm finished core";
        PreviewMaterialDisplay =
            $"{SelectedCopper?.MaterialTypeDisplay ?? "Conductor"} · " +
            $"{SelectedMasterbatch?.ColourName ?? "No insulation colour selected"}";
        var construction = SelectedCopper?.Construction;
        PreviewStrandDetailDisplay = construction is null
            ? "Supplier-defined construction cannot be expanded into strands."
            : construction.IsRopeLay
                ? $"{construction.GroupCount:N0} groups × " +
                  $"{construction.StrandsPerGroup:N0} strands · " +
                  $"{construction.StrandDiameterMillimetres:N3} mm strand diameter"
                : $"{construction.TotalStrandCount:N0} strands · " +
                  $"{construction.StrandDiameterMillimetres:N3} mm strand diameter";

        if (construction is not null)
        {
            PreviewStrandDetailDisplay = construction.TotalStrandCount == 16 &&
                                         !construction.IsRopeLay
                ? $"16 strands; {construction.StrandDiameterMillimetres:N3} mm " +
                  "diameter; 5-strand centre with 11-strand outer layer"
                : construction.IsRopeLay
                ? $"{string.Join(" x ", construction.PackingLevels)} rope hierarchy = " +
                  $"{construction.TotalStrandCount:N0} strands; " +
                  $"{construction.StrandDiameterMillimetres:N3} mm diameter; " +
                  "compact hexagonal group packing"
                : $"{construction.TotalStrandCount:N0} strands; " +
                  $"{construction.StrandDiameterMillimetres:N3} mm diameter; " +
                  "compact hexagonal packing";
        }

        if (!double.IsFinite(ConductorOutsideDiameterMillimetres) ||
            !double.IsFinite(NominalFinishedCoreOutsideDiameterMillimetres))
        {
            PreviewCalculatedRadialWallDisplay = "—";
            PreviewReferenceWallDisplay = "—";
            PreviewWallAssessmentDisplay =
                "Choose complete dimensions to compare the wall.";
            PreviewWallSourceDisplay = "No comparator selected";
            PreviewWallSourceUrl = string.Empty;
            PreviewWallStatusColourHex = "#8A6D1D";
        }
        else
        {
            var wallGuidance = SingleCoreWallGuidance.Compare(
                (decimal)conductorDiameter,
                (decimal)nominalFinishedDiameter,
                SelectedCopper?.NominalAreaSquareMillimetres ?? 0m,
                $"{SelectedCompound?.MaterialType} {SelectedCompound?.Description}");
            PreviewCalculatedRadialWallDisplay =
                $"{wallGuidance.CalculatedRadialWallMillimetres:N3} mm";
            PreviewReferenceWallDisplay =
                $"{wallGuidance.ReferenceWallMillimetres:N3} mm " +
                (wallGuidance.IsDirectNominalSizeMatch
                    ? wallGuidance.ReferenceKind ==
                      WallReferenceKind.PublishedMinimum
                        ? "published minimum"
                        : "published nominal reference"
                    : $"nearest {wallGuidance.ReferenceNominalAreaSquareMillimetres:N3} mm² comparator");
            PreviewWallAssessmentDisplay = wallGuidance.Assessment;
            PreviewWallSourceDisplay =
                $"{wallGuidance.MaterialFamily} · {wallGuidance.SourceLabel}";
            PreviewWallSourceUrl = wallGuidance.SourceUrl;
            PreviewWallStatusColourHex = !wallGuidance.IsGeometryValid
                ? "#A4262C"
                : !wallGuidance.IsDirectNominalSizeMatch
                    ? "#8A6D1D"
                    : wallGuidance.MeetsReferenceWall
                        ? "#107C10"
                        : "#A4262C";
        }

        PreviewPrintTextDisplay =
            string.IsNullOrWhiteSpace(CorePrintText)
                ? "CORE PRINT"
                : CorePrintText.Trim();
        PreviewPrintOpacity = HasCorePrint ? 1d : 0d;
        const double printCylinderHeightPixels = 56d;
        var printHeightMillimetres =
            double.IsFinite(CorePrintHeightMillimetres)
                ? Math.Max(0d, CorePrintHeightMillimetres)
                : 0d;
        PreviewPrintFontSizePixels =
            finishedDiameter <= 0d
                ? 11d
                : Math.Clamp(
                    printHeightMillimetres /
                    finishedDiameter *
                    printCylinderHeightPixels,
                    3d,
                    printCylinderHeightPixels - 6d);
        PreviewPrintTextTopPixels =
            44d - PreviewPrintFontSizePixels * 0.72d;
        var repeatDistanceMillimetres =
            double.IsFinite(CorePrintRepeatDistanceMillimetres)
                ? Math.Max(0d, CorePrintRepeatDistanceMillimetres)
                : 0d;
        const double maximumRepeatPixels = 5000d;
        var axialPixelsPerMillimetre =
            repeatDistanceMillimetres <= maximumRepeatPixels ||
            repeatDistanceMillimetres <= 0d
                ? 1d
                : maximumRepeatPixels / repeatDistanceMillimetres;
        var repeatDistancePixels =
            repeatDistanceMillimetres * axialPixelsPerMillimetre;
        PreviewPrintSecondTextLeftPixels = 34d + repeatDistancePixels;
        PreviewPrintDimensionEndPixels = PreviewPrintSecondTextLeftPixels;
        PreviewPrintCanvasWidthPixels =
            Math.Max(500d, repeatDistancePixels + 330d);
        PreviewPrintCylinderWidthPixels =
            PreviewPrintCanvasWidthPixels - 36d;
        PreviewPrintRightFaceLeftPixels =
            PreviewPrintCanvasWidthPixels - 32d;
        PreviewPrintScaleDisplay =
            axialPixelsPerMillimetre >= 0.999999d
                ? "Axial scale: 1 px = 1 mm. " +
                  "Character height is scaled against finished cable OD."
                : $"Axial scale: 1 px = " +
                  $"{1d / axialPixelsPerMillimetre:N1} mm. " +
                  "Character height is scaled against finished cable OD.";
        PreviewPrintSpecificationDisplay = HasCorePrint
            ? $"{CorePrintHeightMillimetres:N3} mm high · " +
              $"{CorePrintRepeatDistanceMillimetres:N1} mm start-to-start · " +
              $"{CorePrintDotPitchHorizontalMillimetres:N3} × " +
              $"{CorePrintDotPitchVerticalMillimetres:N3} mm dot pitch"
            : "Print disabled";
    }

    private void ApplyCopperDefaults(CopperReference value)
    {
        if (value.Id == "860")
        {
            ConductorSupplierQuoteTotal = 10398.41;
            ConductorSupplierQuotedKilograms = 1000;
        }
        else
        {
            ConductorSupplierQuoteTotal = (double)value.PricePerKilogram;
            ConductorSupplierQuotedKilograms = 1;
        }

        ConductorYieldMetresPerKilogram =
            (double)value.YieldMetresPerKilogram;
        ConductorOutsideDiameterMillimetres =
            (double)value.NominalOutsideDiameterMillimetres;
        UpdateConductorDetails(value);
        UpdateSupplierUnitPriceDisplays();
    }

    private void ApplyCompoundDefaults(CompoundReference value)
    {
        // The COR1 quote sheet records £1.630/kg for the reference quote while
        // the current central table row is £1.620/kg. Keep the quote input used
        // by the reference working example visible and editable; the table view
        // continues to show the retained central value separately.
        CompoundSupplierQuoteTotal =
            value.Id == "63" ? 1.63 : (double)value.PricePerKilogram;
        CompoundSupplierQuotedKilograms = 1;
        CompoundSpecificGravity = (double)value.SpecificGravity;
        CompoundDetailsDisplay =
            $"{value.Supplier} · {value.MaterialType} · {value.Description} · " +
            $"specific gravity {value.SpecificGravity:0.###}" +
            (value.HasDataSheet ? " · data sheet recorded" : "");
        UpdateSupplierUnitPriceDisplays();
        UpdateMasterbatchCompatibility();
    }

    private void ApplyMasterbatchDefaults(MasterbatchReference value)
    {
        MasterbatchSupplierQuoteTotal = (double)value.PricePerKilogram;
        MasterbatchSupplierQuotedKilograms = 1;
        MasterbatchDetailsDisplay =
            $"{value.Supplier} · {value.ColourType} · " +
            $"{value.Compatibility}" +
            (string.IsNullOrWhiteSpace(value.RalEquivalent)
                ? ""
                : $" · RAL {value.RalEquivalent}") +
            (string.IsNullOrWhiteSpace(value.TemperatureLimits)
                ? ""
                : $" · {value.TemperatureLimits}");
        UpdateSupplierUnitPriceDisplays();
        UpdateMasterbatchCompatibility();
    }

    private void UpdateMasterbatchCompatibility()
    {
        MasterbatchCompatibilityRows.Clear();
        if (SelectedMasterbatch is null)
        {
            MasterbatchCompatibilityStatus =
                "Choose a masterbatch colour to see compatibility.";
            return;
        }

        var compatibilityCells = SelectedMasterbatch.CompatibilityCells;
        foreach (var cell in compatibilityCells)
        {
            MasterbatchCompatibilityRows.Add(
                new MasterbatchCompatibilityRow(
                    cell.MaterialFamily,
                    cell.IsCompatible,
                    cell.IsRecorded,
                    cell.TemperatureDisplay));
        }

        var compoundFamily = SelectedCompound is null
            ? null
            : CompoundCompatibilityFamily(SelectedCompound.MaterialType);
        if (compoundFamily is null)
        {
            MasterbatchCompatibilityStatus =
                "The selected compound family is not mapped to one of the eight workbook compatibility groups. Review the supplier data before use.";
            return;
        }

        var selectedFamily = compatibilityCells.FirstOrDefault(item =>
            string.Equals(
                item.MaterialFamily,
                compoundFamily,
                StringComparison.OrdinalIgnoreCase));
        if (selectedFamily?.IsCompatible == true)
        {
            MasterbatchCompatibilityStatus =
                $"Compatible: {SelectedMasterbatch.ColourName} lists {compoundFamily} " +
                $"for {SelectedCompound!.CompoundName}.";
        }
        else if (selectedFamily?.IsRecorded != true)
        {
            MasterbatchCompatibilityStatus =
                "Compatibility is not recorded for this colour. Supplier confirmation is required before use.";
        }
        else
        {
            MasterbatchCompatibilityStatus =
                $"Not listed as compatible: {SelectedMasterbatch.ColourName} does not list " +
                $"{compoundFamily} for {SelectedCompound!.CompoundName}.";
        }
    }

    private static string? CompoundCompatibilityFamily(string materialType)
    {
        var upper = materialType.Trim().ToUpperInvariant();
        if (upper.StartsWith("PVC", StringComparison.Ordinal))
        {
            return "PVC";
        }
        if (upper.StartsWith("PE", StringComparison.Ordinal) ||
            upper.StartsWith("PP", StringComparison.Ordinal) ||
            upper.StartsWith("PUR", StringComparison.Ordinal) ||
            upper.StartsWith("XLPE", StringComparison.Ordinal))
        {
            return "PE/PP/PUR";
        }
        if (upper.StartsWith("PS", StringComparison.Ordinal))
        {
            return "PS";
        }
        if (upper.StartsWith("ABS", StringComparison.Ordinal))
        {
            return "ABS";
        }
        if (upper.StartsWith("ACETAL", StringComparison.Ordinal))
        {
            return "ACETAL";
        }
        if (upper.StartsWith("PBT", StringComparison.Ordinal))
        {
            return "PBT";
        }
        if (upper.StartsWith("NYLON", StringComparison.Ordinal) ||
            upper.StartsWith("PA", StringComparison.Ordinal))
        {
            return "NYLON";
        }
        if (upper.StartsWith("PC", StringComparison.Ordinal) ||
            upper.StartsWith("PES", StringComparison.Ordinal))
        {
            return "PC/PES";
        }

        return null;
    }

    private void PopulateConductorOptions(CopperReference? preferred)
    {
        _isUpdatingConductorOptions = true;
        try
        {
            var materialTypeRows = MatchingConductorMaterialType().ToArray();
            IsConductorGeometrySelectionAvailable =
                materialTypeRows.Any(item => item.Construction is not null);
            ConductorSizeHeader = IsConductorGeometrySelectionAvailable
                ? ConductorSelectionModeIndex switch
                {
                    1 => "Nominal cross-section (mm²)",
                    2 => "Calculated AWG equivalent",
                    _ => "Strand construction",
                }
                : "Supplier-defined description";
            ConductorSizeOptions = materialTypeRows
                .Select(ConductorSizeKey)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => ConductorSizeSortKey(value))
                .ThenBy(value => value)
                .ToArray();

            var preferredSize = preferred is null
                ? null
                : ConductorSizeKey(preferred);
            SelectedConductorSize =
                ConductorSizeOptions.FirstOrDefault(
                    value => string.Equals(
                        value,
                        preferredSize,
                        StringComparison.OrdinalIgnoreCase)) ??
                ConductorSizeOptions.FirstOrDefault();
        }
        finally
        {
            _isUpdatingConductorOptions = false;
        }

        PopulateConductorClassesAndMatches(preferred);
    }

    private void PopulateConductorClassesAndMatches(CopperReference? preferred)
    {
        _isUpdatingConductorOptions = true;
        try
        {
            var matchingSize = MatchingConductorSize().ToArray();
            ConductorClassOptions = matchingSize
                .Select(ConductorClassKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToArray();
            var preferredClass = preferred is null
                ? null
                : ConductorClassKey(preferred);
            SelectedConductorClass =
                ConductorClassOptions.FirstOrDefault(
                    value => string.Equals(
                        value,
                        preferredClass,
                        StringComparison.OrdinalIgnoreCase)) ??
                ConductorClassOptions.FirstOrDefault();
        }
        finally
        {
            _isUpdatingConductorOptions = false;
        }

        PopulateConductorSuppliersAndMatches(preferred);
    }

    private void PopulateConductorSuppliersAndMatches(CopperReference? preferred)
    {
        _isUpdatingConductorOptions = true;
        try
        {
            var matchingClass = MatchingConductorClass().ToArray();
            ConductorSupplierOptions = matchingClass
                .Select(item => item.Supplier)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToArray();
            SelectedConductorSupplier =
                ConductorSupplierOptions.FirstOrDefault(
                    value => string.Equals(
                        value,
                        preferred?.Supplier,
                        StringComparison.OrdinalIgnoreCase)) ??
                ConductorSupplierOptions.FirstOrDefault();
        }
        finally
        {
            _isUpdatingConductorOptions = false;
        }

        PopulateConductorMatches(preferred);
    }

    private void PopulateConductorMatches(CopperReference? preferred)
    {
        _isUpdatingConductorOptions = true;
        try
        {
            CopperMaterials = MatchingConductorClass()
                .Where(
                    item => string.Equals(
                        item.Supplier,
                        SelectedConductorSupplier,
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.DisplayDescription)
                .ToArray();
            SelectedCopper =
                CopperMaterials.FirstOrDefault(item => item.Id == preferred?.Id) ??
                CopperMaterials.FirstOrDefault();
            if (SelectedCopper is not null)
            {
                ApplyCopperDefaults(SelectedCopper);
            }
        }
        finally
        {
            _isUpdatingConductorOptions = false;
        }

        RecalculateIfReady();
    }

    private IEnumerable<CopperReference> MatchingConductorSize() =>
        MatchingConductorMaterialType().Where(
            item => string.Equals(
                ConductorSizeKey(item),
                SelectedConductorSize,
                StringComparison.OrdinalIgnoreCase));

    private IEnumerable<CopperReference> MatchingConductorMaterialType() =>
        _allCopperMaterials.Where(
            item => string.Equals(
                item.MaterialTypeDisplay,
                SelectedConductorMaterialType,
                StringComparison.OrdinalIgnoreCase));

    private IEnumerable<CopperReference> MatchingConductorClass() =>
        MatchingConductorSize().Where(
            item => string.Equals(
                ConductorClassKey(item),
                SelectedConductorClass,
                StringComparison.OrdinalIgnoreCase));

    private string ConductorSizeKey(CopperReference item) =>
        !IsConductorGeometrySelectionAvailable
            ? item.Description
            : ConductorSelectionModeIndex switch
        {
            1 => $"{item.NominalAreaSquareMillimetres:0.###} mm²",
            2 => item.Construction is null
                ? item.WorkbookAwg ?? "AWG not recorded"
                : $"AWG {item.Construction.NearestAwg}",
            _ => item.Construction?.NormalizedConstruction ??
                 item.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                     .FirstOrDefault() ??
                 item.Description,
        };

    private static string ConductorClassKey(CopperReference item) =>
        item.Construction?.ConductorClassDisplay ??
        "Supplier-defined special conductor";

    private static int MaterialTypeSortOrder(string materialType) =>
        materialType.StartsWith("TCW", StringComparison.OrdinalIgnoreCase)
            ? 0
            : materialType.StartsWith("PCW", StringComparison.OrdinalIgnoreCase)
                ? 1
                : materialType.StartsWith("TI ", StringComparison.OrdinalIgnoreCase)
                    ? 2
                    : 3;

    private static decimal ConductorSizeSortKey(string value)
    {
        var numeric = new string(
            value.TakeWhile(
                character =>
                    char.IsDigit(character) ||
                    character is '.' or '/' or '-')
                .ToArray());
        return decimal.TryParse(
            numeric,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : decimal.MaxValue;
    }

    private void UpdateConductorDetails(CopperReference value)
    {
        var construction = value.Construction;
        ConductorSupplierDisplay = value.Supplier;
        ConductorConstructionDisplay =
            construction?.NormalizedConstruction ?? value.Description;
        ConductorNominalAreaDisplay =
            value.NominalAreaSquareMillimetres > 0
                ? value.NominalAreaDisplay
                : "Not recorded";
        ConductorCalculatedAreaDisplay = construction is null
            ? "Construction could not be parsed"
            : $"{construction.CalculatedMetalAreaSquareMillimetres:0.###} mm² " +
              $"from {construction.TotalStrandCount:N0} × " +
              $"π × {construction.StrandDiameterMillimetres:0.###}² ÷ 4";
        ConductorAwgDisplay = construction is null
            ? value.WorkbookAwg ?? "Not recorded"
            : $"AWG {construction.NearestAwg} calculated" +
              (string.IsNullOrWhiteSpace(value.WorkbookAwg)
                  ? ""
                  : $" · workbook AWG {value.WorkbookAwg}");
        ConductorClassDisplay =
            construction?.ConductorClassDisplay ?? "Class not established";
        ConductorClassReasonDisplay =
            construction?.ClassReason ??
            "Select a conductor with a recognised strand construction.";
        ConductorVerificationMessage =
            construction?.AreaVerificationMessage ?? string.Empty;
        if (value.HasDerivedValues)
        {
            ConductorVerificationMessage =
                $"{ConductorVerificationMessage} Retained-data calculations: " +
                value.DerivationSummary;
        }
        if (value.NominalOutsideDiameterMillimetres <= 0m)
        {
            ConductorVerificationMessage =
                $"{ConductorVerificationMessage} The linked record has no nominal " +
                "conductor OD. It remains selectable for traceability, but the " +
                "costing result stays blocked until Nom OD is mapped and imported.";
        }

        if (value.PricePerKilogram <= 0m)
        {
            ConductorVerificationMessage =
                $"{ConductorVerificationMessage} No database price is stored; enter " +
                "the supplier quote total and quoted kilograms for this costing.";
        }

        ConductorVerificationMessage = ConductorVerificationMessage.Trim();
        IsConductorVerificationOpen =
            construction?.RequiresAreaReview == true ||
            value.HasEstimatedValues ||
            value.NominalOutsideDiameterMillimetres <= 0m ||
            value.PricePerKilogram <= 0m;
    }

    private static OperatorReference? FindDefaultOfficeOperator(
        IReadOnlyList<OperatorReference> operators) =>
        operators.FirstOrDefault(
            item => string.Equals(
                item.FirstName,
                "Laura",
                StringComparison.OrdinalIgnoreCase)) ??
        operators.FirstOrDefault();

    private void SetConnectionState(
        CentralDataConnectionState state,
        string display,
        string detail)
    {
        ConnectionState = state;
        CentralDataConnectionDisplay = display;
        CentralDataConnectionDetail = detail;
    }

    private void UpdateSupplierUnitPriceDisplays()
    {
        ConductorPricePerKilogramDisplay = SupplierUnitPrice(
            ConductorSupplierQuoteTotal,
            ConductorSupplierQuotedKilograms);
        CompoundPricePerKilogramDisplay = SupplierUnitPrice(
            CompoundSupplierQuoteTotal,
            CompoundSupplierQuotedKilograms);
        MasterbatchPricePerKilogramDisplay = SupplierUnitPrice(
            MasterbatchSupplierQuoteTotal,
            MasterbatchSupplierQuotedKilograms);
    }

    private static string SupplierUnitPrice(double total, double kilograms)
    {
        if (!double.IsFinite(total) ||
            !double.IsFinite(kilograms) ||
            total < 0 ||
            kilograms <= 0)
        {
            return "Enter a valid quote and kg amount";
        }

        return string.Format(
            PoundCulture,
            "{0:C5}/kg",
            total / kilograms);
    }

    private void UpdateCoreName()
    {
        if (SelectedCopper is null || SelectedCompound is null)
        {
            GeneratedCoreNameDisplay = "—";
            EffectiveCoreNameDisplay = "—";
            return;
        }

        try
        {
            GeneratedCoreNameDisplay = CoreNameGenerator.Generate(
                new CoreNameInputs(
                    SelectedCopper.Description,
                    SelectedCompound.MaterialType,
                    IsCustomerSpecial,
                    CustomerShortName))
                .GeneratedName;
        }
        catch (ArgumentException)
        {
            GeneratedCoreNameDisplay =
                "Name unavailable for this conductor description";
        }

        EffectiveCoreNameDisplay =
            UseCustomCoreName && !string.IsNullOrWhiteSpace(CustomCoreName)
                ? CustomCoreName.Trim()
                : GeneratedCoreNameDisplay;
    }

    private void UpdateFinishedCoreOutsideDiameterRange()
    {
        var nominal = NominalFinishedCoreOutsideDiameterMillimetres;
        var negative = UseSeparateNegativeOutsideDiameterTolerance
            ? FinishedCoreNegativeOutsideDiameterToleranceMillimetres
            : FinishedCoreOutsideDiameterToleranceMillimetres;
        if (!double.IsFinite(nominal) ||
            !double.IsFinite(negative) ||
            !double.IsFinite(FinishedCoreOutsideDiameterToleranceMillimetres))
        {
            FinishedCoreOutsideDiameterRangeDisplay = "—";
            return;
        }

        var minimum = Math.Max(0d, nominal - negative);
        var maximum =
            nominal + FinishedCoreOutsideDiameterToleranceMillimetres;
        FinishedCoreOutsideDiameterRangeDisplay =
            $"{minimum:N3} mm to {maximum:N3} mm " +
            $"(-{negative:N3} / +{FinishedCoreOutsideDiameterToleranceMillimetres:N3})";
    }

    private void UpdateQuoteLengthFromReels()
    {
        if (_isUpdatingReelPlan)
        {
            return;
        }

        if (!double.IsFinite(QuoteReelCount) ||
            !double.IsFinite(QuoteMetresPerReel) ||
            QuoteReelCount <= 0 ||
            QuoteMetresPerReel <= 0)
        {
            QuoteReelPlanDisplay = AppRuntimeMode.IsPublicReview
                ? "—"
                : "Enter a positive reel count and metres per reel.";
            return;
        }

        _isUpdatingReelPlan = true;
        try
        {
            QuoteLengthMetres = QuoteReelCount * QuoteMetresPerReel;
        }
        finally
        {
            _isUpdatingReelPlan = false;
        }

        UpdateQuoteReelPlan();
        RecalculateIfReady();
    }

    private void UpdateQuoteReelPlan()
    {
        if (!double.IsFinite(QuoteReelCount) ||
            !double.IsFinite(QuoteMetresPerReel) ||
            QuoteReelCount <= 0 ||
            QuoteMetresPerReel <= 0)
        {
            QuoteReelPlanDisplay = AppRuntimeMode.IsPublicReview
                ? "—"
                : "Enter a positive reel count and metres per reel.";
            return;
        }

        QuoteReelPlanDisplay =
            $"{QuoteReelCount:N0} reels × {QuoteMetresPerReel:N0} m = " +
            $"{QuoteReelCount * QuoteMetresPerReel:N0} m quoted";
    }

    private void UpdateQuotationDisplays()
    {
        QuoteConductorDisplay = BuildQuotationConductorDisplay();
        QuoteInsulationDisplay = BuildQuotationInsulationDisplay();
        QuoteColourDisplay = BuildQuotationColourDisplay();
    }

    private string BuildQuotationConductorDisplay()
    {
        if (SelectedCopper is null)
        {
            return "—";
        }

        var constructionMatch = Regex.Match(
            SelectedCopper.DisplayDescription,
            @"(?<construction>\d+(?:[xX]\d+)?/\d+(?:\.\d+)?)\s*(?<finish>TCW|PCW|TI)?",
            RegexOptions.IgnoreCase);
        if (!constructionMatch.Success)
        {
            return SelectedCopper.DisplayDescription;
        }

        var construction =
            constructionMatch.Groups["construction"].Value;
        var finish = constructionMatch.Groups["finish"].Value;
        var concise = string.IsNullOrWhiteSpace(finish)
            ? construction
            : $"{construction} {finish.ToUpperInvariant()}";
        var nominalArea =
            SelectedCopper.NominalAreaSquareMillimetres > 0m
                ? $"{SelectedCopper.NominalAreaSquareMillimetres:0.###} mm²"
                : ConductorNominalAreaDisplay;
        var awg = ConductorAwgDisplay;

        return QuoteConductorDisplayModeIndex switch
        {
            1 => $"{concise} · {nominalArea}",
            2 => $"{concise} · {awg}",
            3 => $"{concise} · {awg} · {nominalArea}",
            _ => concise,
        };
    }

    private string BuildQuotationInsulationDisplay()
    {
        if (SelectedCompound is null)
        {
            return "—";
        }

        var source =
            $"{SelectedCompound.MaterialType} " +
            $"{SelectedCompound.Description} " +
            SelectedCompound.CompoundName;
        if (source.Contains("LS0H", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("LSZH", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("LSOH", StringComparison.OrdinalIgnoreCase))
        {
            return "LS0H/LSZH";
        }

        if (source.Contains("PVC", StringComparison.OrdinalIgnoreCase))
        {
            return "PVC";
        }

        if (source.Contains("XLPE", StringComparison.OrdinalIgnoreCase))
        {
            return "XLPE";
        }

        if (source.Contains("PUR", StringComparison.OrdinalIgnoreCase))
        {
            return "PUR";
        }

        if (source.Contains("PE", StringComparison.OrdinalIgnoreCase))
        {
            return "PE";
        }

        return string.IsNullOrWhiteSpace(SelectedCompound.MaterialType)
            ? SelectedCompound.Description
            : SelectedCompound.MaterialType;
    }

    private string BuildQuotationColourDisplay()
    {
        if (SelectedMasterbatch is null)
        {
            return "—";
        }

        if (UseExactCustomerColourName)
        {
            return
                $"{SelectedMasterbatch.ColourName} " +
                $"({SelectedMasterbatch.ColourCode})";
        }

        string[] genericColours =
        [
            "Black",
            "White",
            "Grey",
            "Gray",
            "Red",
            "Orange",
            "Yellow",
            "Green",
            "Blue",
            "Brown",
            "Purple",
            "Violet",
            "Pink",
            "Natural",
            "Clear",
            "Turquoise",
            "Cyan",
        ];
        var match = genericColours.FirstOrDefault(colour =>
            SelectedMasterbatch.ColourName.Contains(
                colour,
                StringComparison.OrdinalIgnoreCase));
        return match switch
        {
            "Gray" => "Grey",
            null => SelectedMasterbatch.ColourName,
            _ => match,
        };
    }

    private void UpdateConvertedQuoteDisplay()
    {
        var currency = SelectedQuoteCurrency ?? PreferredQuoteCurrencies[0];
        try
        {
            ConvertedQuoteDisplay = FormatCurrency(
                ConvertRecommendedPrice(_recommendedQuotePrice, currency.Code),
                currency);
        }
        catch (InvalidOperationException)
        {
            ConvertedQuoteDisplay =
                $"No retained {currency.Code} rate - refresh required";
        }
    }

    private decimal ConvertRecommendedPrice(decimal amount, string currencyCode)
    {
        if (string.Equals(
                currencyCode,
                "GBP",
                StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        if (_exchangeRateSnapshot is null)
        {
            throw new InvalidOperationException(
                $"No retained exchange rate is available for {currencyCode}.");
        }

        return _exchangeRateSnapshot.ConvertFromGbp(amount, currencyCode);
    }

    private SingleCoreCalculatedResultSnapshot CaptureCalculatedResult(
        SingleCoreCostingResult material,
        ProductionLabourResult labour,
        CommercialPricingResult commercial) =>
        new()
        {
            RecommendedQuotePrice = _recommendedQuotePrice,
            EffectiveCoreName = EffectiveCoreNameDisplay,
            GeneratedCoreName = GeneratedCoreNameDisplay,
            ConductorCostPerMetre = ConductorCostPerMetreDisplay,
            CompoundCostPerMetre = CompoundCostPerMetreDisplay,
            MasterbatchCostPerMetre = MasterbatchCostPerMetreDisplay,
            CoreMaterialCostPerMetre = CoreMaterialCostPerMetreDisplay,
            CoreMaterialQuote = CoreMaterialQuoteDisplay,
            RiskAdjustedCostPerMetre = RiskAdjustedCostPerMetreDisplay,
            RiskAdjustedQuote = RiskAdjustedQuoteDisplay,
            MarkedUpCostPerMetre = MarkedUpCostPerMetreDisplay,
            MarkedUpQuote = MarkedUpQuoteDisplay,
            RecommendedLineSpeed = RecommendedLineSpeedDisplay,
            EffectiveLineSpeed = EffectiveLineSpeedDisplay,
            ProductionRunningTime = ProductionRunningTimeDisplay,
            TotalProductionTime = TotalProductionTimeDisplay,
            ChargeableLabourHours = ChargeableLabourHoursDisplay,
            LabourCost = LabourCostDisplay,
            LabourCostPerMetre = LabourCostPerMetreDisplay,
            TotalEstimatedCost = TotalEstimatedCostDisplay,
            RiskValue = RiskValueDisplay,
            MarkupValue = MarkupValueDisplay,
            CombinedRatePrice = CombinedRatePriceDisplay,
            TargetMarginPrice = TargetMarginPriceDisplay,
            ConductorQuoteMass = ConductorQuoteMassDisplay,
            CompoundQuoteMass = CompoundQuoteMassDisplay,
            MasterbatchQuoteMass = MasterbatchQuoteMassDisplay,
            ConductorQuotePrice = ConductorQuotePriceDisplay,
            CompoundQuotePrice = CompoundQuotePriceDisplay,
            MasterbatchQuotePrice = MasterbatchQuotePriceDisplay,
            Trace =
            [
                SaveSection(
                    "conductor",
                    "Conductor",
                    material.Conductor.Steps),
                SaveSection(
                    "compound",
                    "Insulation compound",
                    material.Compound.Steps),
                SaveSection(
                    "masterbatch",
                    "Masterbatch",
                    material.Steps.Where(step => step.Id.StartsWith(
                        "masterbatch-",
                        StringComparison.Ordinal))),
                SaveSection(
                    "material",
                    "Material and core",
                    material.Steps.Where(
                        step => !IsMaterialCommercialStep(step))),
                SaveSection("labour", "Production and labour", labour.Steps),
                SaveSection(
                    "commercial",
                    "Commercial pricing",
                    commercial.Steps),
            ],
        };

    private void ApplyCalculatedResult(
        SingleCoreCalculatedResultSnapshot snapshot)
    {
        _currentCalculatedResult = snapshot;
        _recommendedQuotePrice = snapshot.RecommendedQuotePrice;
        EffectiveCoreNameDisplay = snapshot.EffectiveCoreName;
        GeneratedCoreNameDisplay = snapshot.GeneratedCoreName;
        ConductorCostPerMetreDisplay = snapshot.ConductorCostPerMetre;
        CompoundCostPerMetreDisplay = snapshot.CompoundCostPerMetre;
        MasterbatchCostPerMetreDisplay = snapshot.MasterbatchCostPerMetre;
        CoreMaterialCostPerMetreDisplay = snapshot.CoreMaterialCostPerMetre;
        CoreMaterialQuoteDisplay = snapshot.CoreMaterialQuote;
        RiskAdjustedCostPerMetreDisplay =
            snapshot.RiskAdjustedCostPerMetre;
        RiskAdjustedQuoteDisplay = snapshot.RiskAdjustedQuote;
        MarkedUpCostPerMetreDisplay = snapshot.MarkedUpCostPerMetre;
        MarkedUpQuoteDisplay = snapshot.MarkedUpQuote;
        RecommendedLineSpeedDisplay = snapshot.RecommendedLineSpeed;
        EffectiveLineSpeedDisplay = snapshot.EffectiveLineSpeed;
        ProductionRunningTimeDisplay = snapshot.ProductionRunningTime;
        TotalProductionTimeDisplay = snapshot.TotalProductionTime;
        ChargeableLabourHoursDisplay = snapshot.ChargeableLabourHours;
        LabourCostDisplay = snapshot.LabourCost;
        LabourCostPerMetreDisplay = snapshot.LabourCostPerMetre;
        TotalEstimatedCostDisplay = snapshot.TotalEstimatedCost;
        RiskValueDisplay = snapshot.RiskValue;
        MarkupValueDisplay = snapshot.MarkupValue;
        CombinedRatePriceDisplay = snapshot.CombinedRatePrice;
        TargetMarginPriceDisplay = snapshot.TargetMarginPrice;
        ConductorQuoteMassDisplay = snapshot.ConductorQuoteMass;
        CompoundQuoteMassDisplay = snapshot.CompoundQuoteMass;
        MasterbatchQuoteMassDisplay = snapshot.MasterbatchQuoteMass;
        ConductorQuotePriceDisplay = snapshot.ConductorQuotePrice;
        CompoundQuotePriceDisplay = snapshot.CompoundQuotePrice;
        MasterbatchQuotePriceDisplay = snapshot.MasterbatchQuotePrice;

        ReplaceRows(
            ConductorCalculationSteps,
            FindSavedSection(snapshot, "conductor"));
        ReplaceFlowStages(
            ConductorCalculationFlow,
            FindSavedSection(snapshot, "conductor"));
        ReplaceRows(
            CompoundCalculationSteps,
            FindSavedSection(snapshot, "compound"));
        ReplaceFlowStages(
            CompoundCalculationFlow,
            FindSavedSection(snapshot, "compound"));
        ReplaceRows(
            MasterbatchCalculationSteps,
            FindSavedSection(snapshot, "masterbatch"));
        ReplaceFlowStages(
            MasterbatchCalculationFlow,
            FindSavedSection(snapshot, "masterbatch"));
        ReplaceRows(
            LabourCalculationSteps,
            FindSavedSection(snapshot, "labour"));
        ReplaceFlowStages(
            LabourCalculationFlow,
            FindSavedSection(snapshot, "labour"));

        CalculationSteps.Clear();
        foreach (var sectionId in new[] { "material", "labour", "commercial" })
        {
            foreach (var step in FindSavedSection(snapshot, sectionId))
            {
                CalculationSteps.Add(ToRow(step));
            }
        }

        UpdateQuotationDisplays();
        UpdateQuoteReelPlan();
        UpdateConvertedQuoteDisplay();
    }

    private static SavedCalculationSection SaveSection(
        string id,
        string label,
        IEnumerable<CalculationStep> steps) =>
        new(
            id,
            label,
            steps.Select(ToSavedStep).ToArray());

    private static SavedCalculationStep ToSavedStep(CalculationStep step) =>
        new(
            step.Id,
            step.Label,
            step.Expression,
            step.SubstitutedExpression,
            step.RawValue,
            step.DisplayValue,
            step.Unit,
            step.InputSteps.Select(ToSavedStep).ToArray(),
            step.Warning,
            step.BusinessMeaning,
            step.RoundingRule,
            step.RuleVersion);

    private static IReadOnlyList<SavedCalculationStep> FindSavedSection(
        SingleCoreCalculatedResultSnapshot snapshot,
        string id) =>
        snapshot.Trace.FirstOrDefault(
            section => string.Equals(
                section.Id,
                id,
                StringComparison.Ordinal))?.Steps ?? [];

    private static CopperReference? CreateSavedCopperReference(
        SingleCoreProjectDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.CopperId) ||
            string.IsNullOrWhiteSpace(document.CopperDescription) ||
            document.ConductorYieldMetresPerKilogram <= 0 ||
            document.ConductorOutsideDiameterMillimetres <= 0)
        {
            return null;
        }

        return new CopperReference(
            document.CopperId,
            document.CopperDescription,
            document.CopperSupplier,
            SavedPricePerKilogram(
                document.ConductorSupplierQuoteTotal,
                document.ConductorSupplierQuotedKilograms),
            ToDecimal(document.ConductorYieldMetresPerKilogram),
            ToDecimal(document.ConductorOutsideDiameterMillimetres));
    }

    private static CompoundReference? CreateSavedCompoundReference(
        SingleCoreProjectDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.CompoundId) ||
            string.IsNullOrWhiteSpace(document.CompoundName) ||
            document.CompoundSpecificGravity <= 0)
        {
            return null;
        }

        return new CompoundReference(
            document.CompoundId,
            document.CompoundName,
            document.CompoundSupplier,
            SavedPricePerKilogram(
                document.CompoundSupplierQuoteTotal,
                document.CompoundSupplierQuotedKilograms),
            ToDecimal(document.CompoundSpecificGravity),
            "",
            document.CompoundName);
    }

    private static MasterbatchReference? CreateSavedMasterbatchReference(
        SingleCoreProjectDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.MasterbatchCode) ||
            string.IsNullOrWhiteSpace(document.MasterbatchName))
        {
            return null;
        }

        return new MasterbatchReference(
            document.MasterbatchCode,
            document.MasterbatchName,
            document.MasterbatchSupplier,
            SavedPricePerKilogram(
                document.MasterbatchSupplierQuoteTotal,
                document.MasterbatchSupplierQuotedKilograms),
            "");
    }

    private static decimal SavedPricePerKilogram(
        double total,
        double kilograms) =>
        double.IsFinite(total) &&
        double.IsFinite(kilograms) &&
        total >= 0 &&
        kilograms > 0
            ? ToDecimal(total) / ToDecimal(kilograms)
            : 0m;

    private void ApplyRevisionIdentity(SingleCoreProjectDocument document)
    {
        CurrentProjectId = document.ProjectId;
        CurrentRevisionId = document.RevisionId;
        CurrentRevisionNumber = document.RevisionNumber;
        CurrentRevisionState = document.RevisionState;
        CurrentRevisionCreatedAtUtc = document.CreatedAtUtc;
        CurrentRevisionUpdatedAtUtc = document.UpdatedAtUtc;
        CurrentRevisionApprovedAtUtc = document.ApprovedAtUtc;
        UpdateRevisionStatus();
    }

    private void TrackPersistedInputChange(
        object? sender,
        PropertyChangedEventArgs eventArgs)
    {
        if (_suppressDocumentTracking ||
            _isApplyingCentralData ||
            string.IsNullOrWhiteSpace(eventArgs.PropertyName) ||
            !PersistedInputPropertyNames.Contains(eventArgs.PropertyName))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (CurrentRevisionState ==
            CostingRevisionState.ApprovedRevision)
        {
            var nextRevision = _revisionService.CreateNextRevision(
                CreateProjectDocument(),
                now);
            _suppressDocumentTracking = true;
            try
            {
                ApplyRevisionIdentity(nextRevision);
                CurrentDocumentPath = null;
            }
            finally
            {
                _suppressDocumentTracking = false;
            }
        }

        CurrentRevisionUpdatedAtUtc = now;
        HasUnsavedChanges = true;
        UpdateRevisionStatus();
    }

    private void UpdateRevisionStatus()
    {
        RevisionStatusDisplay =
            CurrentRevisionState == CostingRevisionState.ApprovedRevision
                ? $"Approved · revision {CurrentRevisionNumber} · immutable"
                : $"Working copy · revision {CurrentRevisionNumber}";
        SaveStateDisplay = HasValidationErrors
            ? "Validation required"
            : HasUnsavedChanges
                ? "Unsaved changes"
                : "Saved";
    }

    private static string FormatCurrency(
        decimal amount,
        QuoteCurrencyOption currency) =>
        $"{currency.Symbol}{amount:N2} {currency.Code}";

    private static CalculationStepRow ToRow(CalculationStep step) =>
        new(
            step.Label,
            step.BusinessMeaning ?? string.Empty,
            step.Expression,
            step.InputSteps.Count == 0
                ? string.Empty
                : step.SubstitutedExpression,
            FormatCalculationResult(step),
            step.RoundingRule ?? string.Empty,
            step.RuleVersion ?? string.Empty,
            step.Warning);

    private static CalculationStepRow ToRow(SavedCalculationStep step) =>
        new(
            step.Label,
            step.BusinessMeaning ?? string.Empty,
            step.Expression,
            step.Inputs.Count == 0
                ? string.Empty
                : step.SubstitutedExpression,
            FormatCalculationResult(step),
            step.RoundingRule ?? string.Empty,
            step.RuleVersion ?? string.Empty,
            step.Warning);

    private static string FormatCalculationResult(CalculationStep step) =>
        step.Unit switch
        {
            "£" => Pounds(step.RawValue),
            "£/kg" => string.Format(
                PoundCulture,
                "{0:C5}/kg",
                step.RawValue),
            "£/m" => PoundPerMetre(step.RawValue),
            "kg" => $"{step.RawValue:N6} kg",
            "kg/m" => $"{step.RawValue:N9} kg/m",
            "g/m" => $"{step.RawValue:N6} g/m",
            "m/kg" => $"{step.RawValue:N6} m/kg",
            "m/h" => $"{step.RawValue:N0} m/h",
            "m" => $"{step.RawValue:N3} m",
            "mm" => $"{step.RawValue:N6} mm",
            "mm²" => $"{step.RawValue:N6} mm²",
            "h" => $"{step.RawValue:N4} h",
            _ => $"{step.DisplayValue} {step.Unit}".Trim(),
        };

    private static string FormatCalculationResult(SavedCalculationStep step) =>
        step.Unit switch
        {
            "£" => Pounds(step.RawValue),
            "£/kg" => string.Format(
                PoundCulture,
                "{0:C5}/kg",
                step.RawValue),
            "£/m" => PoundPerMetre(step.RawValue),
            "kg" => $"{step.RawValue:N6} kg",
            "kg/m" => $"{step.RawValue:N9} kg/m",
            "g/m" => $"{step.RawValue:N6} g/m",
            "m/kg" => $"{step.RawValue:N6} m/kg",
            "m/h" => $"{step.RawValue:N0} m/h",
            "m" => $"{step.RawValue:N3} m",
            "mm" => $"{step.RawValue:N6} mm",
            "mm²" => $"{step.RawValue:N6} mm²",
            "h" => $"{step.RawValue:N4} h",
            _ => $"{step.DisplayValue} {step.Unit}".Trim(),
        };

    private void RecalculateIfReady()
    {
        if (_isApplyingCentralData ||
            SelectedCopper is null ||
            SelectedCompound is null ||
            SelectedMasterbatch is null)
        {
            return;
        }

        Recalculate();
    }

    private static void ReplaceRows(
        ObservableCollection<CalculationStepRow> rows,
        IEnumerable<CalculationStep> steps)
    {
        rows.Clear();
        foreach (var step in steps)
        {
            rows.Add(ToRow(step));
        }
    }

    private static void ReplaceRows(
        ObservableCollection<CalculationStepRow> rows,
        IEnumerable<SavedCalculationStep> steps)
    {
        rows.Clear();
        foreach (var step in steps)
        {
            rows.Add(ToRow(step));
        }
    }

    private static void ReplaceFlowStages(
        ObservableCollection<CalculationFlowStage> stages,
        IEnumerable<CalculationStep> steps)
    {
        stages.Clear();
        var allSteps = FlattenSteps(
                steps,
                step => step.Id,
                step => step.InputSteps)
            .ToArray();
        AddFlowStages(
            stages,
            allSteps,
            step => step.Id,
            step => step.InputSteps,
            step => new CalculationFlowNode(
                step.Label,
                step.BusinessMeaning ?? string.Empty,
                step.InputSteps.Count == 0 ? "Source value" : step.Expression,
                step.InputSteps.Count == 0
                    ? string.Empty
                    : step.SubstitutedExpression,
                FormatCalculationResult(step),
                InputSummary(step.InputSteps.Select(input => input.Label)),
                step.Warning));
    }

    private static void ReplaceFlowStages(
        ObservableCollection<CalculationFlowStage> stages,
        IEnumerable<SavedCalculationStep> steps)
    {
        stages.Clear();
        var allSteps = FlattenSteps(
                steps,
                step => step.Id,
                step => step.Inputs)
            .ToArray();
        AddFlowStages(
            stages,
            allSteps,
            step => step.Id,
            step => step.Inputs,
            step => new CalculationFlowNode(
                step.Label,
                step.BusinessMeaning ?? string.Empty,
                step.Inputs.Count == 0 ? "Source value" : step.Expression,
                step.Inputs.Count == 0
                    ? string.Empty
                    : step.SubstitutedExpression,
                FormatCalculationResult(step),
                InputSummary(step.Inputs.Select(input => input.Label)),
                step.Warning));
    }

    private static IReadOnlyList<TStep> FlattenSteps<TStep>(
        IEnumerable<TStep> roots,
        Func<TStep, string> id,
        Func<TStep, IReadOnlyList<TStep>> inputs)
    {
        var ordered = new List<TStep>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Visit(TStep step)
        {
            foreach (var input in inputs(step))
            {
                Visit(input);
            }

            if (seen.Add(id(step)))
            {
                ordered.Add(step);
            }
        }

        foreach (var root in roots)
        {
            Visit(root);
        }

        return ordered;
    }

    private static void AddFlowStages<TStep>(
        ObservableCollection<CalculationFlowStage> stages,
        IReadOnlyList<TStep> steps,
        Func<TStep, string> id,
        Func<TStep, IReadOnlyList<TStep>> inputs,
        Func<TStep, CalculationFlowNode> toNode)
    {
        if (steps.Count == 0)
        {
            return;
        }

        var byId = steps.ToDictionary(id, StringComparer.Ordinal);
        var depths = new Dictionary<string, int>(StringComparer.Ordinal);

        int Depth(TStep step)
        {
            var stepId = id(step);
            if (depths.TryGetValue(stepId, out var existing))
            {
                return existing;
            }

            var stepInputs = inputs(step)
                .Where(input => byId.ContainsKey(id(input)))
                .ToArray();
            var depth = stepInputs.Length == 0
                ? 0
                : stepInputs.Max(Depth) + 1;
            depths[stepId] = depth;
            return depth;
        }

        foreach (var step in steps)
        {
            Depth(step);
        }

        var groups = steps
            .GroupBy(step => depths[id(step)])
            .OrderBy(group => group.Key)
            .ToArray();
        for (var index = 0; index < groups.Length; index++)
        {
            var group = groups[index];
            var last = index == groups.Length - 1;
            stages.Add(
                new CalculationFlowStage(
                    group.Key == 0
                        ? "1 · Source values"
                        : last
                            ? $"{index + 1} · Section result"
                            : $"{index + 1} · Derived values",
                    group.Key == 0
                        ? "Quoted, locked, or user-entered values start this calculation."
                        : last
                            ? "These outputs are produced from the linked stages above."
                            : "Each value below names the earlier values it consumes.",
                    index % 2 == 0 ? "#173047" : "#193B36",
                    group.Select(toNode).ToArray(),
                    last ? string.Empty : "↓  outputs feed the next stage"));
        }
    }

    private static string InputSummary(IEnumerable<string> labels)
    {
        var inputs = labels
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return inputs.Length == 0
            ? "Entered or locked source value"
            : $"Uses: {string.Join(" + ", inputs)}";
    }

    private void ClearMaterialCalculationSteps()
    {
        ConductorCalculationSteps.Clear();
        CompoundCalculationSteps.Clear();
        MasterbatchCalculationSteps.Clear();
        LabourCalculationSteps.Clear();
        ConductorCalculationFlow.Clear();
        CompoundCalculationFlow.Clear();
        MasterbatchCalculationFlow.Clear();
        LabourCalculationFlow.Clear();
    }

    private void UpdateCentralDataLinkStatuses()
    {
        CentralDataLinkStatuses.Clear();
        var liveCount = 0;
        var checkingCount = 0;

        foreach (var area in CentralDataAreas)
        {
            var link = DatabaseTableLinks.FirstOrDefault(item => item.Area == area);
            if (link is null)
            {
                _centralDataAreaConnectionStates.Remove(area);
                CentralDataLinkStatuses.Add(new CentralDataLinkStatusRow(
                    AreaName(area),
                    "Cached only",
                    "No database refresh link",
                    "#6B5A2A"));
                continue;
            }

            var areaState = _centralDataAreaConnectionStates.GetValueOrDefault(
                area,
                CentralDataAreaConnectionState.ReadyToCheck);
            var (status, detail, colour) = areaState switch
            {
                CentralDataAreaConnectionState.Live =>
                    ("LIVE", link.TableName, "#28A745"),
                CentralDataAreaConnectionState.Checking =>
                    ("Checking", link.TableName, "#0078D4"),
                CentralDataAreaConnectionState.Offline =>
                    ("Offline · cached", link.TableName, "#C44B2C"),
                _ =>
                    ("Ready to check", link.TableName, "#CA8A04"),
            };

            if (areaState == CentralDataAreaConnectionState.Live)
            {
                liveCount++;
            }
            else if (areaState == CentralDataAreaConnectionState.Checking)
            {
                checkingCount++;
            }

            CentralDataLinkStatuses.Add(new CentralDataLinkStatusRow(
                AreaName(area),
                status,
                detail,
                colour));
        }

        CentralDataLinkSummaryDisplay = checkingCount > 0
            ? $"Material links · checking {checkingCount}"
            : DatabaseTableLinks.Count == 0
                ? RetainedSourceTables.Count > 0
                    ? "Live Data · cached only"
                    : "Live Data · setup required"
                : $"Material links · {liveCount} of {CentralDataAreas.Length} LIVE";
    }

    private static bool IsMaterialCommercialStep(CalculationStep step) =>
        step.Id.StartsWith("risk-", StringComparison.Ordinal) ||
        step.Id.StartsWith("markup-", StringComparison.Ordinal) ||
        step.Id.StartsWith("marked-up-", StringComparison.Ordinal);

    private static string AreaName(CentralDataArea area) =>
        area switch
        {
            CentralDataArea.Copper => "Copper",
            CentralDataArea.Compounds => "Compounds",
            CentralDataArea.Masterbatch => "Masterbatch",
            CentralDataArea.Contacts => "Contacts",
            CentralDataArea.Operators => "Operators",
            _ => area.ToString(),
        };

    private static decimal ToDecimal(double value) =>
        Convert.ToDecimal(value, CultureInfo.InvariantCulture);

    private static decimal FiniteNonNegativeDecimal(double value) =>
        double.IsFinite(value) && value >= 0
            ? ToDecimal(value)
            : 0m;

    private static string PoundPerMetre(decimal value) =>
        string.Format(PoundCulture, "{0:C4}/m", value);

    private static string Pounds(decimal value) =>
        string.Format(PoundCulture, "{0:C2}", value);

    private static string Duration(decimal hours)
    {
        var totalSeconds = decimal.ToInt64(
            decimal.Round(hours * 3600m, 0, MidpointRounding.AwayFromZero));
        var wholeHours = totalSeconds / 3600;
        var minutes = totalSeconds % 3600 / 60;
        var seconds = totalSeconds % 60;
        return $"{wholeHours:00}:{minutes:00}:{seconds:00} ({hours:N4} h)";
    }
}
