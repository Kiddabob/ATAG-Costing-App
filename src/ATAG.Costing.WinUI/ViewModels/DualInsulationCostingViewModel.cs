using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using ATAG.Costing.Application.CentralData;
using ATAG.Costing.Application.Costing;
using ATAG.Costing.Application.Projects;
using ATAG.Costing.Domain.Calculations;
using ATAG.Costing.Domain.Costing;
using ATAG.Costing.Domain.Materials;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATAG.Costing.WinUI.ViewModels;

/// <summary>
/// Presentation state for the guided dual-insulation editor. All material,
/// production and commercial calculations are delegated to Application and
/// Domain services.
/// </summary>
public partial class DualInsulationCostingViewModel : ObservableObject
{
    private static readonly CultureInfo PoundCulture =
        CultureInfo.GetCultureInfo("en-GB");
    private static readonly HashSet<string> PersistedInputNames =
        new(StringComparer.Ordinal)
        {
            nameof(ProjectName),
            nameof(CustomerName),
            nameof(SelectedCopper),
            nameof(SelectedFirstCompound),
            nameof(SelectedFirstMasterbatch),
            nameof(SelectedSecondCompound),
            nameof(SelectedSecondMasterbatch),
            nameof(FinishedQuoteLengthMetres),
            nameof(CoreStartupLengthMetres),
            nameof(UsageAllowancePercent),
            nameof(RiskPercent),
            nameof(MarkupPercent),
            nameof(TargetMarginPercent),
            nameof(ConductorSupplierQuoteTotal),
            nameof(ConductorSupplierQuotedKilograms),
            nameof(ConductorYieldMetresPerKilogram),
            nameof(ConductorOutsideDiameterMillimetres),
            nameof(FirstCompoundSupplierQuoteTotal),
            nameof(FirstCompoundSupplierQuotedKilograms),
            nameof(FirstCompoundSpecificGravity),
            nameof(FirstFinishedOutsideDiameterMillimetres),
            nameof(FirstOutsideDiameterToleranceMillimetres),
            nameof(FirstMasterbatchSupplierQuoteTotal),
            nameof(FirstMasterbatchSupplierQuotedKilograms),
            nameof(FirstMasterbatchAdditionPercent),
            nameof(SecondCompoundSupplierQuoteTotal),
            nameof(SecondCompoundSupplierQuotedKilograms),
            nameof(SecondCompoundSpecificGravity),
            nameof(SecondFinishedOutsideDiameterMillimetres),
            nameof(SecondOutsideDiameterToleranceMillimetres),
            nameof(SecondMasterbatchSupplierQuoteTotal),
            nameof(SecondMasterbatchSupplierQuotedKilograms),
            nameof(SecondMasterbatchAdditionPercent),
            nameof(FirstProfileMaximumOutsideDiameterMillimetres),
            nameof(FirstProfileLineSpeedMetresPerHour),
            nameof(FirstAboveMaximumLineSpeedMetresPerHour),
            nameof(UseFirstManualLineSpeed),
            nameof(FirstManualLineSpeedMetresPerHour),
            nameof(FirstSetupTimeHours),
            nameof(FirstOperatorCount),
            nameof(FirstHourlyLabourRate),
            nameof(SecondProfileMaximumOutsideDiameterMillimetres),
            nameof(SecondProfileLineSpeedMetresPerHour),
            nameof(SecondAboveMaximumLineSpeedMetresPerHour),
            nameof(UseSecondManualLineSpeed),
            nameof(SecondManualLineSpeedMetresPerHour),
            nameof(SecondSetupTimeHours),
            nameof(SecondOperatorCount),
            nameof(SecondHourlyLabourRate),
            nameof(IncludeTape),
            nameof(IncludeChalk),
            nameof(IncludeFoil),
            nameof(IncludeBraid),
            nameof(IncludeLapscreen),
            nameof(IncludeDrainWire),
        };

    private readonly DualInsulationCostingApplicationService _costingService =
        new();
    private readonly SingleCoreProjectRevisionService _revisionService = new();
    private IReadOnlyList<CopperReference> _allCopper = [];
    private IReadOnlyList<CompoundReference> _allCompounds = [];
    private IReadOnlyList<MasterbatchReference> _allMasterbatches = [];
    private string _centralDataRevision = "";
    private bool _suppressInputTracking = true;
    private DualInsulationCalculatedResultSnapshot? _currentCalculatedResult;

    [ObservableProperty]
    public partial IReadOnlyList<CopperReference> CopperOptions { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CompoundReference> FirstCompoundOptions { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CompoundReference> SecondCompoundOptions { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<MasterbatchReference> FirstMasterbatchOptions { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<MasterbatchReference> SecondMasterbatchOptions { get; set; } = [];

    [ObservableProperty]
    public partial string CopperSearchText { get; set; } = "";

    [ObservableProperty]
    public partial string FirstCompoundSearchText { get; set; } = "";

    [ObservableProperty]
    public partial string SecondCompoundSearchText { get; set; } = "";

    [ObservableProperty]
    public partial string FirstMasterbatchSearchText { get; set; } = "";

    [ObservableProperty]
    public partial string SecondMasterbatchSearchText { get; set; } = "";

    [ObservableProperty]
    public partial CopperReference? SelectedCopper { get; set; }

    [ObservableProperty]
    public partial CompoundReference? SelectedFirstCompound { get; set; }

    [ObservableProperty]
    public partial MasterbatchReference? SelectedFirstMasterbatch { get; set; }

    [ObservableProperty]
    public partial CompoundReference? SelectedSecondCompound { get; set; }

    [ObservableProperty]
    public partial MasterbatchReference? SelectedSecondMasterbatch { get; set; }

    [ObservableProperty]
    public partial string ProjectName { get; set; } = "Dual-insulation costing";

    [ObservableProperty]
    public partial string CustomerName { get; set; } = "";

    [ObservableProperty]
    public partial double FinishedQuoteLengthMetres { get; set; } = 10000;

    [ObservableProperty]
    public partial double CoreStartupLengthMetres { get; set; } = 200;

    [ObservableProperty]
    public partial double UsageAllowancePercent { get; set; } = 3;

    [ObservableProperty]
    public partial double RiskPercent { get; set; }

    [ObservableProperty]
    public partial double MarkupPercent { get; set; } = 45;

    [ObservableProperty]
    public partial double TargetMarginPercent { get; set; } = 45;

    [ObservableProperty]
    public partial double ConductorSupplierQuoteTotal { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double ConductorSupplierQuotedKilograms { get; set; } = 1;

    [ObservableProperty]
    public partial double ConductorYieldMetresPerKilogram { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double ConductorOutsideDiameterMillimetres { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double FirstCompoundSupplierQuoteTotal { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double FirstCompoundSupplierQuotedKilograms { get; set; } = 1;

    [ObservableProperty]
    public partial double FirstCompoundSpecificGravity { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double FirstFinishedOutsideDiameterMillimetres { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double FirstOutsideDiameterToleranceMillimetres { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double FirstMasterbatchSupplierQuoteTotal { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double FirstMasterbatchSupplierQuotedKilograms { get; set; } = 1;

    [ObservableProperty]
    public partial double FirstMasterbatchAdditionPercent { get; set; } = 1;

    [ObservableProperty]
    public partial double SecondCompoundSupplierQuoteTotal { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double SecondCompoundSupplierQuotedKilograms { get; set; } = 1;

    [ObservableProperty]
    public partial double SecondCompoundSpecificGravity { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double SecondFinishedOutsideDiameterMillimetres { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double SecondOutsideDiameterToleranceMillimetres { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double SecondMasterbatchSupplierQuoteTotal { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double SecondMasterbatchSupplierQuotedKilograms { get; set; } = 1;

    [ObservableProperty]
    public partial double SecondMasterbatchAdditionPercent { get; set; } = 1;

    [ObservableProperty]
    public partial double FirstProfileMaximumOutsideDiameterMillimetres { get; set; } = 4;

    [ObservableProperty]
    public partial double FirstProfileLineSpeedMetresPerHour { get; set; } = 5000;

    [ObservableProperty]
    public partial double FirstAboveMaximumLineSpeedMetresPerHour { get; set; } = 700;

    [ObservableProperty]
    public partial bool UseFirstManualLineSpeed { get; set; }

    [ObservableProperty]
    public partial double FirstManualLineSpeedMetresPerHour { get; set; } = 5000;

    [ObservableProperty]
    public partial double FirstSetupTimeHours { get; set; }

    [ObservableProperty]
    public partial double FirstOperatorCount { get; set; } = 1;

    [ObservableProperty]
    public partial double FirstHourlyLabourRate { get; set; } = 35;

    [ObservableProperty]
    public partial double SecondProfileMaximumOutsideDiameterMillimetres { get; set; } = 4;

    [ObservableProperty]
    public partial double SecondProfileLineSpeedMetresPerHour { get; set; } = 3000;

    [ObservableProperty]
    public partial double SecondAboveMaximumLineSpeedMetresPerHour { get; set; } = 700;

    [ObservableProperty]
    public partial bool UseSecondManualLineSpeed { get; set; }

    [ObservableProperty]
    public partial double SecondManualLineSpeedMetresPerHour { get; set; } = 3000;

    [ObservableProperty]
    public partial double SecondSetupTimeHours { get; set; }

    [ObservableProperty]
    public partial double SecondOperatorCount { get; set; } = 1;

    [ObservableProperty]
    public partial double SecondHourlyLabourRate { get; set; } = 35;

    [ObservableProperty]
    public partial bool IncludeTape { get; set; }

    [ObservableProperty]
    public partial bool IncludeChalk { get; set; }

    [ObservableProperty]
    public partial bool IncludeFoil { get; set; }

    [ObservableProperty]
    public partial bool IncludeBraid { get; set; }

    [ObservableProperty]
    public partial bool IncludeLapscreen { get; set; }

    [ObservableProperty]
    public partial bool IncludeDrainWire { get; set; }

    [ObservableProperty]
    public partial string CoreProductionScopeDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SecondLayerProductionScopeDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string MaterialCostDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string MaterialCostPerMetreDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string FirstExtrusionLabourDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SecondExtrusionLabourDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TotalLabourDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string EstimatedCostDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string RiskAdjustedCostDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string RecommendedQuoteDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CombinedRatePriceDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TargetMarginPriceDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string FirstProductionTimeDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SecondProductionTimeDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CalculationStatus { get; set; } =
        "Choose all five material records and complete the highlighted dimensions.";

    [ObservableProperty]
    public partial bool HasValidationErrors { get; set; } = true;

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; set; }

    [ObservableProperty]
    public partial string RevisionStatusDisplay { get; set; } = "Working revision 1 · not saved";

    public ObservableCollection<CalculationStepRow> CalculationSteps { get; } = [];

    public Guid CurrentProjectId { get; private set; } = Guid.NewGuid();
    public Guid CurrentRevisionId { get; private set; } = Guid.NewGuid();
    public int CurrentRevisionNumber { get; private set; } = 1;
    public CostingRevisionState CurrentRevisionState { get; private set; } =
        CostingRevisionState.WorkingCopy;
    public DateTimeOffset CurrentRevisionCreatedAtUtc { get; private set; } =
        DateTimeOffset.UtcNow;
    public DateTimeOffset? CurrentRevisionApprovedAtUtc { get; private set; }
    public string? CurrentDocumentPath { get; private set; }

    public DualInsulationCostingViewModel(CentralDataService centralDataService)
    {
        ArgumentNullException.ThrowIfNull(centralDataService);
        ApplyCentralDataState(
            centralDataService.Load(),
            applyReferenceDefaults: true);
        PropertyChanged += TrackPersistedInputChange;
        _suppressInputTracking = false;
        Recalculate();
        UpdateRevisionStatus();
    }

    public void RefreshCentralData(CentralDataState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var retainedCopper = SelectedCopper?.Id;
        var retainedFirstCompound = SelectedFirstCompound?.Id;
        var retainedSecondCompound = SelectedSecondCompound?.Id;
        var retainedFirstMasterbatch = SelectedFirstMasterbatch?.ColourCode;
        var retainedSecondMasterbatch = SelectedSecondMasterbatch?.ColourCode;

        var previous = _suppressInputTracking;
        _suppressInputTracking = true;
        try
        {
            ApplyCentralDataState(state);
            SelectedCopper = _allCopper.FirstOrDefault(item => item.Id == retainedCopper) ??
                SelectedCopper;
            SelectedFirstCompound = _allCompounds.FirstOrDefault(item => item.Id == retainedFirstCompound) ??
                SelectedFirstCompound;
            SelectedSecondCompound = _allCompounds.FirstOrDefault(item => item.Id == retainedSecondCompound) ??
                SelectedSecondCompound;
            SelectedFirstMasterbatch = _allMasterbatches.FirstOrDefault(item => item.ColourCode == retainedFirstMasterbatch) ??
                SelectedFirstMasterbatch;
            SelectedSecondMasterbatch = _allMasterbatches.FirstOrDefault(item => item.ColourCode == retainedSecondMasterbatch) ??
                SelectedSecondMasterbatch;
        }
        finally
        {
            _suppressInputTracking = previous;
        }

        Recalculate();
    }

    public void Recalculate()
    {
        if (SelectedCopper is null ||
            SelectedFirstCompound is null ||
            SelectedFirstMasterbatch is null ||
            SelectedSecondCompound is null ||
            SelectedSecondMasterbatch is null)
        {
            Invalidate("Choose a conductor, compound and masterbatch for both insulation layers.");
            return;
        }

        try
        {
            var result = _costingService.Calculate(BuildRequest());
            ApplyResult(result);
            HasValidationErrors = false;
            CalculationStatus =
                "Calculated with separate core/first-layer and second-layer production scopes. " +
                "Optional modules are saved in order but do not add material formulas yet.";
        }
        catch (Exception exception) when (exception is
            ArgumentException or InvalidOperationException or OverflowException)
        {
            Invalidate(exception.Message);
        }
    }

    public SingleCoreProjectDocument CreateProjectDocument()
    {
        var now = DateTimeOffset.UtcNow;
        return new SingleCoreProjectDocument
        {
            ConstructionKind = CostingConstructionKind.DualInsulation,
            ProjectId = CurrentProjectId,
            RevisionId = CurrentRevisionId,
            RevisionNumber = CurrentRevisionNumber,
            RevisionState = CurrentRevisionState,
            CreatedAtUtc = CurrentRevisionCreatedAtUtc,
            UpdatedAtUtc = now,
            ApprovedAtUtc = CurrentRevisionApprovedAtUtc,
            SavedAt = now,
            CentralDataRevision = _centralDataRevision,
            CustomerName = CustomerName,
            RuleVersions = CalculationSteps
                .Select(step => step.RuleVersion)
                .Where(version => !string.IsNullOrWhiteSpace(version))
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            DualInsulation = CreatePayload(),
            DualCalculatedResult = _currentCalculatedResult,
        };
    }

    public SingleCoreProjectDocument CreateApprovedProjectDocument()
    {
        if (HasValidationErrors || _currentCalculatedResult is null)
        {
            throw new InvalidOperationException(
                "Resolve the current dual-insulation validation error before approving this revision.");
        }

        return _revisionService.Approve(
            CreateProjectDocument(),
            DateTimeOffset.UtcNow);
    }

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
        if (document.ConstructionKind != CostingConstructionKind.DualInsulation ||
            document.DualInsulation is null)
        {
            message = "The selected revision is a single insulated core costing.";
            return false;
        }

        var payload = document.DualInsulation;
        var copper = FindOrCreateCopper(payload);
        var firstCompound = FindOrCreateCompound(payload.FirstLayer);
        var secondCompound = FindOrCreateCompound(payload.SecondLayer);
        var firstMasterbatch = FindOrCreateMasterbatch(payload.FirstLayer);
        var secondMasterbatch = FindOrCreateMasterbatch(payload.SecondLayer);
        if (copper is null || firstCompound is null || secondCompound is null ||
            firstMasterbatch is null || secondMasterbatch is null)
        {
            message =
                "The dual costing does not contain enough locked material evidence to reopen it.";
            return false;
        }

        var previous = _suppressInputTracking;
        _suppressInputTracking = true;
        try
        {
            EnsureReferenceOptions(
                copper,
                firstCompound,
                secondCompound,
                firstMasterbatch,
                secondMasterbatch);
            SelectedCopper = copper;
            SelectedFirstCompound = firstCompound;
            SelectedSecondCompound = secondCompound;
            SelectedFirstMasterbatch = firstMasterbatch;
            SelectedSecondMasterbatch = secondMasterbatch;
            ProjectName = payload.ProjectName;
            CustomerName = document.CustomerName;
            FinishedQuoteLengthMetres = payload.FinishedQuoteLengthMetres;
            CoreStartupLengthMetres = payload.CoreStartupLengthMetres;
            UsageAllowancePercent = payload.UsageAllowancePercent;
            RiskPercent = payload.RiskPercent;
            MarkupPercent = payload.MarkupPercent;
            TargetMarginPercent = payload.TargetMarginPercent;
            ApplyMaterialPayload(payload);
            ApplyExtrusionPayload(payload.FirstExtrusion, first: true);
            ApplyExtrusionPayload(payload.SecondExtrusion, first: false);
            ApplyModules(payload.AddOnModules);
            ApplyRevisionIdentity(document);
        }
        finally
        {
            _suppressInputTracking = previous;
        }

        if (document.RevisionState == CostingRevisionState.ApprovedRevision &&
            document.DualCalculatedResult is not null)
        {
            ApplyCalculatedSnapshot(document.DualCalculatedResult);
            HasValidationErrors = false;
            message =
                $"Reopened approved dual revision {document.RevisionNumber} exactly as saved.";
        }
        else
        {
            Recalculate();
            message =
                $"Reopened working dual revision {document.RevisionNumber} from its locked values and current shared rules.";
        }

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
        CurrentDocumentPath = fullPath;
        HasUnsavedChanges = false;
        UpdateRevisionStatus();
    }

    public void MarkDocumentNeedsIndexing(string portablePath)
    {
        CurrentDocumentPath = portablePath;
        HasUnsavedChanges = true;
        UpdateRevisionStatus();
    }

    public bool TryDuplicateCurrentProject(out string message)
    {
        var duplicate = _revisionService.Duplicate(
            CreateProjectDocument(),
            DateTimeOffset.UtcNow);
        ApplyRevisionIdentity(duplicate);
        CurrentDocumentPath = null;
        HasUnsavedChanges = true;
        UpdateRevisionStatus();
        message = "Duplicated as a new dual-insulation project and working revision 1.";
        return true;
    }

    partial void OnCopperSearchTextChanged(string value) => FilterCopper();
    partial void OnFirstCompoundSearchTextChanged(string value) => FilterFirstCompounds();
    partial void OnSecondCompoundSearchTextChanged(string value) => FilterSecondCompounds();
    partial void OnFirstMasterbatchSearchTextChanged(string value) => FilterFirstMasterbatches();
    partial void OnSecondMasterbatchSearchTextChanged(string value) => FilterSecondMasterbatches();

    partial void OnSelectedCopperChanged(CopperReference? value)
    {
        if (_suppressInputTracking || value is null)
        {
            return;
        }

        ConductorSupplierQuoteTotal = (double)value.PricePerKilogram;
        ConductorSupplierQuotedKilograms = 1;
        ConductorYieldMetresPerKilogram = (double)value.YieldMetresPerKilogram;
        ConductorOutsideDiameterMillimetres =
            (double)value.NominalOutsideDiameterMillimetres;
    }

    partial void OnSelectedFirstCompoundChanged(CompoundReference? value) =>
        ApplyCompoundDefaults(value, first: true);

    partial void OnSelectedSecondCompoundChanged(CompoundReference? value) =>
        ApplyCompoundDefaults(value, first: false);

    partial void OnSelectedFirstMasterbatchChanged(MasterbatchReference? value) =>
        ApplyMasterbatchDefaults(value, first: true);

    partial void OnSelectedSecondMasterbatchChanged(MasterbatchReference? value) =>
        ApplyMasterbatchDefaults(value, first: false);

    private void ApplyCentralDataState(
        CentralDataState state,
        bool applyReferenceDefaults = false)
    {
        _centralDataRevision = state.Snapshot.Revision;
        _allCopper = state.Snapshot.Copper
            .Where(item => item.IsSelectableForCosting)
            .OrderBy(item => item.Description)
            .ThenBy(item => item.Supplier)
            .ToArray();
        _allCompounds = state.Snapshot.Compounds
            .OrderBy(item => item.CompoundName)
            .ThenBy(item => item.Supplier)
            .ToArray();
        _allMasterbatches = state.Snapshot.Masterbatches
            .OrderBy(item => item.ColourName)
            .ThenBy(item => item.ColourCode)
            .ToArray();
        FilterAll();

        SelectedCopper ??= _allCopper.FirstOrDefault();
        SelectedFirstCompound ??= _allCompounds.FirstOrDefault();
        SelectedSecondCompound ??= _allCompounds.FirstOrDefault();
        SelectedFirstMasterbatch ??= _allMasterbatches.FirstOrDefault();
        SelectedSecondMasterbatch ??= _allMasterbatches.FirstOrDefault();

        if (SelectedCopper is not null)
        {
            ConductorSupplierQuoteTotal = (double)SelectedCopper.PricePerKilogram;
            ConductorYieldMetresPerKilogram =
                (double)SelectedCopper.YieldMetresPerKilogram;
            ConductorOutsideDiameterMillimetres =
                (double)SelectedCopper.NominalOutsideDiameterMillimetres;
        }

        if (applyReferenceDefaults)
        {
            if (SelectedFirstCompound is not null)
            {
                FirstCompoundSupplierQuoteTotal =
                    (double)SelectedFirstCompound.PricePerKilogram;
                FirstCompoundSpecificGravity =
                    (double)SelectedFirstCompound.SpecificGravity;
            }

            if (SelectedSecondCompound is not null)
            {
                SecondCompoundSupplierQuoteTotal =
                    (double)SelectedSecondCompound.PricePerKilogram;
                SecondCompoundSpecificGravity =
                    (double)SelectedSecondCompound.SpecificGravity;
            }

            if (SelectedFirstMasterbatch is not null)
            {
                FirstMasterbatchSupplierQuoteTotal =
                    (double)SelectedFirstMasterbatch.PricePerKilogram;
            }

            if (SelectedSecondMasterbatch is not null)
            {
                SecondMasterbatchSupplierQuoteTotal =
                    (double)SelectedSecondMasterbatch.PricePerKilogram;
            }
        }
    }

    private void TrackPersistedInputChange(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (_suppressInputTracking || e.PropertyName is null ||
            !PersistedInputNames.Contains(e.PropertyName))
        {
            return;
        }

        if (CurrentRevisionState == CostingRevisionState.ApprovedRevision)
        {
            var next = _revisionService.CreateNextRevision(
                CreateProjectDocument(),
                DateTimeOffset.UtcNow);
            ApplyRevisionIdentity(next);
            CurrentDocumentPath = null;
        }

        HasUnsavedChanges = true;
        Recalculate();
        UpdateRevisionStatus();
    }

    private DualInsulationCostingRequest BuildRequest() =>
        new(
            new DualInsulationCostingInputs(
                SelectedCopper!.Description,
                Quote(ConductorSupplierQuoteTotal, ConductorSupplierQuotedKilograms),
                new YieldMetresPerKilogram(ToDecimal(ConductorYieldMetresPerKilogram)),
                new Millimetres(ToDecimal(ConductorOutsideDiameterMillimetres)),
                BuildLayer(first: true),
                BuildLayer(first: false),
                new LengthMetres(ToDecimal(FinishedQuoteLengthMetres)),
                new AdditionalProductionLengthMetres(ToDecimal(CoreStartupLengthMetres)),
                new UsageAllowanceRateFraction(ToDecimal(UsageAllowancePercent) / 100m)),
            BuildExtrusion(first: true),
            BuildExtrusion(first: false),
            new RiskRateFraction(ToDecimal(RiskPercent) / 100m),
            new MarkupRateFraction(ToDecimal(MarkupPercent) / 100m),
            new TargetMarginRateFraction(ToDecimal(TargetMarginPercent) / 100m),
            SelectedModules());

    private DualInsulationLayerInputs BuildLayer(bool first) =>
        first
            ? new DualInsulationLayerInputs(
                SelectedFirstCompound!.CompoundName,
                Quote(FirstCompoundSupplierQuoteTotal, FirstCompoundSupplierQuotedKilograms),
                new SpecificGravity(ToDecimal(FirstCompoundSpecificGravity)),
                new Millimetres(ToDecimal(FirstFinishedOutsideDiameterMillimetres)),
                new Millimetres(ToDecimal(FirstOutsideDiameterToleranceMillimetres)),
                $"{SelectedFirstMasterbatch!.ColourName} ({SelectedFirstMasterbatch.ColourCode})",
                Quote(FirstMasterbatchSupplierQuoteTotal, FirstMasterbatchSupplierQuotedKilograms),
                new AdditionRateFraction(ToDecimal(FirstMasterbatchAdditionPercent) / 100m))
            : new DualInsulationLayerInputs(
                SelectedSecondCompound!.CompoundName,
                Quote(SecondCompoundSupplierQuoteTotal, SecondCompoundSupplierQuotedKilograms),
                new SpecificGravity(ToDecimal(SecondCompoundSpecificGravity)),
                new Millimetres(ToDecimal(SecondFinishedOutsideDiameterMillimetres)),
                new Millimetres(ToDecimal(SecondOutsideDiameterToleranceMillimetres)),
                $"{SelectedSecondMasterbatch!.ColourName} ({SelectedSecondMasterbatch.ColourCode})",
                Quote(SecondMasterbatchSupplierQuoteTotal, SecondMasterbatchSupplierQuotedKilograms),
                new AdditionRateFraction(ToDecimal(SecondMasterbatchAdditionPercent) / 100m));

    private ExtrusionProductionSettings BuildExtrusion(bool first)
    {
        var profile = new ExtrusionLineSpeedProfile(
            first ? "First extrusion working profile" : "Second extrusion working profile",
            first ? "dual-first-line-profile/v1" : "dual-second-line-profile/v1",
            [
                new ExtrusionLineSpeedBand(
                    new Millimetres(ToDecimal(first
                        ? FirstProfileMaximumOutsideDiameterMillimetres
                        : SecondProfileMaximumOutsideDiameterMillimetres)),
                    new LineSpeedMetresPerHour(ToDecimal(first
                        ? FirstProfileLineSpeedMetresPerHour
                        : SecondProfileLineSpeedMetresPerHour))),
            ],
            new LineSpeedMetresPerHour(ToDecimal(first
                ? FirstAboveMaximumLineSpeedMetresPerHour
                : SecondAboveMaximumLineSpeedMetresPerHour)));
        var useManual = first ? UseFirstManualLineSpeed : UseSecondManualLineSpeed;
        return new ExtrusionProductionSettings(
            first ? "First insulation extrusion" : "Second insulation extrusion",
            new Millimetres(ToDecimal(first
                ? FirstFinishedOutsideDiameterMillimetres
                : SecondFinishedOutsideDiameterMillimetres)),
            profile,
            useManual
                ? new LineSpeedMetresPerHour(ToDecimal(first
                    ? FirstManualLineSpeedMetresPerHour
                    : SecondManualLineSpeedMetresPerHour))
                : null,
            new LabourHours(ToDecimal(first ? FirstSetupTimeHours : SecondSetupTimeHours)),
            new OperatorCount(ToDecimal(first ? FirstOperatorCount : SecondOperatorCount)),
            new HourlyLabourRate(ToDecimal(first ? FirstHourlyLabourRate : SecondHourlyLabourRate)));
    }

    private void ApplyResult(DualInsulationCostingApplicationResult result)
    {
        var materials = result.Materials;
        var production = result.Production;
        var commercial = result.Commercial;
        CoreProductionScopeDisplay = $"{materials.CoreProductionLength.Value:N0} m · finished + start-up";
        SecondLayerProductionScopeDisplay = $"{materials.SecondLayerProductionLength.Value:N0} m · finished only";
        MaterialCostDisplay = Pounds(materials.MaterialPriceForProductionRun);
        MaterialCostPerMetreDisplay = PoundPerMetre(materials.MaterialPricePerFinishedMetre.Value);
        FirstExtrusionLabourDisplay = Pounds(production.FirstExtrusion.LabourCost);
        SecondExtrusionLabourDisplay = Pounds(production.SecondExtrusion.LabourCost);
        TotalLabourDisplay = Pounds(production.TotalLabourCost);
        EstimatedCostDisplay = Pounds(commercial.EstimatedCost);
        RiskAdjustedCostDisplay = Pounds(commercial.RiskAdjustedCost);
        RecommendedQuoteDisplay = Pounds(commercial.SequentialRiskThenMarkupPrice);
        CombinedRatePriceDisplay = Pounds(commercial.CombinedRiskAndMarkupPrice);
        TargetMarginPriceDisplay = Pounds(commercial.TargetGrossMarginPrice);
        FirstProductionTimeDisplay = Duration(production.FirstExtrusion.TotalProcessTime.Value);
        SecondProductionTimeDisplay = Duration(production.SecondExtrusion.TotalProcessTime.Value);

        CalculationSteps.Clear();
        AddSteps(materials.Steps);
        AddSteps(production.Steps);
        AddSteps(commercial.Steps);
        _currentCalculatedResult = CreateCalculatedSnapshot(result);
    }

    private DualInsulationCalculatedResultSnapshot CreateCalculatedSnapshot(
        DualInsulationCostingApplicationResult result) =>
        new()
        {
            ProjectName = ProjectName,
            CoreAndFirstLayerProductionLengthMetres =
                result.Materials.CoreProductionLength.Value,
            SecondLayerProductionLengthMetres =
                result.Materials.SecondLayerProductionLength.Value,
            MaterialPriceForProductionRun =
                result.Materials.MaterialPriceForProductionRun,
            MaterialPricePerFinishedMetre =
                result.Materials.MaterialPricePerFinishedMetre.Value,
            FirstExtrusionLabourCost = result.Production.FirstExtrusion.LabourCost,
            SecondExtrusionLabourCost = result.Production.SecondExtrusion.LabourCost,
            TotalLabourCost = result.Production.TotalLabourCost,
            EstimatedCost = result.Commercial.EstimatedCost,
            RiskAdjustedCost = result.Commercial.RiskAdjustedCost,
            SequentialRiskThenMarkupPrice =
                result.Commercial.SequentialRiskThenMarkupPrice,
            CombinedRiskAndMarkupPrice =
                result.Commercial.CombinedRiskAndMarkupPrice,
            TargetGrossMarginPrice = result.Commercial.TargetGrossMarginPrice,
            MaterialPriceForProductionRunDisplay = MaterialCostDisplay,
            MaterialPricePerFinishedMetreDisplay = MaterialCostPerMetreDisplay,
            FirstExtrusionLabourCostDisplay = FirstExtrusionLabourDisplay,
            SecondExtrusionLabourCostDisplay = SecondExtrusionLabourDisplay,
            FirstProductionTimeDisplay = FirstProductionTimeDisplay,
            SecondProductionTimeDisplay = SecondProductionTimeDisplay,
            TotalLabourCostDisplay = TotalLabourDisplay,
            EstimatedCostDisplay = EstimatedCostDisplay,
            RiskAdjustedCostDisplay = RiskAdjustedCostDisplay,
            RecommendedQuoteDisplay = RecommendedQuoteDisplay,
            CombinedRatePriceDisplay = CombinedRatePriceDisplay,
            TargetMarginPriceDisplay = TargetMarginPriceDisplay,
            Trace =
            [
                new SavedCalculationSection(
                    "dual-materials",
                    "Dual material costing",
                    result.Materials.Steps.Select(ToSavedStep).ToArray()),
                new SavedCalculationSection(
                    "dual-production",
                    "First and second extrusion production",
                    result.Production.Steps.Select(ToSavedStep).ToArray()),
                new SavedCalculationSection(
                    "commercial",
                    "Commercial pricing",
                    result.Commercial.Steps.Select(ToSavedStep).ToArray()),
            ],
        };

    private DualInsulationProjectPayload CreatePayload() =>
        new()
        {
            ProjectName = ProjectName,
            Conductor = Material(
                SelectedCopper?.Id,
                SelectedCopper?.Description,
                SelectedCopper?.Supplier,
                ConductorSupplierQuoteTotal,
                ConductorSupplierQuotedKilograms),
            ConductorYieldMetresPerKilogram = ConductorYieldMetresPerKilogram,
            ConductorOutsideDiameterMillimetres = ConductorOutsideDiameterMillimetres,
            FirstLayer = CreateLayerPayload(first: true),
            SecondLayer = CreateLayerPayload(first: false),
            FinishedQuoteLengthMetres = FinishedQuoteLengthMetres,
            CoreStartupLengthMetres = CoreStartupLengthMetres,
            UsageAllowancePercent = UsageAllowancePercent,
            RiskPercent = RiskPercent,
            MarkupPercent = MarkupPercent,
            TargetMarginPercent = TargetMarginPercent,
            FirstExtrusion = CreateExtrusionPayload(first: true),
            SecondExtrusion = CreateExtrusionPayload(first: false),
            AddOnModules = SelectedModules(),
        };

    private DualInsulationLayerProjectPayload CreateLayerPayload(bool first) =>
        new()
        {
            Compound = Material(
                first ? SelectedFirstCompound?.Id : SelectedSecondCompound?.Id,
                first ? SelectedFirstCompound?.CompoundName : SelectedSecondCompound?.CompoundName,
                first ? SelectedFirstCompound?.Supplier : SelectedSecondCompound?.Supplier,
                first ? FirstCompoundSupplierQuoteTotal : SecondCompoundSupplierQuoteTotal,
                first ? FirstCompoundSupplierQuotedKilograms : SecondCompoundSupplierQuotedKilograms),
            CompoundSpecificGravity = first
                ? FirstCompoundSpecificGravity
                : SecondCompoundSpecificGravity,
            NominalFinishedOutsideDiameterMillimetres = first
                ? FirstFinishedOutsideDiameterMillimetres
                : SecondFinishedOutsideDiameterMillimetres,
            PositiveOutsideDiameterToleranceMillimetres = first
                ? FirstOutsideDiameterToleranceMillimetres
                : SecondOutsideDiameterToleranceMillimetres,
            Masterbatch = Material(
                first ? SelectedFirstMasterbatch?.ColourCode : SelectedSecondMasterbatch?.ColourCode,
                first ? SelectedFirstMasterbatch?.ColourName : SelectedSecondMasterbatch?.ColourName,
                first ? SelectedFirstMasterbatch?.Supplier : SelectedSecondMasterbatch?.Supplier,
                first ? FirstMasterbatchSupplierQuoteTotal : SecondMasterbatchSupplierQuoteTotal,
                first ? FirstMasterbatchSupplierQuotedKilograms : SecondMasterbatchSupplierQuotedKilograms),
            MasterbatchAdditionPercent = first
                ? FirstMasterbatchAdditionPercent
                : SecondMasterbatchAdditionPercent,
        };

    private DualExtrusionProjectPayload CreateExtrusionPayload(bool first) =>
        new()
        {
            ProcessName = first ? "First insulation extrusion" : "Second insulation extrusion",
            ProfileReference = first
                ? "First extrusion working profile"
                : "Second extrusion working profile",
            ProfileRuleVersion = first
                ? "dual-first-line-profile/v1"
                : "dual-second-line-profile/v1",
            LineSpeedBands =
            [
                new DualLineSpeedBandSnapshot
                {
                    MaximumOutsideDiameterMillimetres = first
                        ? FirstProfileMaximumOutsideDiameterMillimetres
                        : SecondProfileMaximumOutsideDiameterMillimetres,
                    LineSpeedMetresPerHour = first
                        ? FirstProfileLineSpeedMetresPerHour
                        : SecondProfileLineSpeedMetresPerHour,
                },
            ],
            AboveMaximumLineSpeedMetresPerHour = first
                ? FirstAboveMaximumLineSpeedMetresPerHour
                : SecondAboveMaximumLineSpeedMetresPerHour,
            UseManualLineSpeed = first ? UseFirstManualLineSpeed : UseSecondManualLineSpeed,
            ManualLineSpeedMetresPerHour = first
                ? FirstManualLineSpeedMetresPerHour
                : SecondManualLineSpeedMetresPerHour,
            SetupTimeHours = first ? FirstSetupTimeHours : SecondSetupTimeHours,
            OperatorCount = first ? FirstOperatorCount : SecondOperatorCount,
            HourlyLabourRate = first ? FirstHourlyLabourRate : SecondHourlyLabourRate,
        };

    private void ApplyMaterialPayload(DualInsulationProjectPayload payload)
    {
        ConductorSupplierQuoteTotal = payload.Conductor.SupplierQuoteTotal;
        ConductorSupplierQuotedKilograms = payload.Conductor.SupplierQuotedKilograms;
        ConductorYieldMetresPerKilogram = payload.ConductorYieldMetresPerKilogram;
        ConductorOutsideDiameterMillimetres = payload.ConductorOutsideDiameterMillimetres;
        ApplyLayerPayload(payload.FirstLayer, first: true);
        ApplyLayerPayload(payload.SecondLayer, first: false);
    }

    private void ApplyLayerPayload(
        DualInsulationLayerProjectPayload layer,
        bool first)
    {
        if (first)
        {
            FirstCompoundSupplierQuoteTotal = layer.Compound.SupplierQuoteTotal;
            FirstCompoundSupplierQuotedKilograms = layer.Compound.SupplierQuotedKilograms;
            FirstCompoundSpecificGravity = layer.CompoundSpecificGravity;
            FirstFinishedOutsideDiameterMillimetres = layer.NominalFinishedOutsideDiameterMillimetres;
            FirstOutsideDiameterToleranceMillimetres = layer.PositiveOutsideDiameterToleranceMillimetres;
            FirstMasterbatchSupplierQuoteTotal = layer.Masterbatch.SupplierQuoteTotal;
            FirstMasterbatchSupplierQuotedKilograms = layer.Masterbatch.SupplierQuotedKilograms;
            FirstMasterbatchAdditionPercent = layer.MasterbatchAdditionPercent;
        }
        else
        {
            SecondCompoundSupplierQuoteTotal = layer.Compound.SupplierQuoteTotal;
            SecondCompoundSupplierQuotedKilograms = layer.Compound.SupplierQuotedKilograms;
            SecondCompoundSpecificGravity = layer.CompoundSpecificGravity;
            SecondFinishedOutsideDiameterMillimetres = layer.NominalFinishedOutsideDiameterMillimetres;
            SecondOutsideDiameterToleranceMillimetres = layer.PositiveOutsideDiameterToleranceMillimetres;
            SecondMasterbatchSupplierQuoteTotal = layer.Masterbatch.SupplierQuoteTotal;
            SecondMasterbatchSupplierQuotedKilograms = layer.Masterbatch.SupplierQuotedKilograms;
            SecondMasterbatchAdditionPercent = layer.MasterbatchAdditionPercent;
        }
    }

    private void ApplyExtrusionPayload(
        DualExtrusionProjectPayload extrusion,
        bool first)
    {
        var band = extrusion.LineSpeedBands.FirstOrDefault();
        if (first)
        {
            FirstProfileMaximumOutsideDiameterMillimetres =
                band?.MaximumOutsideDiameterMillimetres ?? 4;
            FirstProfileLineSpeedMetresPerHour = band?.LineSpeedMetresPerHour ?? 5000;
            FirstAboveMaximumLineSpeedMetresPerHour =
                extrusion.AboveMaximumLineSpeedMetresPerHour;
            UseFirstManualLineSpeed = extrusion.UseManualLineSpeed;
            FirstManualLineSpeedMetresPerHour = extrusion.ManualLineSpeedMetresPerHour;
            FirstSetupTimeHours = extrusion.SetupTimeHours;
            FirstOperatorCount = extrusion.OperatorCount;
            FirstHourlyLabourRate = extrusion.HourlyLabourRate;
        }
        else
        {
            SecondProfileMaximumOutsideDiameterMillimetres =
                band?.MaximumOutsideDiameterMillimetres ?? 4;
            SecondProfileLineSpeedMetresPerHour = band?.LineSpeedMetresPerHour ?? 3000;
            SecondAboveMaximumLineSpeedMetresPerHour =
                extrusion.AboveMaximumLineSpeedMetresPerHour;
            UseSecondManualLineSpeed = extrusion.UseManualLineSpeed;
            SecondManualLineSpeedMetresPerHour = extrusion.ManualLineSpeedMetresPerHour;
            SecondSetupTimeHours = extrusion.SetupTimeHours;
            SecondOperatorCount = extrusion.OperatorCount;
            SecondHourlyLabourRate = extrusion.HourlyLabourRate;
        }
    }

    private void ApplyCalculatedSnapshot(
        DualInsulationCalculatedResultSnapshot snapshot)
    {
        _currentCalculatedResult = snapshot;
        CoreProductionScopeDisplay =
            $"{snapshot.CoreAndFirstLayerProductionLengthMetres:N0} m · finished + start-up";
        SecondLayerProductionScopeDisplay =
            $"{snapshot.SecondLayerProductionLengthMetres:N0} m · finished only";
        MaterialCostDisplay = snapshot.MaterialPriceForProductionRunDisplay;
        MaterialCostPerMetreDisplay = snapshot.MaterialPricePerFinishedMetreDisplay;
        FirstExtrusionLabourDisplay = snapshot.FirstExtrusionLabourCostDisplay;
        SecondExtrusionLabourDisplay = snapshot.SecondExtrusionLabourCostDisplay;
        FirstProductionTimeDisplay = snapshot.FirstProductionTimeDisplay;
        SecondProductionTimeDisplay = snapshot.SecondProductionTimeDisplay;
        TotalLabourDisplay = snapshot.TotalLabourCostDisplay;
        EstimatedCostDisplay = snapshot.EstimatedCostDisplay;
        RiskAdjustedCostDisplay = snapshot.RiskAdjustedCostDisplay;
        RecommendedQuoteDisplay = snapshot.RecommendedQuoteDisplay;
        CombinedRatePriceDisplay = snapshot.CombinedRatePriceDisplay;
        TargetMarginPriceDisplay = snapshot.TargetMarginPriceDisplay;
        CalculationSteps.Clear();
        foreach (var section in snapshot.Trace)
        {
            foreach (var step in section.Steps)
            {
                CalculationSteps.Add(ToRow(step));
            }
        }
    }

    private void ApplyRevisionIdentity(SingleCoreProjectDocument document)
    {
        var normalized = document.Upgrade();
        CurrentProjectId = normalized.ProjectId;
        CurrentRevisionId = normalized.RevisionId;
        CurrentRevisionNumber = normalized.RevisionNumber;
        CurrentRevisionState = normalized.RevisionState;
        CurrentRevisionCreatedAtUtc = normalized.CreatedAtUtc;
        CurrentRevisionApprovedAtUtc = normalized.ApprovedAtUtc;
    }

    private void ApplyModules(IReadOnlyList<CableAddOnModule> modules)
    {
        IncludeTape = modules.Contains(CableAddOnModule.Tape);
        IncludeChalk = modules.Contains(CableAddOnModule.Chalk);
        IncludeFoil = modules.Contains(CableAddOnModule.Foil);
        IncludeBraid = modules.Contains(CableAddOnModule.Braid);
        IncludeLapscreen = modules.Contains(CableAddOnModule.Lapscreen);
        IncludeDrainWire = modules.Contains(CableAddOnModule.DrainWire);
    }

    private IReadOnlyList<CableAddOnModule> SelectedModules()
    {
        var modules = new List<CableAddOnModule>();
        if (IncludeTape) modules.Add(CableAddOnModule.Tape);
        if (IncludeChalk) modules.Add(CableAddOnModule.Chalk);
        if (IncludeFoil) modules.Add(CableAddOnModule.Foil);
        if (IncludeBraid) modules.Add(CableAddOnModule.Braid);
        if (IncludeLapscreen) modules.Add(CableAddOnModule.Lapscreen);
        if (IncludeDrainWire) modules.Add(CableAddOnModule.DrainWire);
        return DualInsulationWorkspaceState.OrderModules(modules);
    }

    private void ApplyCompoundDefaults(CompoundReference? value, bool first)
    {
        if (_suppressInputTracking || value is null)
        {
            return;
        }

        if (first)
        {
            FirstCompoundSupplierQuoteTotal = (double)value.PricePerKilogram;
            FirstCompoundSupplierQuotedKilograms = 1;
            FirstCompoundSpecificGravity = (double)value.SpecificGravity;
        }
        else
        {
            SecondCompoundSupplierQuoteTotal = (double)value.PricePerKilogram;
            SecondCompoundSupplierQuotedKilograms = 1;
            SecondCompoundSpecificGravity = (double)value.SpecificGravity;
        }
    }

    private void ApplyMasterbatchDefaults(MasterbatchReference? value, bool first)
    {
        if (_suppressInputTracking || value is null)
        {
            return;
        }

        if (first)
        {
            FirstMasterbatchSupplierQuoteTotal = (double)value.PricePerKilogram;
            FirstMasterbatchSupplierQuotedKilograms = 1;
        }
        else
        {
            SecondMasterbatchSupplierQuoteTotal = (double)value.PricePerKilogram;
            SecondMasterbatchSupplierQuotedKilograms = 1;
        }
    }

    private void FilterAll()
    {
        FilterCopper();
        FilterFirstCompounds();
        FilterSecondCompounds();
        FilterFirstMasterbatches();
        FilterSecondMasterbatches();
    }

    private void FilterCopper() => CopperOptions =
        DualInsulationWorkspaceState.FilterCopper(
            _allCopper,
            CopperSearchText);

    private void FilterFirstCompounds() =>
        FirstCompoundOptions = FilterCompounds(FirstCompoundSearchText);

    private void FilterSecondCompounds() =>
        SecondCompoundOptions = FilterCompounds(SecondCompoundSearchText);

    private IReadOnlyList<CompoundReference> FilterCompounds(string search) =>
        DualInsulationWorkspaceState.FilterCompounds(
            _allCompounds,
            search);

    private void FilterFirstMasterbatches() =>
        FirstMasterbatchOptions = FilterMasterbatches(FirstMasterbatchSearchText);

    private void FilterSecondMasterbatches() =>
        SecondMasterbatchOptions = FilterMasterbatches(SecondMasterbatchSearchText);

    private IReadOnlyList<MasterbatchReference> FilterMasterbatches(string search) =>
        DualInsulationWorkspaceState.FilterMasterbatches(
            _allMasterbatches,
            search);

    private CopperReference? FindOrCreateCopper(DualInsulationProjectPayload payload)
    {
        var saved = payload.Conductor;
        return _allCopper.FirstOrDefault(item => item.Id == saved.Id) ??
            (string.IsNullOrWhiteSpace(saved.Id) || string.IsNullOrWhiteSpace(saved.Name)
                ? null
                : new CopperReference(
                    saved.Id,
                    saved.Name,
                    saved.Supplier,
                    UnitPrice(saved),
                    ToDecimal(payload.ConductorYieldMetresPerKilogram),
                    ToDecimal(payload.ConductorOutsideDiameterMillimetres)));
    }

    private CompoundReference? FindOrCreateCompound(
        DualInsulationLayerProjectPayload layer)
    {
        var saved = layer.Compound;
        return _allCompounds.FirstOrDefault(item => item.Id == saved.Id) ??
            (string.IsNullOrWhiteSpace(saved.Id) || string.IsNullOrWhiteSpace(saved.Name)
                ? null
                : new CompoundReference(
                    saved.Id,
                    saved.Name,
                    saved.Supplier,
                    UnitPrice(saved),
                    ToDecimal(layer.CompoundSpecificGravity),
                    "Saved revision",
                    "Locked document-local reference"));
    }

    private MasterbatchReference? FindOrCreateMasterbatch(
        DualInsulationLayerProjectPayload layer)
    {
        var saved = layer.Masterbatch;
        return _allMasterbatches.FirstOrDefault(item => item.ColourCode == saved.Id) ??
            (string.IsNullOrWhiteSpace(saved.Id) || string.IsNullOrWhiteSpace(saved.Name)
                ? null
                : new MasterbatchReference(
                    saved.Id,
                    saved.Name,
                    saved.Supplier,
                    UnitPrice(saved),
                    "Saved revision"));
    }

    private void EnsureReferenceOptions(
        CopperReference copper,
        CompoundReference firstCompound,
        CompoundReference secondCompound,
        MasterbatchReference firstMasterbatch,
        MasterbatchReference secondMasterbatch)
    {
        if (!_allCopper.Any(item => item.Id == copper.Id))
        {
            _allCopper = [copper, .. _allCopper];
        }

        foreach (var compound in new[] { firstCompound, secondCompound })
        {
            if (!_allCompounds.Any(item => item.Id == compound.Id))
            {
                _allCompounds = [compound, .. _allCompounds];
            }
        }

        foreach (var masterbatch in new[] { firstMasterbatch, secondMasterbatch })
        {
            if (!_allMasterbatches.Any(item => item.ColourCode == masterbatch.ColourCode))
            {
                _allMasterbatches = [masterbatch, .. _allMasterbatches];
            }
        }

        FilterAll();
    }

    private void Invalidate(string message)
    {
        _currentCalculatedResult = null;
        HasValidationErrors = true;
        CalculationStatus = message;
        CoreProductionScopeDisplay = "—";
        SecondLayerProductionScopeDisplay = "—";
        MaterialCostDisplay = "—";
        MaterialCostPerMetreDisplay = "—";
        FirstExtrusionLabourDisplay = "—";
        SecondExtrusionLabourDisplay = "—";
        TotalLabourDisplay = "—";
        EstimatedCostDisplay = "—";
        RiskAdjustedCostDisplay = "—";
        RecommendedQuoteDisplay = "—";
        CombinedRatePriceDisplay = "—";
        TargetMarginPriceDisplay = "—";
        FirstProductionTimeDisplay = "—";
        SecondProductionTimeDisplay = "—";
        CalculationSteps.Clear();
    }

    private void UpdateRevisionStatus()
    {
        var state = CurrentRevisionState == CostingRevisionState.ApprovedRevision
            ? "Approved"
            : "Working";
        var saveState = HasUnsavedChanges
            ? "unsaved changes"
            : CurrentDocumentPath is null
                ? "not saved"
                : "saved";
        RevisionStatusDisplay =
            $"{state} revision {CurrentRevisionNumber} · {saveState}";
    }

    private void AddSteps(IEnumerable<CalculationStep> steps)
    {
        foreach (var step in steps)
        {
            CalculationSteps.Add(ToRow(step));
        }
    }

    private static CalculationStepRow ToRow(CalculationStep step) =>
        new(
            step.Label,
            step.BusinessMeaning ?? "",
            step.Expression,
            step.SubstitutedExpression,
            $"{step.DisplayValue} {step.Unit}".Trim(),
            step.RoundingRule ?? "",
            step.RuleVersion ?? "",
            step.Warning);

    private static CalculationStepRow ToRow(SavedCalculationStep step) =>
        new(
            step.Label,
            step.BusinessMeaning ?? "",
            step.Expression,
            step.SubstitutedExpression,
            $"{step.DisplayValue} {step.Unit}".Trim(),
            step.RoundingRule ?? "",
            step.RuleVersion ?? "",
            step.Warning);

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

    private static DualMaterialReferenceSnapshot Material(
        string? id,
        string? name,
        string? supplier,
        double quoteTotal,
        double quotedKilograms) =>
        new()
        {
            Id = id ?? "",
            Name = name ?? "",
            Supplier = supplier ?? "",
            SupplierQuoteTotal = quoteTotal,
            SupplierQuotedKilograms = quotedKilograms,
        };

    private static MaterialSupplierQuote Quote(double total, double kilograms) =>
        new(
            new SupplierQuoteTotal(ToDecimal(total)),
            new MassKilograms(ToDecimal(kilograms)));

    private static decimal UnitPrice(DualMaterialReferenceSnapshot material) =>
        material.SupplierQuotedKilograms > 0
            ? ToDecimal(material.SupplierQuoteTotal) /
              ToDecimal(material.SupplierQuotedKilograms)
            : 0m;

    private static decimal ToDecimal(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException("Complete every required numeric input.");
        }

        return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    private static string Pounds(decimal value) =>
        value.ToString("C2", PoundCulture);

    private static string PoundPerMetre(decimal value) =>
        $"{value.ToString("C4", PoundCulture)}/m";

    private static string Duration(decimal hours) =>
        $"{hours:N4} h";
}
