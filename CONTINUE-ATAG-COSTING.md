# Continue ATAG Costing development on another PC

**Handoff updated:** 9 August 2026, after the auditable missing-field derivation,
per-table link-status, and compact conductor-preview follow-up. Central-data workflow windows now use the
app's WinUI title bar and owner-only stacking above ATAG, connection actions
appear before the data previews, Remove link always begins with an explicit
linked-table choice, and the navigation footer shows the exact state of all
five links instead of treating a Copper-only connection as globally live. The
same pure preview geometry now serves retained cached and LIVE imported Copper
rows. Numeric strand constructions use connected triangular/hexagonal packing
instead of circular concentric rings, recursive rope hierarchies retain every
declared level, and the side profile draws only physically exposed strands or
top-level bundles while both cross-section and compressed end face retain every
parsed strand. Supplier-only descriptions without reliable numeric stranding
remain a labelled simplified envelope rather than guessed geometry. The
main window restores its previous monitor, size, position, and maximised state;
every central-data workflow window follows the app's current monitor; all five
link areas remain visible; tabs cannot unlink data; and explicit link removal
keeps the retained source and costing data. This follows full-source-table retention,
Access schema-metadata matching, transform-editor rename/remove-column, the WinUI 3
database Navigator and transform-editor slice, non-blocking division-by-zero
import repair, live Access/SQL provider integration, and atomic per-table
retained-data refresh.
This follows the V1.3a construction/preview refinement, detailed-preview crash
repair, responsive-dock follow-up, second depth/order correction, 7x19 reference
comparison, real annular opening repair, Hudl-launch isolation audit, and
content-height storage-prompt repair. The
second-layer material and production rules remain confirmed. The COR preview
moves between a compact-window bottom dock
and a user-resizable wide-window right rail. The complete scaled-print block is
absent while core print is disabled. Both simplified and detailed side profiles
now place the conductor in front of a hollow insulation annulus instead of
terminating it against a separate conductor-coloured plug. A 7x19 rope is drawn as seven
continuous, gently twisting bundles with fine internal strand cues and matching
end faces. The closed left insulation cap uses the tube's vertical gradient and
only its exposed outer rim. A native WinUI crash triggered by detailed mode was
previously isolated to one `Geometry` instance being attached to two `Path`
elements; each path still owns its own geometry. The x64 build, automated
tests, valid-OD simplified/detailed transition, and detailed-mode stability were
verified on this PC by launching the exact ATAG Costing executable. The
project-owned executable, command launcher, shortcut, and source remain free of
Hudl references. Do not use the generic Computer Use `launch_app` bridge for
future ATAG verification: the bridge reproduced the cross-project launch again
on 1 August even when passed the exact ATAG executable. Hudl was closed
immediately. A subsequent project-scoped process launch opened only an
**ATAG Costing** window and no Hudl window, but the automation window index still
misreported the ATAG window owner as the Hudl executable path and could not
capture it reliably. Both processes were stopped after verification.

## Prompt to give Codex

Copy and send this prompt after opening the folder containing this project:

> Open and read `ATAG Costing/CONTINUE-ATAG-COSTING.md`, then read
> `ATAG Costing/docs/SCOPE.md` and `ATAG Costing/README.md`. Treat those files and
> the existing source as the handoff from my previous PC. Do not recreate the
> solution. Locate the project and workbook using relative paths because the USB
> drive letter may be different. Verify the development prerequisites and that
> the current solution builds before making changes. Then continue with the
> recommended next development slice in the handoff document, keeping all
> calculations auditable and all modules separated.

If the opened workspace is already the `ATAG Costing` directory, use:

> Read `CONTINUE-ATAG-COSTING.md`, `docs/SCOPE.md`, and `README.md`, inspect the
> existing solution, verify it builds, and continue from the recommended next
> development slice. Do not recreate the project or hard-code the USB drive
> letter.

---

## User objective

Build a polished, modular WinUI 3 application to replace
`(WIP Mitchell) Costing Sheet.xlsm`.

The application must make every costing input, assumption, intermediate
calculation, formula, unit, rounding decision, warning, and result easy to find.
Quotes, contract reviews, costing summaries, braid calculations, and printable
documents must be separate modules over one shared calculation model—not copied
worksheets or duplicated formulas.

The workbook remains the reference implementation while its rules are migrated
and verified.

## Portable project locations

Do not assume a drive letter.

When the workspace folder is the USB folder containing both the workbook and the
project:

```text
<workspace>/
  (WIP Mitchell) Costing Sheet.xlsm
  ATAG Costing/
    ATAG.Costing.sln
    CONTINUE-ATAG-COSTING.md
    README.md
    docs/
      SCOPE.md
    src/
```

From inside `ATAG Costing`, the workbook is normally:

```text
../(WIP Mitchell) Costing Sheet.xlsm
```

Always discover the actual workspace path first. Do not replace relative paths
with a hard-coded `D:` path in source code, configuration, documentation, or
build scripts.

## Current milestone

The **first-openable foundation milestone** and the **single-core costing V1**
are complete. The broader workbook migration remains in progress.

Completed and verified:

- Visual Studio WinUI development workload identified and available.
- Developer Mode enabled on the original PC.
- .NET and Windows App SDK dependencies restored.
- Five-project modular solution created.
- Full solution builds with zero warnings and zero errors.
- Native WinUI application launches and remains responsive.
- First-start storage-location screen visually verified.
- Folder selection verified end to end: the chosen path updates in the app,
  enables Continue, and persists to the per-user settings file.
- The ATAG Design cable icon has replaced all WinUI template icon/logo assets and
  is visible in the native application title bar.
- Appearance settings now follow Windows by default while allowing persisted
  Light/Dark and Mica/Acrylic app-specific overrides.
- The workspace root contains an ATAG-icon Windows shortcut and a
  drive-letter-independent launcher.
- Navigation shell and module placeholders implemented.
- Product scope and architecture documented.
- Immutable workbook identity and a reproducible workbook map documented.
- Domain and workbook-parity test projects added to the existing solution.
- The first pure masterbatch usage calculation implemented with an auditable
  trace.
- The general 3% usage boost identified as a waste/start-up allowance and kept
  separate from risk and markup.
- `single-core-material-costing/v1` implemented for conductor, insulation
  compound, masterbatch, quote-length material cost, separate risk, and
  separate markup.
- A usable one-core WinUI costing page implemented from workbook-derived inputs.
- Supplier prices are entered as total quoted price plus quoted kilograms; £/kg
  is derived by the domain calculation.
- Database-controlled yield, conductor OD, and specific gravity are locked.
- Each material card groups its formula summary, quote usage in kg, and cost per
  metre, followed by its live substituted calculation breakdown.
- Single-core inputs automatically recalculate the material cards and complete
  result as values change.
- Production time and labour are calculated from the workbook-derived core-OD
  speed bands, with visible speed override, setup time, operator count, hourly
  rate, cost per metre, quote cost, and live calculation breakdown.
- The app generates the workbook-style core name and provides a deliberate
  custom/customer-name override.
- Sequential risk then markup is the recommended price; additive risk plus
  markup and target gross margin are separately labelled comparisons.
- Contract review V1 consumes the same live costing and records customer scope,
  approval, order acceptance, acknowledgement, and proposed amendments.
- A cable-build menu exposes single insulated core as the working V1 and clearly
  stages dual-insulated, multi-core, and bespoke models.
- Central-data setup maps Copper, Compounds, Masterbatch, Contacts, or Operators
  independently to Microsoft Access or SQL Server. Navigator searches real
  tables/views and shows a row preview; Transform data shows applied steps,
  complete source-column metadata, deliberate keep/remove and rename controls,
  and automatic field matches before import.
- A successful live import retains the complete transformed database object;
  costing records are validated typed projections of that table rather than the
  only imported data kept by the app.
- `#DIV/0!`/division-by-zero cells are non-blocking preview/import blanks with
  diagnostics, so valid cells and later rows continue.
- The local last-successful snapshot is retained when a link is absent or a
  refresh fails; the app includes all rows from those five workbook tables.
- A colour-coded link control beside Settings checks a configured link every 30
  seconds, pauses automatic attempts after failure, and offers manual refresh.
- Conductor selection supports construction, nominal mm², or calculated AWG,
  then class and supplier, with visible exact geometry and discrepancies.
- Material and final calculation traces use the full page width and responsive
  multi-column tiles.
- Portable single-core costing and contract-review documents remain browseable
  independently of central-data availability and are added to the selected
  folder index on save.
- Schema-v2 costing documents add project/revision identity, timestamps,
  explicit working versus approved state, exact result displays, raw quotation
  price, and the complete recursive calculation trace.
- Approved revisions are immutable in Infrastructure and reopen their stored
  outputs/trace rather than silently applying newer rules or refreshed central
  data.
- Saving and opening now use a relative-path index inside the selected
  business-data folder. An unavailable selected folder stops the operation;
  there is no fallback path.
- Editing an approved revision automatically creates the next working revision;
  **Duplicate as new project** creates a new project identity and revision 1.
- The costing header clearly shows revision state and unsaved/validation state.
  Older schema-v1 portable files remain browseable and upgrade on their next
  indexed save.
- Access/SQL discovery, preview, import, and saved-query refresh are implemented;
  business acceptance of the authoritative ATAG schema remains gated until a
  real database copy can be exercised.

## First-start storage requirement

This behaviour is already implemented and should be preserved:

1. On launch, show a screen asking the user to choose a local or network folder.
2. Use that folder as the default root for costings, quote revisions, reports,
   templates, and backups.
3. Continue showing the setup screen at every startup by default, even after a
   valid folder has been chosen.
4. The recurring startup screen can be disabled only through
   **Settings > Storage and files**.
5. Continue may dismiss the screen for the current session but must not silently
   disable the startup preference.
6. Settings can change the location, reopen setup, or restore the prompt.
7. If the saved folder is unavailable, show setup again even when the recurring
   prompt was disabled. Never silently redirect business files.

Preferences are per Windows user and are currently stored at:

```text
%LOCALAPPDATA%/ATAG Design Ltd/ATAG Costing/settings.json
```

That file is intentionally local to each PC and is not expected to travel with
the USB drive. The second PC should therefore show the first-start screen.

### Folder-selection correction completed

The first openable build contained a package-identity incompatibility:

```text
StorageApplicationPermissions.FutureAccessList.AddOrReplace(...)
```

The folder picker itself returned the selected folder, but that next call threw:

```text
System.ArgumentException: The parameter is incorrect.
PackageNameAndPublisherIdFromFamilyName 0
```

`FutureAccessList` is intended for apps with package identity. ATAG Costing is
currently an unpackaged desktop application so it can run from the USB/exFAT
workspace. It already has normal desktop filesystem access and needs only to
persist the selected path.

The packaged-access call and its token were removed from:

```text
src/ATAG.Costing.WinUI/MainPage.xaml.cs
```

The corrected picker now:

1. opens against the WinUI window;
2. receives the selected `StorageFolder`;
3. sends `folder.Path` directly to the view model;
4. validates that the directory exists;
5. updates all one-way UI bindings;
6. persists the path through `JsonAppPreferencesService`;
7. catches and reports selection/persistence errors in the setup screen instead
   of allowing an unhandled asynchronous exception.

Post-fix verification on the original PC confirmed:

- the solution builds with zero warnings and zero errors;
- a real user-selected folder appeared in the running app;
- the same path was written to `settings.json`;
- the selected folder existed;
- the startup-prompt preference remained enabled;
- after restarting, the popup reloaded the selected path, displayed
  **Folder ready**, and presented an enabled **Continue** button;
- production source contains no call to `FutureAccessList` or
  `StorageApplicationPermissions`.

Do not reintroduce `FutureAccessList` while the application remains unpackaged.
If a future packaged/MSIX build is added, package-specific storage access must be
implemented behind an Infrastructure/Application interface rather than added
directly to the WinUI click handler.

### ATAG application icon completed

The approved source artwork is:

```text
../ATAG Design LTD. Icon.ico
```

It is a six-frame, 32-bit icon containing 16, 32, 48, 64, 128, and 256-pixel
versions of the ATAG cable mark. Its SHA-256 hash at handoff is:

```text
615EA67FB8161E05E1E9775FA6D4F26014A09647D4DE7CEB66E72A1DCEA6FCA7
```

The source artwork was preserved exactly rather than regenerated by AI. A
reproducible asset generator was added:

```text
tools/Generate-AppIconAssets.ps1
```

It copies the original multi-frame `.ico` and derives transparent, correctly
sized PNG assets for:

```text
AppIcon.ico
LockScreenLogo.scale-200.png
SplashScreen.scale-200.png
Square150x150Logo.scale-200.png
Square44x44Logo.scale-200.png
Square44x44Logo.targetsize-24_altform-unplated.png
Square44x44Logo.targetsize-48_altform-lightunplated.png
StoreLogo.png
Wide310x150Logo.scale-200.png
```

To regenerate the assets from the `ATAG Costing` directory:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\tools\Generate-AppIconAssets.ps1"
```

The generator updates the copied icon's timestamp deliberately. Without this,
MSBuild can retain the newer template icon in an incremental output directory
because the original company icon has a 2024 timestamp.

Post-generation verification confirmed:

- the project and output `AppIcon.ico` hashes match the approved source hash;
- all PNGs have the required dimensions;
- every PNG uses 32-bit alpha with transparent corners;
- the solution builds with zero warnings and zero errors;
- the coloured ATAG cable mark appears in the native WinUI title bar.

Do not replace the icon with a generated approximation or restore the original
WinUI template assets.

### Visual-style settings completed

Settings now contains an **Appearance** section with:

- **Use system setting**, **Light**, and **Dark** app colour modes;
- **Mica** and **Acrylic** window-material choices;
- direct links to Windows **Colours**, **Themes**, **Text size**, and
  **Contrast themes** pages.

Microsoft's recommended theme wording and Windows colour-settings link were used.
The Windows Settings URIs are:

```text
ms-settings:colors
ms-settings:themes
ms-settings:easeofaccess-display
ms-settings:easeofaccess-highcontrast
```

Appearance preferences are stored in the same per-user `settings.json` file:

```json
{
  "ThemeMode": "System",
  "BackdropMode": "Mica"
}
```

Valid values are:

```text
ThemeMode: System | Light | Dark
BackdropMode: Mica | Acrylic
```

Older settings files remain compatible because both new preference values have
defaults. Changes are applied immediately and saved by `MainPageViewModel`.
`MainWindow.ApplyVisualStyle` owns the native backdrop; `MainPage` maps the stored
theme to `ElementTheme`.

Verification completed:

- the solution builds with zero warnings and zero errors;
- the four Windows-settings buttons are present and enabled;
- visual and accessibility-tree checks confirmed both selectors and all four
  links are visible, enabled, and correctly grouped on the Settings page;
- a Dark preference was selected in the app and persisted to JSON;
- restarting the app preserved that preference;
- the app remained responsive after restart.

Relevant files:

```text
src/ATAG.Costing.Application/Preferences/AppPreferences.cs
src/ATAG.Costing.WinUI/ViewModels/MainPageViewModel.cs
src/ATAG.Costing.WinUI/MainPage.xaml
src/ATAG.Costing.WinUI/MainPage.xaml.cs
src/ATAG.Costing.WinUI/MainWindow.xaml.cs
```

Keep **Use system setting** as the default for new users. Do not read or write
Windows theme registry values directly; use WinUI's `ElementTheme.Default` and
the published `ms-settings:` links.

### Root app shortcut completed

The workspace/USB root contains:

```text
Open ATAG Costing.lnk
Open ATAG Costing.cmd
```

The `.lnk` is the normal user-facing Windows shortcut and uses the ATAG app icon.
It targets the `.cmd` launcher. The launcher resolves its own directory with
`%~dp0`, so its path to the Debug x64 executable does not contain a fixed USB
drive letter.

If the Windows shortcut does not resolve after moving the USB drive to another
PC or drive letter, either open `Open ATAG Costing.cmd` directly or regenerate
the `.lnk` from the `ATAG Costing` directory:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\tools\Create-RootShortcut.ps1"
```

The shortcut currently launches:

```text
ATAG Costing/src/ATAG.Costing.WinUI/bin/x64/Debug/
  net10.0-windows10.0.26100.0/win-x64/ATAG.Costing.WinUI.exe
```

If the target does not exist, the portable launcher displays a clear instruction
to build `ATAG.Costing.sln` using the x64 Debug configuration.

## Solution architecture

Open:

```text
ATAG Costing/ATAG.Costing.sln
```

Projects:

```text
src/ATAG.Costing.Domain
src/ATAG.Costing.Application
src/ATAG.Costing.Infrastructure
src/ATAG.Costing.Reporting
src/ATAG.Costing.WinUI
tests/ATAG.Costing.Domain.Tests
tests/ATAG.Costing.WorkbookParity.Tests
tests/ATAG.Costing.Application.Tests
```

Dependency direction:

```text
WinUI -> Application -> Domain
Infrastructure -> Application + Domain
Reporting -> Application + Domain
Domain.Tests -> Domain
WorkbookParity.Tests -> Domain + approved workbook fixtures
Application.Tests -> Application + Infrastructure
```

Responsibilities:

- **Domain** — pure costing concepts, typed values, calculation rules, validation,
  and calculation traces. It must not depend on WinUI, Excel, filesystems,
  printing, or databases.
- **Application** — use cases, ports/interfaces, orchestration, preferences, and
  saved-costing workflows.
- **Infrastructure** — preference and last-successful-snapshot persistence,
  read-only workbook import, and the Access/SQL Navigator providers.
- **Reporting** — quote, costing, contract-review, and audit-trail template models.
  It consumes approved costing snapshots and must not reproduce formulas.
- **WinUI** — pages, controls, view models, navigation, and application
  composition. Business formulas must not be placed here.

`CalculationStep` now carries business meaning, rounding policy, warnings,
dependencies, and rule-version metadata. The typed domain rules include
`usage-allowance/v1`, `masterbatch-usage-per-metre/v1`,
`single-core-material-costing/v1`, the production-labour calculation,
commercial-price comparisons, and the core-name generator.

## Important source files

```text
src/ATAG.Costing.Domain/Calculations/CalculationStep.cs
src/ATAG.Costing.Domain/Materials/SingleCoreCosting.cs
src/ATAG.Costing.Domain/Materials/DualInsulationCosting.cs
src/ATAG.Costing.Domain/Costing/ProductionLabourCosting.cs
src/ATAG.Costing.Domain/Costing/DualInsulationProductionCosting.cs
src/ATAG.Costing.Domain/Costing/CableConstructionPlan.cs
src/ATAG.Costing.Domain/Costing/CommercialPricing.cs
src/ATAG.Costing.Domain/Costing/CoreNameGenerator.cs
src/ATAG.Costing.Application/CentralData/
src/ATAG.Costing.Application/Projects/
src/ATAG.Costing.Application/Preferences/AppPreferences.cs
src/ATAG.Costing.Application/Preferences/IAppPreferencesService.cs
src/ATAG.Costing.Application/Preferences/StorageLocationPolicy.cs
src/ATAG.Costing.Infrastructure/CentralData/
src/ATAG.Costing.Infrastructure/Projects/
src/ATAG.Costing.Infrastructure/Preferences/JsonAppPreferencesService.cs
src/ATAG.Costing.Reporting/Templates/ReportTemplateDescriptor.cs
src/ATAG.Costing.WinUI/MainPage.xaml
src/ATAG.Costing.WinUI/MainPage.xaml.cs
src/ATAG.Costing.WinUI/ViewModels/MainPageViewModel.cs
src/ATAG.Costing.WinUI/ViewModels/SingleCoreCostingViewModel.cs
src/ATAG.Costing.WinUI/ATAG.Costing.WinUI.csproj
docs/CENTRAL-DATA.md
docs/CABLE-CONSTRUCTION-AND-VISUALISATION.md
docs/PROJECT-REVISIONS.md
```

## Build configuration and the USB filesystem

The original USB workspace used exFAT. Windows cannot register an MSIX package
from an exFAT deployment source, so the WinUI project is configured as an
**unpackaged, self-contained** development app:

```xml
<WindowsPackageType>None</WindowsPackageType>
<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
<SelfContained>true</SelfContained>
<EnableWinAppRunSupport>false</EnableWinAppRunSupport>
```

Preserve this configuration while developing directly from the USB drive. A
signed MSIX or traditional installer can be built later from an NTFS staging
location.

The original development environment was:

- Windows 11;
- Visual Studio Community 2026, version 18.7.3;
- **WinUI application development** workload;
- .NET SDK 10.0.301;
- Windows SDK 10.0.26100;
- project target `net10.0-windows10.0.26100.0`;
- Microsoft Windows App SDK package `1.8.260317003`;
- CommunityToolkit.Mvvm `8.4.2`;
- Developer Mode enabled.

Exact tool versions can be newer on the second PC, but do not retarget or upgrade
packages until the existing solution has first been built unchanged.

## Verification commands

Run these commands from the `ATAG Costing` directory:

```powershell
dotnet --info
dotnet build "ATAG.Costing.sln" -c Debug -p:Platform=x64
dotnet test "ATAG.Costing.sln" -c Debug -p:Platform=x64
dotnet run --project "src\ATAG.Costing.WinUI\ATAG.Costing.WinUI.csproj" -c Debug -p:Platform=x64
```

If Windows cannot authenticate to NuGet but the required packages are already
installed for the current user, restore from that existing local cache without
hard-coding a user or drive path:

```powershell
dotnet restore "src\ATAG.Costing.WinUI\ATAG.Costing.WinUI.csproj" `
  --runtime win-x64 `
  --packages "$env:USERPROFILE\.nuget\packages" `
  --ignore-failed-sources
```

Expected result:

- build succeeds with zero warnings and zero errors;
- domain tests pass without Excel;
- workbook-parity evidence tests pass and any unapproved golden case is reported
  as skipped rather than silently accepted;
- a native **ATAG Costing** window opens;
- storage setup covers the main page;
- no folder is selected on a new PC;
- Continue is disabled until a valid folder is chosen;
- choosing a folder immediately displays its path and enables Continue;
- restarting the app reloads that same path;
- Settings contains **Show storage setup when app starts**.

If the app compiles but will not launch:

1. confirm Developer Mode is enabled;
2. confirm the Visual Studio **WinUI application development** workload;
3. confirm the x64 platform is being used;
4. confirm the unpackaged/self-contained properties above are still present;
5. inspect `%TEMP%/ATAG-Costing-startup.log`;
6. inspect the Windows Application event log for .NET or application errors.

The original packaged launch failure was:

```text
Windows cannot deploy to path AppX of file system type exFAT. (0x80073CFD)
```

Do not attempt to fix that error by moving or recreating the source project.
Use the existing unpackaged configuration.

## Workbook audit already completed

The source workbook was inspected as a standalone file.

Important findings:

- 54 worksheets;
- 21 visible and 33 hidden sheets;
- approximately 1,565 formulas;
- 9 Excel tables, 6 defined names, and 5 Power Query outputs/connections;
- embedded VBA project with 68 extracted modules and 14 non-document code
  modules;
- repeated costing-sheet families;
- contract-review content;
- braid-calculator logic;
- reference data for copper, compounds, masterbatch, contacts, operators, and
  other costing inputs;
- Power Query-backed tables or connections.

The immutable source identity, full sheet/table/name/connection/VBA/formula map,
rounding cells, error/default behaviour, first source-cell map, and reproducible
inspection command are now in `docs/WORKBOOK-SPECIFICATION.md`.

Two cached `#DIV/0!` results were identified:

```text
SBS1DualInsMBPrice2!AV31
SBS2FlatMBPrice2!AV31
```

Do not use those results as approved parity examples until the underlying inputs
or intended error behaviour have been clarified.

Previously identified business rules that must be retained:

- the general 3% multiplier is a waste/start-up usage allowance, not risk,
  markup, or margin;
- risk and markup are sequential, separately visible steps;
- markup and margin are different concepts;
- braid coverage needs reverse-calculation support where applicable;
- print composition must be modular;
- quotes and contract reviews consume saved costing revisions rather than owning
  their own formulas.

The workbook contains hidden sheets and VBA, so formula-cell extraction alone is
not a complete specification.

## Single-core V1.2 working document and recommended next development slice

The first usable vertical slice is complete. Do not recreate it.

Implemented:

- `usage-allowance/v1`;
- `masterbatch-usage-per-metre/v1`;
- `single-core-material-costing/v1`;
- typed conductor, compound, masterbatch, contact, and operator reference
  records;
- one-core conductor, annular insulation, masterbatch, quote, risk, and markup
  calculations with ordered traces and no intermediate rounding;
- workbook-derived starter inputs for the discovered `COR1` example;
- a guided WinUI costing page for one core with supplier total/quoted-kg entry;
- locked database values for conductor yield/OD and compound specific gravity;
- quote kg, formula summary, cost per metre, and an expanded live substituted
  calculation breakdown grouped with each material;
- workbook-derived production line-speed bands, running/setup time, operator
  count, hourly labour rate, labour cost per metre, labour quote total, and an
  expanded live labour calculation breakdown;
- generated workbook-style core naming plus an explicit custom/customer-name
  override;
- three clearly labelled commercial-price methods: sequential risk then markup,
  additive risk plus markup, and target gross margin;
- a first contract-review screen using the same live result for customer scope,
  materials, labour, estimate approval, order acceptance, acknowledgement, and
  proposed amendments;
- a cable-type build menu with single core active and future cable types staged;
- a full embedded last-successful central-data snapshot containing 322 Copper,
  74 Compounds, 203 MasterbatchCodeList, 567 Contacts, and 5 Operators rows;
- labelled, scrollable working-table views for all five cached tables;
- a Copper/Compounds/Masterbatch/Contacts/Operators Access/SQL setup followed by
  searchable table/view Navigator preview and a transform editor with applied
  steps and automatic field matching;
- independent saved queries and atomic per-area imports that retain the previous
  table whenever connection, reading, or validation fails;
- non-blocking `#DIV/0!` cell handling, trim-text and remove-blank-row steps,
  preview/error counts, and saved-query refresh;
- a colour-coded material-link control beside Settings, 30-second automatic
  checks, automatic-check pause after failure, and manual **Refresh link**;
- conductor choice by strand construction, nominal mm², or calculated AWG,
  followed by copper class and supplier;
- simple and rope-lay (`7x19/0.32`) parsing, exact metallic-area calculation,
  display-only strand-diameter normalisation (`7/0.196` -> `7/0.20`), nearest
  AWG, geometry-based class evidence, and visible area discrepancies;
- full-width material expanders and final calculation trace with responsive
  multi-column calculation tiles;
- conductor finish/material type is selected separately, including TCW, PCW,
  titanium, tinsel, and other supplier-defined constructions;
- all cached masterbatch colours are available even when a price is missing,
  with selector swatches, a selected-colour preview, compound-family
  compatibility, recorded temperature limits, and workbook notes;
- OD tolerance uses one linked ± input by default and supports explicit
  asymmetric positive and negative values;
- detailed material calculations and the final trace are collapsed by default;
  a compact flow view, pinned result, and section-jump menu preserve access;
- GBP remains the costing basis, while a locally retained ECB reference-rate
  snapshot supports a clearly labelled quotation-currency total;
- the Reporting project generates a self-contained, single-page A4 PDF
  quotation from application values and contains no costing formulas;
- a versioned `.atagcosting` working document with atomic Infrastructure JSON
  save/load behind an Application interface;
- saved costing inputs, locked material values, rule identifiers, customer
  naming, and contract-review fields remain reopenable while the database link
  is offline;
- application tests for snapshot retention, five-table import, legacy
  three-table cache upgrade, independent database links, project-document
  round-trip, conductor type classification, retained exchange rates, A4 PDF
  structure/currency encoding, and the internal workbook migration adapter.

The 3% is confirmed as one general waste/start-up usage allowance. V1 applies it
once to each material stream. The extra price-side `*1.03` in
`COR1MBPrice!B34` is not migrated.

The user also confirmed that the second masterbatch addition in
`CORT1Summary!X12` is wrong. V1 includes masterbatch once in the material
subtotal, applies risk, then applies markup. With the built-in `COR1` inputs,
0% risk, and 45% markup, the corrected one-core quote is £161.71 rather than
the workbook's £162.67.

The discovered `COR1MBPrice` fixture remains `PendingBusinessApproval`; one
golden parity test is intentionally skipped. Do not mark it approved without an
explicit ATAG business approval.

## Accepted single-core UX and quotation refinement checklist

The following requirements were reconfirmed from an in-app visual review on
29 July 2026. Preserve this complete list across PCs even when only part of the
slice is implemented in one session.

- Put the real one-core result card at the top of the costing workspace so the
  user sees the outcome before filling the sections.
- Give the result card compact icon actions for:
  - pinning it while the main costing page scrolls;
  - opening a genuinely resizable movable result window, rather than showing
    only a second small summary strip.
- Keep the existing **Jump to section** button and add the same section list as
  a costing-workspace submenu in the left navigation pane.
- Use subtle compatible/incompatible/not-recorded background colour states in
  the masterbatch compatibility tiles; do not rely on small glyphs alone.
- Show calculation workings as a visible dependency flow. Responsive tiles are
  useful inside each stage, but an unstructured tile grid is not itself a flow.
- Rename **Material data** to **Live Data** and keep the retained Copper,
  Compounds, Masterbatch, Contacts, and Operators working tables there, not in
  Contract Review.
- Working tables must use the available width and preserve readable minimum
  column widths with horizontal scrolling when necessary. Do not squeeze every
  column until users cannot confidently inspect the retained data.
- Ensure **Costing carried into review** displays the quote values for every
  conductor, compound, masterbatch, and labour row, not only the labour total.
- Recompose **Commercial comparison** as compact aligned cards/tiles.
- Risk, markup, and target-margin controls must bind to the one shared costing
  values so a change from Costing or Contract Review updates every display and
  calculation that depends on it.
- Customer selection in the core-name area must use the retained Contacts
  table and populate the customer name, short name, and delivery address.
- Every data-backed selector should be editable/searchable. Colour selection
  should additionally provide a dedicated visual search experience comparable
  to the workbook VBA colour form.
- The customer quotation must visually and verbally follow the approved
  workbook quotation rather than merely borrowing its section names.
- Quotation inputs must include reel count and metres per reel; remove the
  unnecessary batch-size field.
- Quotation conductor wording should be concise, for example
  `7/0.20 TCW`, with optional AWG and mm². When those representations conflict,
  the user chooses which representation is printed.
- Quotation insulation wording should be a simple family such as `PVC` or
  `LS0H/LSZH`, not the internal compound record name.
- Quotation colour is a close-enough generic name by default. An explicit
  customer-request checkbox prints the exact retained colour name/code.
- Make the source of special notes explicit and editable.
- Provide an editable A4 quotation preview before PDF save so final wording can
  be adjusted deliberately without changing the costing calculation.

### Status of the 29 July refinement slice

Completed in this slice:

- the real live result is now the first costing card, with compact pin and
  pop-out icon actions;
- pinning keeps the shared live values visible while the page scrolls, and
  pop-out opens the same bound result in a movable, resizable, always-on-top
  window;
- the Open, Save, Revision actions, Jump to section, and cable-type controls
  remain above the scrollable costing content;
- the left navigation now contains the costing-section submenu while the
  existing **Jump to section** control remains available;
- masterbatch compatibility tiles now use subtle green, red, and amber states
  for compatible, not-listed, and not-recorded data;
- conductor, compound, masterbatch, and labour workings are grouped into
  source, derived, and result stages; every derived tile names the upstream
  values it consumes and each stage visibly feeds the next;
- direct source-value tiles no longer repeat an unformatted raw value beside
  the correctly formatted result;
- **Material data** is renamed **Live Data** and the five retained working
  tables are moved there at runtime, with a readable minimum table width and
  horizontal scrolling;
- every material and labour row in **Costing carried into review** now displays
  its corresponding quote value;
- **Commercial comparison** is recomposed as aligned compact cards. All risk,
  markup, and target-margin controls continue to bind to the same view-model
  properties used by Costing and Contract Review;
- the core-name and Contract Review customer selectors now provide typed
  Contacts-table search with readable account/short-name/postcode suggestions
  and use the selected row to populate customer details;
- masterbatch colour selection provides name/code/supplier/RAL/type search,
  workbook-derived HSL searches such as `dark blue`, `warm pastel`, `muted`,
  and `navy`, small-error text matching, family/tone and colour-type filters,
  colour swatches in the results, and a selected-colour preview;
- the Masterbatch Live Data view uses fixed shared column widths. Its final
  column shows each compound family above its temperature and leaves
  incompatible temperature cells blank rather than combining uneven text;
- quotation inputs now include reel count and metres per reel, and the obsolete
  batch-size output is removed;
- quotation conductor wording, conductor representation, simple insulation
  family, generic-versus-exact colour wording, packaging, delivery, special
  notes, and terms are explicit editable inputs;
- reel settings, quotation wording choices, notes, and terms are retained in
  the portable `.atagcosting` document;
- the A4 PDF uses the reel plan and simplified material wording without
  containing costing formulas.

Still required in a following refinement slice:

- convert the remaining conductor, compound, supplier, operator, and other
  data-backed dropdowns to genuine typed-search selectors;
- if user testing still benefits from it, expand the implemented semantic HSL,
  family/tone, type, fuzzy-text, and swatch search into a larger palette-style
  colour browser;
- carry the completed dependency-stage pattern into dual-insulation and future
  cable constructions as they are added;
- create a true editable, page-accurate A4 preview before save, and finish the
  approved quotation branding, spacing, logo, and exact wording against the
  reference sheet;
- visually review the always-on-top result window, sticky command strip,
  dependency stages, readable Contacts suggestions, semantic colour filters,
  aligned compatibility cells, navigation submenu, wide tables, and quotation
  fields in the ATAG app. This handoff update deliberately does not claim user
  visual approval.

### Status of the 29 July follow-up refinement

The latest user review identified six concrete corrections and they are now
implemented:

- flat per-tile arrows were replaced by dependency-depth stages derived from the
  existing recursive `CalculationStep` inputs; no costing formula was moved
  into WinUI;
- the live result pop-out uses WinUI's `OverlappedPresenter.IsAlwaysOnTop` while
  retaining normal move and resize behaviour;
- the costing action row is outside `CostingWorkspaceScroller`, so scrolling the
  costing sections does not move Open, Save, Revision actions, Jump to section,
  or the cable selector off screen;
- `ContactReference` and both customer selectors now render account names
  deliberately instead of exposing the record's generated property dump;
- the workbook VBA HSL search semantics were retained and expanded to combined
  terms, group filters, colour-type filters, additional useful families, and
  one-edit text tolerance;
- the retained Masterbatch table now presents eight consistently aligned
  compound-family cells, each with its temperature below the family name and a
  blank temperature for incompatible material families.

Verification for this follow-up:

- `dotnet restore` succeeded using the current user's portable
  `%USERPROFILE%\.nuget\packages` cache after stale restore assets were found;
- the complete x64 Debug solution builds with **0 warnings and 0 errors**;
- 35 Domain tests pass;
- 31 Application/Infrastructure/Reporting tests pass, including the new colour
  search and compatibility-cell cases;
- 2 parity-evidence tests pass and 2 golden parity tests remain intentionally
  skipped pending business approval;
- maintained source, tests, tools, README, scope, and handoff contain no
  unrelated product/device identifiers, current workspace drive-letter, or
  current-user absolute path matches;
- the WinUI app was not launched during this correction pass, preventing any
  unrelated desktop console from being opened and leaving visual acceptance to
  the next deliberate ATAG app review.

## V1.3a dual-insulation domain and parity evidence — completed 29 July 2026

The recommended pure-domain slice is complete and remains deliberately outside
WinUI until the construction document model is versioned.

### Confirmed construction rule

The mapped dual case has two distinct production scopes:

- conductor and first insulation: 10,000 m finished quote length plus 200 m
  core start-up, for 10,200 m total;
- second insulation: the 10,000 m finished quote length only.

The user confirmed that the general 3% waste/start-up allowance applies once to
every material stream in both layers. Any second `*1.03` applied to a downstream
price is accidental, and any material/masterbatch value added a second time is
also accidental.

### Implemented domain model

- `MaterialCostingFormulas` centralises the unrounded conductor, circular,
  annular, compound, allowance, length, and price primitives already shared by
  single-core and dual-insulation calculators;
- `DualInsulationCostingCalculator` uses the existing typed supplier quote,
  conductor yield/OD, compound SG/OD/tolerance, masterbatch, allowance, and
  calculation-trace types;
- first-layer material usage is calculated over core production length;
- second-layer annular geometry and usage are calculated over finished length;
- both masterbatch streams use the existing mass-based addition rule;
- the production-run material subtotal groups the core/first-layer run and
  second-layer finished run, adding each once;
- finished-metre material price distributes the complete production-run cost
  across customer-delivered metres;
- recursive steps expose both length scopes, geometry dependencies, supplier
  quote derivations, allowance-once usage, layer subtotals, and final subtotal.

The rule identifier is:

```text
dual-insulation-material-costing/v1
```

### Workbook evidence and approval gate

The read-only workbook map covers:

```text
SBS1DualInsCopperPrice
SBS1DualInsCompPrice1
SBS1DualInsMBPrice1
SBS1DualInsCompPrice2
SBS1DualInsMBPrice2
SBS1DualInsSummary
```

The fixture
`tests/ATAG.Costing.WorkbookParity.Tests/Fixtures/dual-insulation-sbs1.json`
records the current observed workbook hash, source cells, confirmed corrections,
and these workbook defects:

- first- and second-layer masterbatch quote paths contain repeated 3%
  multipliers;
- the summary adds combined masterbatch again after it is already in the
  material subtotal;
- second-layer masterbatch takes a compound-litre value and presents the same
  number as kilograms.

The repeated allowance paths, repeated summary addition, and the
volume-labelled-as-mass path are confirmed defects. OQ-006 is resolved:
masterbatch is mass-based in both layers and adds material amount/cost only.
The golden test remains skipped until the complete workbook identity, source
map, and corrected expected case are approved together. The current observed
workbook hash does not replace the immutable earlier single-core baseline.

### Verification

- 35 Domain tests pass, including seven dual-insulation normal, allowance-once,
  single-addition, length-scope, trace, invalid, and boundary cases;
- 2 workbook-parity evidence tests pass;
- 2 golden workbook-parity tests remain intentionally skipped pending business
  approval;
- the complete x64 Debug solution builds with zero warnings and zero errors;
- the app was not launched while completing this calculation slice.

## V1.3a confirmed production and construction planning follow-up — completed 29 July 2026

### Confirmed fault resolutions

The user explicitly confirmed:

- `SBS1DualInsMBPrice2!V30` is wrong to take compound litres and present the
  result as masterbatch kilograms;
- every extra downstream `*1.03` is wrong because the general 3% allowance has
  already been applied to material usage;
- adding a masterbatch or other material after it already entered the subtotal
  is wrong;
- both compounds enter the final material cost once;
- the 200 m core start-up is internal and affects conductor/first insulation
  only; second insulation covers the 10,000 m finished quote;
- copper appears once;
- masterbatch contributes material amount and cost only and has no independent
  process time.

OQ-006 is moved to the resolved section of `docs/OPEN-QUESTIONS.md`. The dual
fixture records the decisions but remains `PendingBusinessApproval` for the
complete mapped case; formula confirmation was not misrepresented as approval
of workbook identity and every expected output.

### Independent extrusion production

`ExtrusionLineSpeedProfile` holds a line-specific, strictly ordered
outside-diameter-to-speed table. `ProductionLabourCalculator` can now use a
selected profile while preserving the original single-core default policy and
manual override.

`DualInsulationProductionCalculator` calls the shared production calculator
twice:

- first extrusion: core/first-layer production length and its own profile,
  set-up, operators, and rate;
- second extrusion: finished-only length and its independent profile, set-up,
  operators, and rate.

Its trace prefixes both processes and adds their labour costs once. There is no
masterbatch production-time input or step.

### Construction chooser and ordered module plan

- The Home page generic start action is replaced by descriptive COR, Dual
  insulated, Flat cable, and D-shape cable tiles.
- COR opens the working single-core page.
- Dual opens a first construction planner with Tape, Chalk, Foil, Braid,
  Lapscreen, and Drain wire checkboxes.
- Selected modules become visible after first insulation and before second
  insulation in the inside-to-outside flow.
- Flat and D-shape are clearly labelled planned and target up to ten in-line
  cores.
- `CableConstructionPlan` preserves ordered stages, rejects duplicate modules,
  and enforces the current construction/core-count boundaries.

The full dual editable form, saved schema, and calculation-result UI are not
claimed complete.

### Visualisation requirements

`docs/CABLE-CONSTRUCTION-AND-VISUALISATION.md` records the opt-in scaled
cross-section and side-profile contract, exact versus simplified strand views,
rope-lay grouping, layer colours, foil/tape direction, braid versus lapscreen,
drain-wire centring, later lay-length use, and Flat/D-shape targets. Rendering
remains off by default. The COR cross-section and side profile now support both
simplified and parsed all-strand detail; the shared Dual/Flat/D-shape renderer
remains future work.

### Construction-icon and COR-preview refinement

- Home construction tiles use code-native vector silhouettes:
  - COR: conductor plus one concentric insulation layer;
  - Dual: conductor plus two concentric insulation layers;
  - Flat: central in-line single cores in a rounded rectangular profile;
  - D-shape: raised in-line single cores above the flat base of a
    flat-bottomed domed profile.
- All four construction buttons stretch to the same two-column grid and
  consistent minimum height instead of sizing themselves around their content.
- The single-core command strip retains Open, Save, Revision actions, and Jump
  to section, but no longer repeats cable-type selection.
- The single-core workspace heading identifies the already-selected COR
  construction instead of asking the user to choose again.
- The opt-in COR preview uses a responsive dock outside the costing scroller.
  At wide sizes it is a right-hand rail with a draggable divider and retained
  user width; at compact sizes it moves to a bottom row instead of squeezing
  the costing form. The off-state bottom dock stays shallow and expands when
  the user enables the preview.
- The fixed-coordinate cross-section and side-profile canvases are hosted in
  uniform-scaling viewboxes, so their content follows the resized dock width.
  When enabled, the print-repeat cylinder remains horizontally scrollable
  because its axial length represents the configured repeat. Its heading,
  cylinder, scale, and specification are all collapsed when print is disabled.
- The conductor ratio follows retained conductor OD versus nominal finished OD.
- The preview follows selected insulation colour and retained conductor finish;
  a dashed ring represents the positive OD tolerance.
- Detailed cross-section mode draws every parsed strand. Rope entries retain
  group boundaries, so `7x19/0.32` draws seven 19-strand groups.
- The side profile exposes the conductor beyond a straight insulation tube.
  Parallel body edges replace the earlier capsule shape, while compressed
  insulation and conductor end faces represent a view about 20 degrees off
  side-on.
- The detailed 7x19 side view now uses seven coherent rope bundles, matching the
  reviewed customer drawing. Every group is one continuous, gently twisting
  surface from the insulation opening to its compressed end face. Three fine
  longitudinal strokes suggest its 19 strands without creating high-frequency
  noodle paths or false extra layers. Packed radial position controls the
  bundle route, while a stable subtle shade carries it to the matching
  final-rotation end face.
- The insulation cut face is a true hollow annulus. Its base-colour and
  gradient ring, outer rim, and inner rim form the rear cut face; the conductor
  body and detailed bundles are painted in front. This one layer order is used
  in simplified and detailed modes and removes the former conductor-coloured
  plug and untidy bundle stack at the opening.
- The closed insulation end uses an un-stroked fill plus only the exposed
  outside rim. The previous full ellipse line through the coloured tube is not
  rendered, and the cap now uses the same vertical highlight/shadow gradient as
  the insulation body.
- `single-core-wall-guidance/v1` shows the calculated radial wall separately
  from published PVC or LS0H/LSZH H05/H07 manufacturer comparators. A nearest
  out-of-range comparator is clearly labelled and never claimed as
  certification or as a universal minimum.
- A new **Core print** section stores enabled state, text, colour, character
  height, start-to-start repeat distance, and horizontal/vertical dot pitch.
  Enabled print is shown on a separate smaller cylinder below the construction
  side profile. Character height is proportional to finished OD; the second
  copy is positioned at the configured axial repeat and the preview grows
  inside a horizontal scroller. Very long repeats use a labelled reduced axial
  scale. When print is disabled, the entire scaled-print block is absent. No
  unverified print cost or process time has been added.
- The new print fields are optional schema-v2 properties, so legacy schema-v1
  and existing schema-v2 readers remain compatible. Schema v3 is still reserved
  for the construction discriminator and dual-insulation payload in V1.3b.
- The preview remains collapsed until explicitly enabled.

### Detailed-preview crash and responsive-dock follow-up

- Live reproduction showed that simplified preview mode was stable and that
  switching **Conductor rendering** to **Detailed strands** closed the native
  WinUI process.
- Windows Error Reporting recorded `Microsoft.UI.Xaml.dll` failures with
  `0xc000027b`; one report resolved the underlying COM error to
  `0x80020012` (`DISP_E_DIVBYZERO`).
- The detailed side renderer was assigning the same mutable WinUI `Geometry`
  dependency object to both an outline `Path` and a fill `Path`. It now creates
  an independent `PathGeometry` or `LineGeometry` for every visual path.
- The corrected exact x64 Debug executable remained open after detailed mode
  was enabled. The normal compact window showed the bottom dock; maximising the
  same window moved the preview to the right, and dragging the divider enlarged
  the rail while the cross-section, side profile, and print preview responded
  to the available width.
- A later user review found that the full-strand rear/centre/front passes still
  produced incorrect overlap at individual crossings and that disabled print
  left an empty cylinder visible. The first correction depth-sorted short helix
  sections, aligned the end face to the final helix rotation, and painted the
  conductor over the insulation cut face. Live print-on and print-off checks
  confirmed that the entire scaled-print block follows `HasCorePrint`.
- A subsequent visual review showed that those short depth sections created
  scalloped false layers and obscured which end circle belonged to which
  strand. The current rope renderer uses one continuous, gently twisting
  bundle per group, fine longitudinal internal-strand cues, and a matching
  final-rotation end face. The left insulation cap draws only its exposed outer
  rim and uses the body gradient.
- Setting the reviewed H72 rope example to its valid 8.000 mm finished OD
  exposed a separate shared join defect: the old side profile used a
  conductor-coloured oval as a fake opening, then painted detailed bundles over
  it. The opening is now a real hollow annulus behind the conductor, so
  simplified and detailed modes share the correct physical layer order.

### BAC 7x19 AWG evidence reviewed

The open email attachment `Specification #7 Ins Railbond cable.pdf` was
inspected read-only on 29 July 2026. Page 1 records:

- specification title: **#7 AWG Bronze Copper (PVC)**;
- construction: **7 bundles of 19 strand #28 AWG**;
- individual nominal diameter: **0.321 mm**;
- bunch outside diameter: **5.00 mm ± 0.05 mm**;
- insulation nominal OD: **8.00 mm ± 0.20 mm**;
- minimum wall: **1.50 mm**.

These values agree with the application's retained `133/0.32` H72 record:
`133 × π × 0.32² ÷ 4 = 10.696495 mm²`, nearest overall **AWG 7**. A 0.321 mm
individual strand is **AWG 28**. The same drawing also contains a separate
arrow callout saying **24 AWG**; that callout conflicts with its title,
construction table, strand diameter, and total area. Treat 24 AWG as a
source-document conflict and do not copy it into central data unless an ATAG
business reviewer supplies corrected evidence.

### Hudl isolation audit

- Before any deliberate application launch, Computer Use showed ATAG Costing
  stopped and Hudl Device Console stopped.
- Maintained ATAG source, tests, tools, documentation, and the portable root
  command launcher contain no `Hudl`, `HudlOS`, or `HudlDeviceConsole`
  reference.
- The ATAG startup log contains only ATAG process/WinUI lifecycle entries.
  Windows Application events for the last seven days contained no
  `HudlDeviceConsole` entry.
- `Open ATAG Costing.cmd` launches only the relative ATAG executable. The root
  shortcut target and working directory were already portable, but its icon
  metadata still pointed at an old fixed `D:` path. Running the existing
  `tools/Create-RootShortcut.ps1` regenerated the shortcut so target, working
  directory, and icon now all resolve on the current workspace drive.
- The exact rebuilt ATAG x64 Debug executable was launched twice during live
  verification. Hudl remained stopped with no windows before and after both
  project-scoped launches. No Hudl project/source file, launcher, or
  configuration was modified.
- A later verification launch through the generic Computer Use `launch_app`
  bridge reproduced the unwanted cross-project behaviour: both separately
  indexed applications were started even though ATAG's exact process path was
  requested. Hudl was closed immediately. This is an automation-launcher hazard,
  not a maintained ATAG reference or root-shortcut route. **Do not use
  `launch_app` for ATAG.** Launch via `Open ATAG Costing.cmd`, the repaired root
  shortcut, or a project-scoped direct process command after confirming Hudl is
  stopped.

### Verification

- the complete x64 Debug solution builds with zero warnings and zero errors;
- 45 Domain tests pass, including independent extrusion profiles/run lengths,
  absence of masterbatch process time, construction ordering, duplicate
  rejection, ten-core Flat/D-shape bounds, calculated radial wall, direct
  published-size matching, and out-of-range comparator labelling;
- 31 Application/Infrastructure/Reporting tests and 2 parity-evidence tests
  pass, including core-print document round-trip; 2 golden tests remain
  intentionally skipped;
- workbook parity stays approval-gated;
- the reference workbook was not changed;
- the exact native ATAG Costing x64 Debug app was launched deliberately. It
  remained stable in detailed mode; print enabled inserted the cylinder, scale,
  and measurements, while print disabled removed that complete block;
- the exact native app remained stable while the live preview and detailed
  conductor were toggled repeatedly. Maximised mode used the resizable
  right-hand rail, the restored 868 x 567 window reported **Bottom dock ·
  compact window**, and the final live state was maximised with the preview and
  detailed strands enabled;
- live inspection at a 5.000 mm conductor and valid 8.000 mm finished OD
  confirmed that only the exposed left insulation rim remains, the left cap
  shares the body gradient, the simplified and detailed conductor pass in front
  of a hollow annular opening, the former plug/stack artefact is absent, and each
  continuous rope bundle reaches its identifying end face;
- the rebuilt preview remained stable while simplified/detailed mode was
  toggled repeatedly;
- the storage setup panel now has content height inside its full-screen overlay;
  it remains centred and scrollable without stretching to the window height;
- the project-scoped exact-ATAG live runs left Hudl stopped. One later generic
  Computer Use `launch_app` bridge invocation started Hudl as well as ATAG; Hudl
  was closed immediately, the unsafe bridge route is now explicitly forbidden
  above, and both applications were stopped at handoff.

## V1.2b immutable revisions — completed 29 July 2026

The complete bounded V1.2b slice is implemented. See
`docs/PROJECT-REVISIONS.md` for the storage and lifecycle contract.

### Step 1 — promote the working document to an immutable revision — complete

The portable working document already contains:

- selected material identifiers plus copied last-known names and suppliers;
- all entered rates, quote length, 3% usage allowance, risk, and markup;
- the locked conductor yield/OD and compound specific gravity used;
- production speed mode/value, setup time, operator count, hourly labour rate,
  target margin, generated/custom name inputs, and customer fields;
- the associated contract-review fields and decision states;
- rule/version identifiers and the central-data snapshot revision used.

Schema version 2 now adds project/revision identity, saved calculated result and
complete recursive trace, created/updated/approved timestamps, and explicit
working-copy versus approved-revision state. The JSON document store rejects
any attempt to overwrite a saved approved revision.

### Step 2 — bind revisions to the selected business-data folder — complete

`ATAG-Costing-Index.json` and all indexed costing paths are relative to the
selected business-data folder. The repository validates containment, refuses an
unavailable root, and never falls back to another folder. The Open dialog can
still browse a legacy portable file and then index it through **Save costing**.

### Step 3 — add duplicate and unsaved-change state — complete

The single-core header now shows revision and save/validation state. Duplicate
creates a new project identity and revision 1. The first edit to an approved
revision starts the next working revision without changing the approved source.
Approved revisions restore stored inputs, outputs, effective name,
contract-review state, and trace while the live central-data link is offline.

### Step 4 — preserve the central-data boundary — expanded and retained

The workbook importer remains an internal migration/test adapter, not a runtime
central-data source. Access and SQL providers now perform real catalogue
discovery, table/view preview, full-object import, and saved-query refresh.
Navigator never asks the user to choose columns. Transform data exposes applied
steps and reviewable automatic ATAG matches. The complete selected area and link
definition commit together only after validation; other retained areas are not
discarded. Real authoritative schema acceptance remains gated. See
`docs/CENTRAL-DATA.md`.

### Step 5 — retain the approval gate — retained

Review the pending parity fixture when the user is ready, but do not equate the
successful single-core domain tests with business approval of that golden case.

## Full retained database objects and schema-aware transform follow-up — completed 8 August 2026

The live import boundary was corrected after the first real Access import was
reviewed. The earlier importer retained every typed Copper/Compound/Masterbatch/
Contact/Operator row but discarded source columns which had no current typed
field. That made future field corrections unnecessarily destructive and made an
Access physical-name-versus-business-label mismatch difficult to diagnose.

Implemented:

- every successful Access or SQL import now commits the complete transformed
  database object: all rows, all kept columns, physical source names, available
  captions/descriptions, provider type/nullability, cell diagnostics, and query
  steps;
- the existing typed material/contact/operator records remain for safe costing
  and backwards compatibility, but are explicitly generated as a validated
  projection of that full retained table;
- the full table, typed projection, link, field mappings, and transformation
  steps commit together in one atomic state-file replacement; a provider,
  transform, or required-mapping failure retains the earlier complete table and
  earlier costing records;
- the Access Navigator reads the OLE DB `COLUMNS` schema rowset and carries the
  physical column name plus available caption/description metadata into the
  preview and matcher. A physical column named `Nominal` can therefore match
  `Nom OD (mm)` when that business label is present in the Access description;
- Transform data keeps every source column by default. It now allows a user to
  deliberately remove a column or rename it before the costing-field matches,
  persists those operations as ordered query steps, validates duplicates and
  missing rename sources, and reuses the same operations for manual or 30-second
  refresh;
- the preview header shows the effective name, physical source name when it
  differs, caption/description metadata, and provider type;
- Live Data exposes each complete retained object with its full saved row and
  column counts plus a bounded inspection grid, separately from the existing
  costing-ready views;
- Copper's previous ambiguous **Nominal** heading was the nominal-area field,
  not OD. It is now labelled **Nominal area**, with a separate **Nominal OD**
  column. The current local cache was inspected without exposing database
  contents: its saved OD mapping was already `Nom OD (mm)`, while 2 of 12 cached
  Copper rows had blank/zero OD values;
- legacy state files with no full retained objects remain readable. Because the
  earlier importer did not save discarded columns, each already-linked area
  must be imported once through the new transform editor to create its first
  full retained object. Subsequent manual/automatic refreshes preserve it.

Verification:

- the x64 Debug solution builds with zero warnings and zero errors;
- 45 Domain tests pass;
- 45 Application/Infrastructure/Reporting tests pass, including Access
  description matching for `Nominal` -> `Nom OD (mm)`, rename/remove transforms,
  full unprojected-column retention, atomic per-area import, JSON round-trip,
  legacy-state compatibility, and saved-query refresh;
- 2 workbook-parity evidence tests pass and 2 golden parity cases remain
  intentionally skipped pending business approval;
- the app was not launched during this follow-up, so the documented unsafe
  generic Computer Use launch bridge was not used.

## Resizable central-data workflow and saved-transform editing — completed 8 August 2026

The first real full-table import exposed a WinUI presentation fault rather than
an import fault: Navigator and Transform data were hosted in fixed
`ContentDialog` widths. On a 1920 x 1080 screen the Transform data window showed
only its query list and a narrow table preview; the complete source-column
rename/remove and ATAG match panel existed but was clipped beyond the visible
dialog, making the page appear non-interactive.

Implemented:

- every central-data setup page, Navigator, Transform data, and completion/error
  message now uses the shared `CentralDataWorkflowWindow`, a normal native
  WinUI window with a title bar, move, resize, minimise, maximise, and close;
- setup and short result pages receive a generous compact default, while
  Navigator and Transform data default to a near-work-area 1600 x 900 maximum
  workspace and remain responsive when the user resizes them;
- Navigator no longer derives a fixed width from the underlying MainPage. Its
  searchable source-object list and horizontally/vertically scrollable table
  preview stretch with the window;
- Transform data no longer uses a fixed 1240 x 720 content rectangle. It keeps
  the Query column, a flexible full-width preview, and a wider scrollable Query
  Settings column visible together. The settings explicitly state that the user
  can rename/remove source columns while database row values remain
  source-controlled;
- **Edit existing link** is available beside **Set up data link** after at least
  one database link exists. One saved link opens directly; multiple saved links
  receive a clear area/table/source picker. The saved object is previewed and
  its previous remove/rename steps and ATAG mappings are loaded into Transform
  data;
- Access and Windows-authenticated SQL links reuse their saved connection.
  SQL-password links deliberately ask for the session-only password again;
- saving an edited transform still re-reads the complete object and calls the
  existing atomic import boundary. Cancel, provider failure, blocking preview,
  mapping failure, or import failure leaves the previous full retained table
  and typed costing projection unchanged;
- the reference workbook was not opened or edited, no drive letter was added to
  maintained code, and no application was launched during this slice.

Verification:

- x64 Debug solution build: zero warnings, zero errors;
- Domain: 45 passed;
- Application/Infrastructure/Reporting: 45 passed;
- workbook parity: 2 evidence tests passed, 2 golden fixtures intentionally
  skipped pending business approval;
- an initial sandbox restore attempt reproduced the known `NU1301` Windows
  package-credential failure. A normal Windows-credential restore succeeded,
  after which the no-restore compile and all test suites were clean.

## Multi-monitor placement and unambiguous linked-table views — completed 8 August 2026

The first user-completed Copper import proved that the data was retained, but
the Live Data page made the complete transformed table and its typed costing
projection look like two competing copies. The setup chooser also looked as if
Copper were the only remaining option, tab close buttons suggested that viewing
a table could unlink it, and workflow windows did not consistently follow the
main app to another monitor.

Implemented:

- the ATAG main window persists its last restored position, size, monitor, and
  maximised state to the per-user
  `%LOCALAPPDATA%/ATAG Design Ltd/ATAG Costing/window-placement.json`; saved
  bounds are clamped to the matching work area and fall back safely when a
  monitor is no longer connected;
- every setup, Navigator, Transform data, confirmation, and result window is
  centred on the display containing the ATAG main window rather than defaulting
  to the primary display;
- the first setup page is now an always-visible five-item list for Copper,
  Compounds, Masterbatch, Contacts, and Operators. Each item explains its data
  and reports either its saved table/source or retained-only state, so a Copper
  import never hides the remaining areas;
- Live Data now states that the complete transformed linked table is the source
  of truth and that the lower table is the validated, unit-normalised costing
  view generated from the same atomic import. It is not another connection or
  independently maintained database copy;
- the direct transformed-table preview is collapsed by default but remains easy
  to inspect, with complete saved row/column counts and a linked/cached label;
- the lower Copper costing view no longer displays the redundant blank
  **Nominal area** column. It retains **Nominal OD**, calculated metallic area,
  AWG, class, yield, and price used by the costing workflow;
- direct-table and costing-view tabs are not closable. The only unlink route is
  the explicit **Remove link** button, followed by a confirmation naming the
  area/table;
- removing a link deletes only its refresh definition. The complete transformed
  table and validated five-table costing snapshot remain available offline;
  the JSON store performs this change atomically;
- an Application test now proves that removing a Copper link leaves its retained
  transformed rows and validated snapshot unchanged;
- the reference workbook was not opened or edited, no maintained path contains
  a fixed drive letter, and the app was not launched through the unsafe generic
  Computer Use bridge.

Verification:

- x64 Debug solution build: zero warnings, zero errors;
- Domain: 45 passed;
- Application/Infrastructure/Reporting: 46 passed;
- workbook parity: 2 evidence tests passed, 2 golden fixtures intentionally
  skipped pending business approval;
- the exact built `ATAG.Costing.WinUI.exe` was launched, closed through its
  recorded ATAG window handle, and launched again. Both runs reached an
  activated MainWindow with no new startup exception, the second run consumed
  the saved monitor placement, and both runs exited cleanly. No Hudl process or
  generic application launcher was involved;
- that live relaunch exposed a valid secondary-display work area only 573 px
  high. Placement validation now accepts practical small work areas (minimum
  480 x 320) and still clamps the restored bounds to the selected display,
  rather than silently discarding a valid saved screen because it is below a
  conventional 800 x 600 threshold;
- the sandbox initially regenerated restore assets against an inaccessible
  NuGet endpoint. Restoring from the existing current-user package cache with a
  path supplied only to the command repaired the assets; the no-restore build
  and all test suites then completed cleanly. No cache path was written into
  maintained source or documentation.

## Owned data windows and construction-aware strand packing — completed 8 August 2026

This follow-up completed the remaining Live Data window/status requirements and
replaced the circular-ring conductor preview with source-construction-aware
close packing.

Implemented central-data presentation:

- `CentralDataWorkflowWindow` extends the app content into a WinUI title bar,
  uses the ATAG window as its native owner, and therefore remains above ATAG
  while the user works in it without becoming system-wide always-on-top;
- connection actions appear above the complete transformed-table and validated
  costing-view previews;
- **Remove link** always begins with an explicit linked-area picker, even when
  only Copper is currently linked;
- the navigation footer lists Copper, Compounds, Masterbatch, Contacts, and
  Operators separately and reports the exact `N of 5 LIVE` state;
- refresh now returns structured per-area results, preserving the previous
  cached rows and clear offline status for any failed area.

Implemented conductor parsing and preview:

- `conductor-construction/v2` retains a packing-level hierarchy for simple,
  rope, and deeper numeric source descriptions, including `4 x 0.1`,
  `7x19/0.32`, `183 x 7 0.10`, `130 x 7 x 7`, and
  `104 x 3 x 7 x 7` forms;
- a numeric hierarchy which omits its strand diameter may use a positive
  retained nominal area to infer the diameter for preview/area checking. The UI
  labels the inference and does not rewrite the linked source row;
- `ConductorPreviewLayoutBuilder` selects a connected triangular lattice.
  Complete layers form a compact hexagon; incomplete counts such as 16 form a
  compact approximately hexagonal cluster rather than separated circular
  rings;
- rope sub-bundles are recursively close-packed with only a small clearance,
  so their hierarchy stays legible without large artificial voids;
- detailed cross-section and compressed angled end face contain every parsed
  strand in one geometry each. The side length shows only physically exposed
  strands or top-level rope bundles, with continuous helices and rope texture;
- strand, group, side-surface, and end-face outline widths scale with the
  rendered strand diameter. High-count conductors therefore remain dense
  instead of becoming oversized outlined noodles;
- supplier-defined text with no reliable numeric stranding remains in the
  simplified conductor-envelope view. The app does not invent missing strand
  counts;
- the preview renderer is still off by default, so the most expensive detailed
  geometry is created only when the user enables the preview and strand detail.

Verification:

- x64 Debug solution build: zero warnings, zero errors;
- Domain: 50 passed;
- Application/Infrastructure/Reporting: 50 passed;
- workbook parity: 2 evidence tests passed, 2 golden fixtures intentionally
  skipped pending business approval;
- the Copper snapshot audit parses every numeric construction (at least 315
  rows) and proves that every expected strand is present, touches a neighbour,
  does not overlap another strand, and remains inside its conductor envelope;
- a generated representative audit was inspected for 4, 7, 16, 19, and 32
  strand constructions plus a 7-by-19 rope. The 16-end example is visibly a
  compact hexagonal cluster and the rope keeps seven distinct 19-strand groups;
- the exact project-built ATAG executable was launched directly and reached an
  activated MainWindow. No Hudl process was started and the generic application
  launcher was not used;
- the official screen-control bridge was retried after restoring its full
  Windows environment and restarting its helper transport. Both window
  enumeration and direct rehydration of the verified ATAG HWND still fail at
  the bridge with `EnumWindows ... 0x80070003`. Do not claim an automated live
  click-through from this run. Continue to avoid generic `launch_app`; use the
  project-scoped executable/shortcut and visual review until the bridge is
  repaired by the host;
- the reference workbooks were not opened or edited, and no maintained source,
  configuration, or documentation path hard-codes the current workspace drive.

## Imported-copper eligibility, operator default, and opening-face preview — completed 9 August 2026

This corrective follow-up keeps imperfect but identifiable live database rows
visible without weakening the calculation boundary, and brings the 16-end and
side-profile visuals closer to the supplied physical references.

Implemented conductor selection and validation:

- imported Copper records are selector-eligible when they retain a description,
  supplier, positive yield, and either a nominal OD or parsed numeric
  construction; they are no longer silently removed merely because their stored
  supplier price or nominal OD is blank;
- `32/0.20 TCW (H)` is therefore present in the strand-construction path after a
  successful import. A row with no locked nominal OD remains deliberately
  blocked from producing a finished costing and now displays a specific mapping
  warning instead of disappearing;
- a missing database price does not hide the record because the COR workflow
  derives price per kilogram from the user's supplier quote total and quoted
  mass;
- locked imported dimensions and yields are still not made editable on the
  costing page. The later auditable derivation slice may fill a mathematically
  exact gap or show an explicitly labelled geometry estimate; it never silently
  rewrites the retained source cell.

Implemented operator and preview corrections:

- the field is labelled **Costing Prepared by**; when a saved operator is not
  present, the Office list selects the database operator whose first name is
  Laura, falling back to the first available operator rather than a hard-coded
  record ID;
- the 16-end detailed layout is now the confirmed five-strand centre plus
  eleven-strand outer layer, with overlap and envelope checks;
- the side conductor begins at the insulation cut plane. In detailed mode every
  strand receives a matching circular-prism opening cap; in simplified mode the
  conductor receives one centred opening face;
- the insulation annulus and its rims are composited above the conductor edges,
  while the hole and conductor caps remain centred and visible. Copper no longer
  appears to emerge from the outside edge of the insulation.

Verification:

- x64 Debug solution build: zero warnings, zero errors;
- Domain: 50 passed;
- Application/Infrastructure/Reporting: 50 passed;
- workbook parity: 2 evidence tests passed, 2 golden fixtures intentionally
  skipped pending business approval;
- 10 focused copper-selection and conductor-layout tests passed;
- the exact project-built ATAG executable was launched directly from its build
  directory and reached a current activated MainWindow without launching Hudl;
- the Computer Use bridge was retried against the running ATAG process but still
  fails during Windows enumeration with `EnumWindows ... 0x80070003`. No generic
  application launcher was used and no automated visual acceptance is claimed;
- the reference workbooks were not opened or edited, and no drive letter was
  added to maintained application code or launch configuration.

## Auditable missing-field derivation — completed 9 August 2026

This follow-up allows a partially maintained Copper database row to remain useful
when other cells in that same retained row support a defensible result. It does
not alter the direct linked table: derivation occurs only in the typed costing
projection and carries its formula, substituted source values, confidence, and
rule version.

Implemented exact calculations:

- missing price per kilogram may be calculated as **manufacturing cost + copper
  cost**; the copper-including-premium column is used only as the fallback copper
  component when the dedicated copper-cost column is absent;
- missing yield may be calculated as **reel conductor length ÷ reel net weight**;
- missing metallic area may be calculated from parsed strand count and diameter
  as **strand count × π × strand diameter² ÷ 4**;
- missing conductor OD may be calculated from a positive source volume per metre
  as **√(4 × volume per metre ÷ (1,000 × π))**.

Implemented bounded estimate and presentation:

- when both stored OD and source volume are absent but a numeric strand
  construction is available, the app may use the same close-packed physical
  envelope used by the live preview as an OD **estimate**;
- exact results are suffixed **(calculated)** and geometry fallbacks are suffixed
  **(estimated)**. An estimated field opens the existing conductor verification
  notice, and the notice includes the formula/source summary;
- `32/0.196 TCW (H)` demonstrates the boundary in the currently retained Access
  data: 30,000 m ÷ 344 kg supplies an exact yield, while its zero stored OD and
  zero stored volume permit only a visibly labelled packed-envelope estimate;
- already-retained full Copper tables are re-projected at app load, including
  while the link is offline or has been removed. A live database refresh is not
  required to benefit from the rules;
- primary Copper price, yield, and OD matches no longer block import of the whole
  source table. Rows still require their identifying description and supplier,
  and a value is filled only when one of the versioned rules applies;
- records that receive no new value retain their existing object identity. The
  full transformed table and every original blank, zero, error, and supplied
  value remain unchanged for audit and later editing;
- other central-data areas remain source-led until a similarly defensible,
  documented relationship exists; the app does not invent compound density,
  colour compatibility, contact, or operator values.

Verification:

- x64 Debug solution build: zero warnings, zero errors;
- Domain: 50 passed;
- Application/Infrastructure/Reporting: 54 passed;
- workbook parity: 2 evidence tests passed, 2 golden fixtures intentionally
  skipped pending business approval;
- regression coverage confirms exact price/yield/area/volume-to-OD calculation,
  labelled packed-OD estimation, offline retained-table re-projection, source
  cell preservation, and unchanged-object preservation when nothing is derived;
- the reference workbooks and source databases were not opened or edited, and no
  drive letter was added to maintained application code or launch configuration.

## Recommended next development slice

Proceed with **V1.3b: versioned dual-insulation document payload and first
guided UI**.

V1.3a and its construction-planning follow-up are green. Keep V1.3b bounded:

1. add a schema-v3 construction discriminator and dual-insulation input/result
   payload without weakening schema-v1/v2 single-core readers or immutable
   approved revisions;
2. retain both production scopes explicitly: core/first-layer finished plus
   start-up, and second-layer finished-only;
3. add Application orchestration that calls
   `DualInsulationCostingCalculator`; do not reimplement formulas in the view
   model;
4. turn the existing dual construction planner into a complete guided costing
   workspace using the established searchable Copper, Compounds, and
   Masterbatch references for both layers;
5. reuse the grouped dependency-stage presentation and live result controls,
   showing separate first-layer and second-layer material flows;
6. feed saved dual results into the existing revision/index lifecycle while
   leaving quotation and contract-review output clearly staged until their
   dual wording is specified;
7. add schema round-trip, legacy-read, immutable-save, Application, and focused
   UI-state tests before any deliberate app launch;
8. persist the ordered optional-module selections without implementing their
   individual material formulas until each module receives its own audited
   slice;
9. keep both dual and single-core golden workbook cases approval-gated.

## Calculation trace requirement

Every migrated result should be able to display:

```text
Name
Business meaning
Source inputs
Input values and units
Formula/expression
Expression with substituted values
Unrounded result
Rounding rule
Displayed result and unit
Dependency steps
Warnings or validation findings
Rule/version identifier
```

This trace is shared by the interactive costing UI and optional printed
calculation appendix.

## Decisions not yet made

Do not silently decide these without documenting the trade-off or asking the
user when the choice becomes material:

- final persistent business-data format, likely SQLite versus versioned JSON;
- authoritative material-price update workflow;
- user/approval and multi-user requirements;
- final quote and contract-review layouts;
- installer technology and signing;
- which workbook costing family should follow the single-core V1;
- retention and backup rules;
- whether Excel remains an export target after migration.

Reasonable internal interfaces can be prepared without locking in these choices.

## Guardrails

- Do not recreate the solution on the second PC.
- Do not hard-code the current USB drive letter.
- Do not edit the reference workbook unless explicitly asked.
- Do not put formulas in XAML code-behind, view models, or report templates.
- Do not duplicate a calculation for a second report or page.
- Preserve the five production-project dependency direction; test projects may
  reference Domain but production projects must not reference tests.
- Preserve existing user files and unrelated changes.
- Build and test after each bounded calculation family.
- Clearly distinguish implemented functions from navigation placeholders.
- Update this handoff document and `docs/SCOPE.md` after each major milestone so
  another PC can continue without relying on chat history.

## Repository state

At handoff time, the workspace was not initialized as a Git repository. The USB
folder itself therefore carries the current source of truth. Do not assume branch
history or attempt a destructive Git recovery.

Initializing version control is recommended before substantial formula migration,
but it is a separate user decision.

## Completion state for this handoff

The foundation, single-core V1.2, immutable-revision V1.2b, and pure
dual-insulation V1.3a slices have been completed on this PC:

- the existing solution and workbook were located through relative paths;
- .NET SDK 10.0.302 was verified;
- the portable launcher still resolves its root with `%~dp0`;
- the complete eight-project x64 Debug solution builds with zero warnings and
  zero errors;
- 50 Domain tests, 54 Application/Infrastructure/Reporting tests, and 2
  parity-evidence tests pass;
- 2 golden parity tests are intentionally skipped pending business approval;
- the original parity baseline remains
  `6A9DBE53DF2A403BDB92A23FDC2C4AD55702B6ADF089ED02FA327F3E504851D3`;
- the current workbook was observed read-only on 29 July 2026 with SHA-256
  `823FCE28815A9420E87A9FA119790243C8A4E9961B26B976A26EBE79BE9FA0ED`,
  size 1,322,759 bytes, and modified time 28 July 2026 23:36:28. Treat this as
  workbook drift and do not promote it to the approved parity baseline without
  review;
- a later read-only observation of the user-specified relative file
  `..\..\GB - 16-2-2-C.xlsm` on this same date resolved to a different file
  identity: SHA-256
  `E771EF7847C3C1B3CEF38488B74BFB98E33F2EBBBE850F3D6C52783FD8405250`,
  size 318,457 bytes, modified 4 January 2026 15:04:32 UTC. The app did not
  write either file. Preserve both observations as drift evidence; do not
  replace an approval-gated fixture merely because the removable/workspace
  volume now exposes the second identity;
- this PC initially lacked two locked NuGet packages in its local cache; a
  platform-specific restore completed, after which the normal no-restore build
  and test commands were clean;
- schema-v2 revision lifecycle, legacy upgrade, approved overwrite rejection,
  relative index paths, selected-root containment, unavailable-root refusal,
  duplicate/new-revision identity, and recursive trace round-trip are covered by
  automated tests;
- the database import flow uses official `System.Data.OleDb` and
  `Microsoft.Data.SqlClient` providers, a searchable WinUI Navigator with table
  preview, a Power Query-inspired transform editor, Access column metadata,
  saved remove/rename steps, automatic field projection, full transformed-table
  retention, atomic typed/full-table commit, and the existing manual/30-second
  refresh path;
- the central-data setup, Navigator, transform, and result pages now use movable
  and resizable native WinUI windows instead of fixed `ContentDialog` widths.
  Data-heavy pages default to a near-work-area workspace, and **Edit existing
  link** returns a configured Access/SQL object to its saved transform and
  field-match editor without weakening last-successful retention;
- the main app now restores its previous monitor, restored bounds, and maximised
  state, and every central-data workflow window opens on the main app's monitor;
- Live Data distinguishes the collapsed complete transformed source table from
  its validated costing-ready view. All five area choices remain visible after
  any import, tabs cannot unlink data, Copper's redundant blank nominal-area
  display is removed, and confirmed **Remove link** stops refresh while keeping
  the full retained table and typed snapshot;
- synthetic provider/table tests prove that division-by-zero cells become
  visible non-blocking blanks, valid rows continue, automatic Access-style
  compound headers and column descriptions match, rename/remove steps affect
  the projection without discarding unrelated kept columns, only the selected
  retained area changes, the full table round-trips through JSON, and a saved
  query refreshes through the same import path; authoritative business-schema
  acceptance remains required;
- the native app was launched from the exact x64 Debug build. The storage setup,
  home page, and costing workspace were inspected. A first visual pass found the
  new action row squeezed the status label; moving the actions to their own row
  fixed the clipping, and the corrected live window shows the full
  **Working copy · revision 1** and **Unsaved changes** labels;
- the latest reel/wording A4 quotation sample was regenerated, confirmed as one
  595 x 842 point A4 page, rendered at 144 DPI, and inspected for clipping,
  column overlap, currency placement, and footer placement;
- dual-insulation V1.3a separates the 10,200 m core/first-layer production
  length from the 10,000 m second-layer length, applies the confirmed 3%
  allowance once per stream, adds each price once, and retains a recursive
  trace;
- the current-workbook dual evidence is `PendingBusinessApproval`; OQ-006 is
  resolved in favour of the confirmed mass-based rule, while approval of the
  complete mapped case remains gated;
- independent line-specific speed profiles and two-process dual labour
  orchestration are implemented without creating masterbatch process time;
- Home construction tiles and the first ordered dual optional-module planner
  are implemented; Flat and D-shape remain planned for up to ten in-line cores;
- construction tiles use consistently stretched cable-specific vector
  silhouettes, with the D-shape cores raised above its flat base;
- the command strip no longer repeats construction choice, and COR has an
  off-by-default responsive preview dock. It is a resizable right-hand rail at
  wide sizes and a bottom dock at compact sizes, with a simplified envelope or
  connected close-packed all-strand cross-section. Complete shells are
  hexagonal and partial counts such as 16 remain compact instead of being
  spread over circular rings. Recursive rope hierarchy is retained. Its
  straight approximately 20-degree side profile uses one continuous gently
  twisting surface per physically exposed strand or top-level rope bundle,
  fine internal rope-strand cues, and an all-strand final-rotation end face.
  Outline widths scale with strand size. Its conductor passes in front of a hollow annular insulation
  opening in both simplified and detailed modes. Its closed insulation end shows only
  the exposed outside rim and shares the body's vertical gradient. A labelled
  radial-wall comparator and a separate scrollable scaled print-repeat preview
  are included; the print preview is completely absent while print is disabled;
- the detailed preview native crash is repaired by keeping every WinUI
  `Geometry` instance owned by one `Path`; live compact, detailed, maximised,
  and divider-resize checks passed against the exact ATAG Costing x64 Debug
  executable;
- the BAC 7x19 customer specification supports #28 AWG for each 0.321 mm
  strand and #7 AWG for the 133-strand conductor. Its isolated 24 AWG arrow
  conflicts with the rest of the drawing and is recorded as a do-not-import
  source conflict;
- the Hudl isolation audit found no Hudl reference or launch route in
  maintained ATAG source, tools, docs, startup log, or Windows Application
  events. The portable root shortcut was regenerated to remove a stale fixed
  `D:` icon path; exact project-scoped ATAG launches left Hudl Device Console
  stopped. The generic Computer Use `launch_app` bridge later opened both
  indexed applications and is now a documented do-not-use verification route.
  It reproduced again on 1 August when the bridge was accidentally used with an
  explicit ATAG path; Hudl was immediately closed. The safe project-scoped
  process launch opened ATAG alone, while the automation index incorrectly
  attributed that ATAG-titled window to the Hudl executable and therefore could
  not be used for a trustworthy screenshot pass;
- the storage setup card is content-height and centred within its full-screen
  overlay rather than stretching vertically;
- `docs/CABLE-CONSTRUCTION-AND-VISUALISATION.md` preserves the opt-in scaled
  cross-section/side-profile and future module requirements;
- `docs/PROJECT-REVISIONS.md` records the portable folder layout, schema fields,
  lifecycle, offline behavior, legacy compatibility, and key source files;
- the user should still visually review and exercise Save, Approve, Open, edit
  approved, and Duplicate against their chosen real save folder before treating
  the workflow as business-accepted;
- maintained code contains no fixed drive-letter path.

## 2026-08-09 clean-install data boundary and first-run setup slice

This slice was completed after reading both this project handoff and the root
Git/update handoff. It deliberately did not recreate the solution, edit the
workbook, choose an installer technology, or hard-code the current USB drive.

Completed:

- removed the workbook-derived five-table JSON snapshot from production source
  and removed its embedded-resource registration;
- clean production state now contains zero Copper, Compound, Masterbatch,
  Contact, and Operator rows. Existing `%LOCALAPPDATA%` retained state is
  normalised and preserved rather than replaced;
- the central-data store now accepts the first validated table on an empty
  install, then retains each subsequent area independently and atomically;
- added a two-part first-run screen for the user-selected save folder and the
  five LIVE database areas. It shows the exact completed count and missing
  areas, opens the existing Access/SQL Navigator for each link, and prevents
  first-run completion until all five areas have a saved link plus validated
  retained table;
- added a persisted `HasCompletedFirstRunSetup` preference. Existing JSON
  preferences remain compatible because the new field defaults to `false`;
- retained/offline data remains user-local at
  `%LOCALAPPDATA%/ATAG Design Ltd/ATAG Costing/central-data-state.json`; no
  source-controlled fallback rows are invented for incomplete or legacy state;
- expanded `.gitignore` to exclude retained central data, generated snapshots,
  Access/SQL database files and backups, saved `.atagcosting` documents,
  LocalAppData-style settings, secrets files, logs, Python cache, and all nested
  build/artifact output;
- initialised a new local `main` Git repository in the project folder and set
  `origin` to the user-supplied private repository
  `https://github.com/Kiddabob/ATAG-Costing-App.git`. The existing machine-local
  GitHub credential was used without copying it into the project. The remote was
  verified empty and root commit `8d3ab9a` (`Import-existing-ATAG-Costing-App`)
  was pushed to private `origin/main` on 9 August 2026;
- confirmed the current app stack is .NET 10, WinUI 3, Windows App SDK 1.8,
  with an unpackaged/self-contained development build. No production installer,
  release feed, updater, or version-channel mechanism exists yet; the root
  handoff explicitly requires those decisions not to be guessed;
- the wide-screen LIVE Preview divider now uses a native west/east resize
  pointer and a thicker accent-coloured hover/drag affordance;
- moved radial-wall guidance out of the optional preview rail and into the main
  insulation-to-masterbatch costing flow, so calculated wall, comparator,
  assessment, source, and engineering disclaimer remain visible while LIVE
  Preview is off;
- replaced former production-data-dependent tests with fictional test-only
  rows and representative conductor constructions;
- updated `README.md`, `docs/CENTRAL-DATA.md`, and `docs/SCOPE.md` to state the
  clean-install/no-business-data boundary.

Verification on 9 August 2026:

- baseline before this slice:
  `dotnet build ATAG.Costing.sln -c Debug -p:Platform=x64 --no-restore` — passed,
  0 warnings and 0 errors;
- final full build with the same command — passed, 0 warnings and 0 errors;
- `dotnet test ATAG.Costing.sln -c Debug -p:Platform=x64 --no-build --no-restore`
  — 109 passed, 2 intentionally skipped approval-gated workbook parity cases,
  0 failed after the mapping/filter follow-up;
- `git check-ignore --no-index` confirmed `central-data-state.json`,
  `central-data-snapshot.json`, `.accdb`, and `.atagcosting` examples are all
  excluded;
- versionable maintained source contains no fixed `J:`, `C:`, or `D:` runtime
  path. Historical handoff prose still names old drive-path defects as evidence.

Before a production installer or updater is implemented, follow the root
handoff: decide installer/package technology, code signing, private update
authentication, recipients/channels, and release hosting. Do not embed a GitHub
PAT or silently choose an insecure update route.

Continue with **V1.3b: versioned dual-insulation document payload and complete
guided editor** as specified above. Reuse the completed material, production,
and ordered-construction domain rules; keep both length scopes and extrusion
profiles visible; keep formulas out of WinUI and reporting; and keep every
golden parity fixture approval-gated unless an ATAG business reviewer explicitly
approves the complete case.

## 2026-08-09 masterbatch mapping, saved filters, and preview-control refinement

This follow-up was completed after rereading this project handoff and the root
Git/update handoff. It preserves the existing solution, workbook, portable path
rules, retained-data boundary, and first-run database-link requirement.

Completed:

- replaced the two ambiguous Masterbatch **Compatibility** and **Temperature
  limits** mappings with sixteen first-class mappings: **Use** and **Max Temp**
  for PVC, PE/PP/PUR, PS, ABS, ACETAL, PBT, Nylon, and PC/PES;
- the full transformed Masterbatch table remains the retained source of truth.
  Its typed costing projection now builds each compatibility cell from that
  family's own source columns. Explicitly incompatible materials show no
  misleading temperature, while an unmapped family remains visibly unrecorded;
- retained compatibility with saved pre-change links by reading their legacy
  aggregate mappings only as a fallback until the user next edits and imports
  that link;
- added reusable Transform Data row filters with equals, not-equals, contains,
  does-not-contain, starts-with, ends-with, blank, and not-blank conditions.
  Filters are stored in the link definition and are reapplied during manual and
  scheduled refreshes; `Office = True` is now a supported Operators filter;
- repaired the empty **Costing Prepared by** list. Imported Operators are now
  eligible from their mapped `Office` value without requiring the absent
  workbook-only `Employee` field. Laura remains the preferred default whenever
  she is present in the eligible office list;
- moved **Radial Wall Guidance** into the fixed preview rail immediately above
  the LIVE Preview switch. The guidance remains visible when the preview itself
  is disabled;
- replaced the moving Material-links expander with an upward-opening flyout, so
  the footer header remains anchored while all five independent link states and
  the refresh action are shown;
- refined detailed side-profile conductors so the inner/core strands are drawn
  behind the outer helical layer instead of disappearing. Rounded strand paths,
  highlights, and increased lay visibility make the longitudinal construction
  less like flat ribbons while preserving the exact selected strand/group count.

Verification on 9 August 2026:

- `dotnet build ATAG.Costing.sln -c Debug -p:Platform=x64 --no-restore` passed
  with 0 warnings and 0 errors;
- the full test suite passed 109 tests, with 2 intentionally skipped
  approval-gated workbook-parity cases and 0 failures;
- the exact ATAG Costing executable started successfully as
  `ATAG.Costing.WinUI.exe`; no Hudl project launcher or process was invoked;
- the Windows screen-control bridge still failed during window enumeration with
  `0x80070003`, so no visual click-through acceptance is claimed. The automated
  and process-level checks are complete, but the changed Masterbatch and
  Operators links should be reimported and visually reviewed on the next normal
  app run;
- no workbook was edited, no central database or retained cache was added to
  source, and no USB drive letter was hard-coded.

Recommended next slice after that visual review remains **V1.3b: versioned
dual-insulation document payload and complete guided editor**. Carry the saved
filter pipeline and per-family Masterbatch limits into both insulation layers;
do not restore a second interpreted database or duplicate a waste/start-up
allowance.

## 2026-08-09 installer and update identity decisions

The user has now confirmed:

- installed Costing App clients must receive updates without requiring a
  GitHub login;
- each intended user signs into Windows/OneDrive with an `atagcables.com`
  Microsoft work account;
- the existing `Kiddabob/ATAG-Costing-App` repository may now become public,
  provided the public source/build contains no retained database links, cached
  central-data rows, business defaults, private credentials, or generated
  customer/supplier/operator/material data;
- no reusable GitHub credential may be embedded in the application.

An `atagcables.com` OneDrive/Microsoft account is an identity, not a code-signing
certificate. It may be used through Microsoft Entra to authorise Azure Artifact
Signing or an organisation-controlled update service, but signing still requires
an Azure subscription, organisation identity validation, a certificate profile,
and assigned signer permissions. A self-signed certificate would additionally
need to be deployed into every authorised PC's trusted certificate stores; a
OneDrive sign-in alone does not establish that trust.

The selected update direction is now a public GitHub repository/release feed so
installed clients can read release metadata and download assets anonymously.
Developer pushes still use each developer PC's own Git credential; that is
separate from the installed app and must never be copied into a release.

The recommended Windows packaging route is MSIX plus an `.appinstaller` feed,
because Windows supports scheduled update checks and repair from HTTPS or a
shared location. Signing remains a separate decision: an `atagcables.com`
OneDrive sign-in is not itself a signing certificate. The installer/updater is
still not implemented, and no release asset should be described as an updater
until installed-app upgrade behaviour is tested.

## 2026-08-09 isolated public-review build

The user requested a quick build that exposes the app interface without any
database link or retained ATAG data so they can decide whether a public
binary-only distribution is acceptable.

Implemented as a compile-time `AtagPublicReview=true` build, separate from the
normal application:

- `PublicReviewCentralDataStore` returns only
  `InitialCentralDataState.Create()`: zero Copper, Compound, Masterbatch,
  Contact, or Operator rows;
- the review page constructs no Access or SQL database navigator and does not
  construct the ECB exchange-rate service;
- it uses a no-op in-memory preferences service, bypasses first-run storage and
  database setup, never starts the 30-second refresh timer, and does not read
  the installed app's retained LocalAppData;
- central-data setup/edit/remove/refresh controls, project open/save/revision
  controls, and storage settings are hidden or disabled;
- every bound `TextBox`, `AutoSuggestBox`, `ComboBox`, `DatePicker`, and
  `NumberBox` is detached from its operational value and presented blank in the
  review shell after the selected page is rendered. Quote length, allowances,
  commercial rates, supplier quotes, OD/tolerances, labour, print, reel,
  quotation-number, packaging, delivery, and terms defaults do not appear in
  the public binary review; installed user-entered state belongs only in that
  user's LocalAppData;
- a persistent banner states that this is an interface-only review build and
  empty selectors are expected;
- visible and package fallback naming is `Costing App`. A local-only,
  current-user OneDrive registry check changes visible runtime naming to
  `ATAG Costing App` only when a Business account email ends in
  `@atagcables.com`; the address is not retained, logged, displayed, or sent;
- its executable identity is `Costing.App.PublicReview.exe`, and its harmless
  window placement uses a separate `Costing App\Public Review` LocalAppData
  subfolder;
- the A4 quotation button remains usable with no linked data. It produces a
  neutral one-page A4 draft headed `Costing App`, substitutes
  `Not specified`/zero values where required, and contains no ATAG company
  name, address, phone number, quote prefix, or private data;
- the normal application build and user data remain unchanged.

Build and launch:

```text
dotnet publish src\ATAG.Costing.WinUI\ATAG.Costing.WinUI.csproj -c Release -p:Platform=x64 -p:AtagPublicReview=true -p:PublishTrimmed=false -p:PublishReadyToRun=false -r win-x64 --self-contained true --no-restore -o output\Costing-App-Public-Review
Open Costing App Public Review.cmd
```

Verification completed on 9 August 2026:

- public-review Debug build: zero warnings and zero errors;
- normal Debug build: zero warnings and zero errors;
- tests: 109 passed, 0 failed, with 2 workbook fixtures intentionally skipped;
- release publish completed at `output\Costing-App-Public-Review`;
- audit found no `.accdb`, `.mdb`, `.db`, `.sqlite`, `.atagcosting`, workbook,
  CSV, PDB, central-data-state file, known ATAG database/workbook name, or `J:`
  path in the published folder;
- `Directory.Build.props` disables debug metadata for every referenced ATAG
  project during `AtagPublicReview=true` builds. This removes the local build
  path recorded in referenced DLL CodeView headers, rather than merely deleting
  the separate PDB files;
- the exact executable launched as `Costing.App.PublicReview.exe`; its startup
  log recorded runtime name `Costing App`, a real activated HWND, and two
  after-render input audits with zero populated number, text, selection, or
  date values. The process
  check found no Hudl or Python console process;
- the quotation generator test produced a 595 x 842 point, one-page PDF. Its
  rendered PNG was visually inspected with no clipping or overlap, and both
  the PDF bytes and test assertions reject ATAG identity/address/phone text;
- the Computer Use bridge still failed with `0x80070003`, so do not claim
  automated visual acceptance. The app was left open for the user's manual
  review.

This is a review artifact, not the production installer or updater. Audited
commit `acf2b22` (`Prepare-neutral-public-review-build`) was published to
`origin/main` on 9 August 2026, the existing repository was changed to public,
and a separate anonymous GitHub API request verified public access. The first
installer, versioned release asset, anonymous in-app update check, integrity
validation, and upgrade-in-place/rollback test must each be recorded separately
when actually complete.

## 2026-08-09 one-file installer and cumulative in-app updater

The release-management implementation now uses Velopack 1.2.0 around the
existing unpackaged/self-contained WinUI application. This supersedes the
earlier tentative MSIX recommendation: it preserves the current file/database
workflows, installs per user without administrator rights, and produces the
single user-facing `Costing-App-Setup.exe` requested by the user. The unsigned
installer can still show Unknown publisher/SmartScreen until ATAG provisions a
trusted organisation signing certificate.

Implemented:

- authoritative `CostingAppVersion` in `Directory.Build.props`, currently
  `0.1.0`, drives binaries, Velopack packages, and GitHub tags;
- Velopack startup hooks run before COM and WinUI initialisation;
- Settings shows installed version, Stable/Beta choice, automatic-check toggle,
  status, package size/checksum, progress, explicit save/restart confirmation,
  and a safe Later action;
- update checks start only after the main window is visible, use the public
  `Kiddabob/ATAG-Costing-App` GitHub Releases feed without a token, and never
  prevent normal app launch after a network/feed failure;
- update notes are cumulative: the app anonymously reads the public release
  history, filters versions strictly newer than the installed one and no newer
  than the target, respects Stable/Beta, and shows every intervening version.
  If that supplementary history lookup fails, the target package's own notes
  remain available and the update itself can continue;
- the release builder extracts only the current version's CHANGELOG section for
  each package/release. This prevents future cumulative histories from repeating
  the complete changelog inside every individual GitHub release;
- `tools/Build-Release.ps1` tests, self-contained-publishes, audits, packages,
  and checksums the x64 release; `.github/workflows/release.yml` repeats the same
  path and publishes all installer/update-feed assets;
- the safety audit blocks Access/SQL backup/workbook/saved-costing files,
  retained central-data/settings/window state, environment files, symbols, and
  other user data. Runtime settings, links, cached tables, and business-file
  storage stay outside Velopack's replaceable `current` folder.

Verification completed before publication:

- Debug and Release tests pass 114 cases, with the same 2 approval-gated
  workbook parity cases intentionally skipped and no failures;
- the final 0.1.0 package build passed its release safety audit;
- `Costing-App-Setup.exe` is 100,495,346 bytes with SHA-256
  `767a08a9c29a9044f0b74a92d6bbfcd91683dd5947a559e8b0c95db9e5bd66b9`;
- an earlier package of the same implementation installed successfully beneath
  `%LOCALAPPDATA%\Costing.App`, created Costing App desktop/Start shortcuts and
  an uninstall entry, and the project-owned installed stub launched
  `current\ATAG.Costing.WinUI.exe` version 0.1.0;
- the installed process remained healthy and reached the anonymous GitHub feed;
  before a release existed it correctly reported no available release. No Hudl
  process or launcher was invoked;
- the Computer Use bridge still returns `0x80070003`; no screen-control visual
  acceptance is claimed.

Publication and a real older-to-newer installed-app upgrade are the remaining
acceptance actions for this milestone. Do not describe updater installation as
fully accepted until those results are appended here. After that, resume the
recommended **V1.3b versioned dual-insulation document payload and complete
guided editor**.

Final local-installer acceptance subsequently passed against the exact audited
0.1.0 artifact: silent install returned exit code 0, `sq.version` reported
0.1.0, all four existing runtime files beneath the normal ATAG Costing
LocalAppData folder retained identical SHA-256 values through installation, the
installed process remained running, and the process check again found zero Hudl
processes. A local 0.0.9 package containing the same update client is retained
under ignored `artifacts\update-test-old` solely for the real upgrade test.

GitHub publication did not complete in this turn because the required one-time
developer device confirmation was not entered before its code expired. No token
was issued or stored. On continuation, create a fresh device code, publish the
audited commit and all 0.1.0 release assets, anonymously verify the release, then
install the local 0.0.9 baseline and accept the offered 0.1.0 update through the
WinUI Settings surface. Do not ask an installed end user to authenticate; only
the developer publication action requires this one-time confirmation.

### GitHub publication and anonymous upgrade acceptance completed

This supersedes the remaining-publication note immediately above.

- audited source commit `8e9c2ea0d43e5e5defb240361b0c4440f16bd41d`
  was fast-forwarded to public `main`; GitHub's returned tree and final commit
  both matched the exact local Git objects before the ref was moved;
- stable release `v0.1.0` is public at
  `https://github.com/Kiddabob/ATAG-Costing-App/releases/tag/v0.1.0`;
- the GitHub Release contains the one-file `Costing-App-Setup.exe`, full
  Velopack package, portable ZIP, both feed formats, legacy `RELEASES`, and
  `SHA256SUMS.txt`;
- an unauthenticated GitHub API/download check confirmed the stable latest
  release, all seven asset names and sizes, the checksum-manifest bytes, and
  the published installer checksum. Installed users require no GitHub login;
- the ignored 0.0.9 acceptance installer was installed and the exact installed
  app automatically found 0.1.0 from the public feed. Settings displayed the
  full release notes, 91.5 MB download size, and SHA-256 verification notice;
- accepting the app's own `Download and restart` flow downloaded, verified,
  applied, and restarted 0.1.0. The restarted Settings surface reported
  `Version 0.1.0 · installed`, and Velopack logged `Package version 0.1.0
  applied successfully`;
- the four pre-upgrade private runtime files remained present outside the app
  directory (`central-data-state.json`, `exchange-rates.json`, `settings.json`,
  and `window-placement.json`). The restarted UI still reported all five LIVE
  material links, demonstrating that the private retained state survived;
- the Computer Use plugin remains blocked by Windows access in this environment,
  so the acceptance used Windows UI Automation against the exact installed
  Costing App process and the Velopack log. No unrelated application was opened.

The installer/updater milestone is now accepted. The installer remains unsigned,
so Unknown publisher/SmartScreen is still expected until an organisation signing
certificate is provisioned. Resume the recommended **V1.3b versioned
dual-insulation document payload and complete guided editor** next.

### Public README corrected after release

`README.md` was replaced with an installer-first public guide. It now links the
latest GitHub Release and direct one-file installer, explains first-run data
linking and anonymous updates, summarises the actual 0.1.0 scope, and keeps the
clean package/private LocalAppData boundary visible.

The old “Visual Studio 2026” wording was wrong. `ATAG.Costing.sln` declares
Visual Studio solution format/version 17 and the guide now says Visual Studio
2022. It also distinguishes opening the `.sln` from launching the app, and makes
the command-line launch target the WinUI `.csproj`. The exact documented x64
restore, Debug build, and test commands passed: 114 tests passed, the same 2
approval-gated workbook cases were skipped, and there were no failures. NuGet's
optional vulnerability-feed lookup emitted four `NU1900` warnings because that
feed was unavailable, but restore/build/test all completed successfully.

## 2026-08-11 V1.3b dual-insulation document and guided editor

The recommended V1.3b slice is implemented in the local working tree. It has
not been committed, pushed, packaged, or added to the public 0.1.0 installer.
The repository remains on `main` at `52c605f`, aligned with `origin/main` before
these local changes.

### Implemented document and lifecycle boundary

- `.atagcosting` current schema is now version 3 with an explicit
  `SingleInsulatedCore` / `DualInsulation` construction discriminator;
- schema-v1 and schema-v2 documents still upgrade as single-core documents and
  retain their existing reader/recalculation behaviour;
- the dual payload stores locked conductor, first-layer compound/masterbatch,
  second-layer compound/masterbatch, quote values, dimensions, addition rates,
  commercial inputs, and the central-data revision;
- both confirmed scopes are explicit: core/first layer uses finished quote
  length plus the separate start-up length; second layer uses finished quote
  length only;
- two independent extrusion profiles retain their OD band, line speeds,
  optional manual override, setup, operators, and hourly rate;
- Tape, Chalk, Foil, Braid, Lapscreen, and Drain wire selections persist in
  physical inside-to-outside order. They deliberately add no unapproved module
  material formulas;
- approved dual revisions retain raw results, exact display strings, both
  production scopes, both extrusion results, commercial comparisons, and the
  recursive material/production/commercial trace;
- the existing repository, relative-path index, working/approved lifecycle,
  immutable approved save, automatic next working revision, portable browse,
  and duplicate-as-new-project paths now accept both construction kinds;
- index names use the saved dual project name without changing the v1 index
  format or writing a machine-specific storage root.

### Implemented Application and WinUI boundary

- `DualInsulationCostingApplicationService` is the only new orchestration seam.
  It calls `DualInsulationCostingCalculator`,
  `DualInsulationProductionCalculator`, and `CommercialPricingCalculator`;
  WinUI contains no copied material, labour, or commercial formulas;
- `DualInsulationWorkspaceState` provides testable Copper/Compound/Masterbatch
  search and stable optional-module ordering independently of WinUI;
- the Dual Home tile opens a complete scrollable costing editor with one
  searchable Copper selector and separate searchable Compound/Masterbatch
  selectors for both layers;
- the page keeps finished length, core start-up, allowance, risk, markup and
  margin visible, then shows the two calculated scopes side by side;
- supplier quote total/mass remain editable while retained yield, conductor OD,
  and compound specific gravity are locked from central data;
- both extrusion cards expose their own working line profile, optional manual
  speed, setup, operators, labour rate, process time, and labour cost;
- the result surface shows complete material cost, both-line labour, estimated
  cost, risk-adjusted cost, sequential risk-then-markup recommendation, additive
  comparison, target-margin comparison, and the complete trace;
- Open, Save costing, Recalculate, Approve, and Duplicate are available on the
  dual page. Opening from either single or dual surfaces dispatches to the
  document's construction kind rather than trying to read it as the wrong
  editor;
- dual quotation and contract-review wording are explicitly labelled staged.
  The UI does not silently reuse single-core wording before that contract is
  specified;
- the inherited navigation synchronisation bug was fixed. Previously the Dual
  tile briefly selected `costing-dual`, then the left navigation's generic
  Costing selection immediately replaced it with the single-core page.
  Programmatic navigation synchronisation is now suppressed from re-entering
  the selection handler.

Primary new files:

```text
src/ATAG.Costing.Application/Costing/DualInsulationCostingApplicationService.cs
src/ATAG.Costing.Application/Costing/DualInsulationWorkspaceState.cs
src/ATAG.Costing.Application/Projects/DualInsulationProjectPayload.cs
src/ATAG.Costing.WinUI/ViewModels/DualInsulationCostingViewModel.cs
tests/ATAG.Costing.Application.Tests/Costing/DualInsulationCostingApplicationServiceTests.cs
tests/ATAG.Costing.Application.Tests/Costing/DualInsulationWorkspaceStateTests.cs
```

### Verification on 11 August 2026

- the current PC uses .NET SDK `10.0.301`;
- missing NuGet packages were downloaded to the current user's normal package
  cache only; no package path was written into maintained source;
- final x64 Debug solution build succeeded with zero compile errors. The only
  two warnings were `NU1900` because the sandbox could not read NuGet's optional
  vulnerability feed;
- Domain: 50 passed;
- Application/Infrastructure/Reporting: 68 passed;
- workbook parity: 2 evidence tests passed and the same 2 approval-gated golden
  cases remain intentionally skipped;
- new coverage includes schema-v3 dual round trip, schema-v2 legacy read,
  immutable dual approval overwrite rejection, Application orchestration,
  separate production lengths, searchable reference state, and ordered module
  state;
- the exact rebuilt `ATAG.Costing.WinUI.exe` reached an activated native ATAG
  window. Exact-process UI Automation invoked the Dual tile and the project
  diagnostic log recorded `Main section shown: costing-dual` with no following
  single-core fallback;
- the test process was closed after verification. One intermediate build failed
  only because the intentionally open test process held its output DLLs; the
  final build after closing it passed;
- `git diff --check` found no whitespace errors, and maintained source contains
  no current workspace drive, current user path, workbook data rows, database
  link, or credential added by this slice.

### Recommended next slice

V1.3c should define and implement the dual-specific quotation and contract-review
payload before enabling those documents. Start by recording the exact customer
description, two-layer colour/material wording, optional-module wording,
production/reel fields, and approval questions that differ from COR. Then:

1. extend schema 3 with versioned dual quotation/review sub-payloads without
   changing approved V1.3b result evidence;
2. make Reporting consume the saved dual snapshot without recalculating;
3. add a clearly dual A4 preview/PDF and contract-review surface;
4. add round-trip, immutable revision, one-page PDF, and clean-package tests;
5. keep the shared Dual/Flat/D-shape renderer and every optional-module material
   formula as separate later slices;
6. keep the dual and single-core workbook golden cases approval-gated.

Before continuing on another PC, read this file, the root GitHub handoff,
`docs/PROJECT-REVISIONS.md`, and
`docs/CABLE-CONSTRUCTION-AND-VISUALISATION.md`; run the unchanged x64 build and
tests; inspect `git status`; and do not recreate the solution or hard-code the
USB drive letter.

## 2026-08-11 conditional ATAG Design logo from OneDrive identity

The local working tree now extends the existing privacy-safe organisation
branding boundary. This is an additional uncommitted development change on top
of the V1.3b work above; it has not been pushed, packaged, released, or added to
the public 0.1.0 installer.

### Implemented behaviour

- the existing current-user registry reader still checks only
  `Software\Microsoft\OneDrive\Accounts` and performs no Microsoft sign-in or
  network request;
- `OrganisationBrandingPolicy` now owns the testable decision: only a
  `Business*` OneDrive registration whose `UserEmail` or legacy `Email` ends
  exactly in `@atagcables.com` enables ATAG branding;
- the address exists in memory only while the local registrations are checked.
  It is never stored, displayed, logged, or transmitted;
- the supplied transparent 870 x 293 ATAG Design long-logo variants are
  packaged at `Assets/Organisation/ATAGDesignLongLogoDarkText.png` and
  `ATAGDesignLongLogoLightText.png`. No source OneDrive/database path or
  workspace drive is retained;
- when enabled, the long logo appears without an artificial card/background in
  the expanded navigation pane, Home welcome panel, and Settings. Navigation
  and Settings follow the app's light/dark theme while the navy Home banner
  always uses the white-text version;
- when no matching registration exists, all three logo cards remain collapsed,
  Settings explains how to enable them, and standard Costing App branding stays
  active;
- the state is evaluated once at process startup. After signing into or out of
  OneDrive, restart Costing App to refresh it;
- this change affects the application shell only. It does not silently insert a
  logo into quotation or other Reporting templates.

Primary files for this slice:

```text
src/ATAG.Costing.Application/Branding/OrganisationBrandingPolicy.cs
src/ATAG.Costing.WinUI/Assets/Organisation/ATAGDesignLongLogoDarkText.png
src/ATAG.Costing.WinUI/Assets/Organisation/ATAGDesignLongLogoLightText.png
src/ATAG.Costing.WinUI/LocalBrandingService.cs
src/ATAG.Costing.WinUI/AppRuntimeMode.cs
src/ATAG.Costing.WinUI/MainPage.xaml
src/ATAG.Costing.WinUI/MainPage.xaml.cs
tests/ATAG.Costing.Application.Tests/Branding/OrganisationBrandingPolicyTests.cs
```

### Verification

- eight policy cases cover accepted current/legacy ATAG business addresses,
  case and whitespace handling, non-business registrations, similar but invalid
  domains, blanks, and multiple-account discovery;
- final x64 Debug build succeeds with zero errors. The same two `NU1900`
  warnings only report the sandbox-blocked optional NuGet vulnerability feed;
- Domain: 50 passed;
- Application/Infrastructure/Reporting: 76 passed;
- workbook parity: 2 evidence tests passed and the same 2 approval-gated golden
  cases remain intentionally skipped;
- total: 128 passed, 2 intentionally skipped, 0 failed;
- the exact rebuilt app launched on this PC, detected its matching ATAG OneDrive
  registration, reported `ATAG Costing App`, logged only
  `Organisation branding: ATAG Design logo enabled`, and reached Home without an
  exception. The exact test process was then closed;
- no email address, credential, OneDrive folder, current-user path, or removable
  drive letter was added to maintained text source.

The recommended product-development slice remains V1.3c dual-specific
quotation and contract-review payload/reporting. Keep report logo placement and
wording in that versioned Reporting slice rather than coupling it to the shell.

## 2026-08-11 version 0.2.0 release candidate

The user authorised publication of the completed V1.3b dual-insulation editor
and conditional ATAG shell branding as the next GitHub version. Semantic version
`0.2.0` was selected because this adds a new construction workflow and saved
document capability rather than a patch-only correction.

Release preparation completed before the source commit:

- `Directory.Build.props` and `CHANGELOG.md` now define and describe 0.2.0;
- the authoritative Release suite passed 128 tests, with the same 2
  approval-gated workbook golden cases intentionally skipped and no failures;
- `tools/Build-Release.ps1` completed the self-contained x64 publish, blocked
  file audit, Velopack 1.2.0 package, update feeds, portable ZIP, one-file
  installer, and checksums;
- all six checksum-manifest entries match their generated files;
- both generated archives contain no Access/SQL/workbook/saved-costing files,
  retained settings/data, environment files, or debug symbols;
- the publish contains no current workspace/user/old removable-drive string or
  named costing-workbook/database-backup source path;
- the ATAG Design logos are compiled into `ATAG.Costing.WinUI.pri` under their
  two `Assets\Organisation\ATAGDesignLongLogo*` names rather than shipped from
  their original workspace locations;
- the final transparent light/dark-logo candidate was rebuilt from scratch;
  `Costing-App-Setup.exe` is 100,481,236 bytes with SHA-256
  `65c09e11cac11c44cff3080c3d50f08fc6547160cd654b99f03c78d6b017389c`;
- the exact candidate silently upgraded the local installed app from 0.1.0 to
  0.2.0 with exit code 0. All four retained private LocalAppData files existed
  before and after with identical SHA-256 values;
- the exact final same-version installer updated the installed WinUI PRI to the
  exact final publish hash while all four private-data hashes remained
  identical;
- the exact installed 0.2.0 executable launched, selected `ATAG Costing App`,
  enabled the logo branch, activated a native window, and reached Home. The
  verification process was then closed.

The installer remains unsigned, so Unknown publisher/SmartScreen remains the
only known distribution caveat. Initial 0.2.0 source commit `32ffd7a` has been
fast-forwarded to public `main`, but the user supplied final light/dark branding
assets before release publication. A final branding follow-up commit, public
GitHub Release, and anonymous verification are still pending and must be
recorded separately after they actually succeed. No `v0.2.0` tag or Release
exists at this point.

## 2026-08-11 public version 0.2.0 accepted

This section supersedes the pending publication statements in the release-
candidate section above. The transparent theme-aware ATAG branding follow-up
was committed as `518c72becb8630f07758c7a4a8923db0abafcce6`, pushed to
`origin/main`, and used as the exact source and tag target for the public
release.

GitHub Actions run
`https://github.com/Kiddabob/ATAG-Costing-App/actions/runs/31475824203`
completed successfully in 2 minutes 50 seconds. Restore, the authoritative
build/test/audit/package script, version read, public release creation, and
artifact retention all succeeded. The resulting
`https://github.com/Kiddabob/ATAG-Costing-App/releases/tag/v0.2.0` is published,
not a draft, not a prerelease, and is the repository's latest stable release.
Its tag resolves to the final branding commit above.

The seven published application assets are:

```text
assets.win.json                         205 bytes
Costing-App-Setup.exe           100,530,264 bytes
Costing.App-0.2.0-full.nupkg     95,937,112 bytes
Costing.App-win-Portable.zip     95,882,540 bytes
RELEASES                                  81 bytes
releases.win.json                      2,653 bytes
SHA256SUMS.txt                            525 bytes
```

The public installer and public checksum manifest were downloaded again from
the release, independently of the workflow. The downloaded installer hash is
`66ce6fe81771557682e588900b5defbd9b73474bcd1a44647fbe1ddea9d53ffd`,
which exactly matches both `SHA256SUMS.txt` and GitHub's asset digest. The
manifest lists and matches all six generated application/update files; the
checksum manifest itself has GitHub digest
`86e35189f81abfea8f947d6cfac8fa63f4516b65a502f8743a1c75fae32310bb`.

Release acceptance therefore includes the V1.3b dual-insulation editor,
schema-v3 save/revision workflow, the transparent light/dark ATAG long logos,
and the already-transparent square app icon. The authoritative release suite
remains 128 passed, 2 intentionally skipped approval-gated golden cases, and 0
failed. The earlier local upgrade evidence confirms that the four private
LocalAppData files were preserved byte-for-byte. The installer is still
unsigned, so Windows may show Unknown publisher or SmartScreen.

Continue product development with the recommended V1.3c dual quotation and
contract-review payload/reporting slice. Do not recreate the solution, replace
the accepted v0.2.0 tag/assets, or hard-code a USB drive letter.

## 2026-08-11 installed-branding, updater, and taskbar hotfix accepted

This section supersedes v0.2.0 as the latest public/installed state. The user
reported that the reserved logo spaces were blank in installed v0.2.0. The app
had correctly detected the ATAG OneDrive account and selected `ATAG Costing App`,
but the two logo PNGs were not present as loose Velopack application files and
the dynamic `ms-appx` image load did not resolve them in the Release payload.

The accepted correction was delivered and exercised as three small releases:

- v0.2.1, source/tag `19964613a3cd002034cc3ae80f8ed0fdf63eeb82`,
  carries both transparent logo variants and `AppIcon.ico` as explicit publish
  and update-package files, loads the long logos directly from the installed
  application directory, removes redundant navigation-title overlap, and turns
  the existing automatic check into a visible install-or-later launch dialog;
- v0.2.2, source/tag `7a1748cbf114e46646f1afff2163ab2944692276`,
  is the no-calculation-change follow-up used to exercise that launch dialog
  against a real newer GitHub release;
- v0.2.3, source/tag `065537db5eadda8131c19139cbdeef6e08196e44`,
  embeds the supplied transparent square ICO in the Windows executable. This
  fixes the generic taskbar/Desktop/Start-menu icon that remained even though
  the loose ICO and WinUI title-bar icon were correct.

GitHub Actions runs 31479185856, 31480209956, and 31481224931 all completed
successfully. Public v0.2.3 is the latest stable Release, not a draft or
prerelease:
`https://github.com/Kiddabob/ATAG-Costing-App/releases/tag/v0.2.3`.
It contains seven application assets. The installer is 100,822,733 bytes with
GitHub SHA-256
`e001011ee2f2921d2cd1319f850304f67aa3cdb53f9d6c214906e536fd0df717`.
The full update package is 96,229,581 bytes with SHA-256
`086baedb11a7b2ff99e6988a88f1b4df02e21d9f199c0695b8e9e00ed275993d`.
The package downloaded by the app has that exact size and hash.

End-to-end acceptance evidence:

- installed v0.2.0 detected v0.2.1 through its existing Settings updater,
  downloaded the public package, applied it, and restarted as v0.2.1;
- the updater-installed v0.2.1 displayed both long logos correctly. Its three
  loose icon/logo files matched their maintained source SHA-256 values;
- installed v0.2.1 displayed the new v0.2.2 launch dialog on Home with version,
  download size, cumulative notes, checksum assurance, `Download and restart`,
  and `Later`; the update completed and restarted as v0.2.2;
- installed v0.2.2 displayed the same launch dialog for v0.2.3. The dialog's
  primary action downloaded, verified, installed, and restarted into exact
  version `0.2.3+065537db5eadda8131c19139cbdeef6e08196e44`;
- the installed v0.2.3 executable's extracted 32 x 32 icon matches the supplied
  ICO pixel-for-pixel, and a live desktop capture shows that ATAG icon as the
  running taskbar button rather than the previous generic window icon;
- the release gate now rejects a publish that omits or changes the loose ICO or
  either logo, or whose executable icon differs from the supplied ICO;
- every final release gate passed 128 tests, with the same 2 approval-gated
  workbook golden cases intentionally skipped and 0 failures;
- all four private LocalAppData files remained present through the update chain.
  `settings.json` stayed byte-identical; central-data state, exchange-rate data,
  and window placement were legitimately refreshed by the running app during
  the multi-launch test rather than being supplied by an update package.

The installer remains unsigned, so Unknown publisher/SmartScreen is still the
only known distribution caveat. Continue product development from V1.3c dual
quotation and contract-review payload/reporting. Do not recreate the solution,
replace the accepted tags/assets, or hard-code a removable-drive letter.

## 2026-08-11 local Production Speed Library and changelog-reader slice

Development continued from accepted public v0.2.3. This slice is intentionally
local and uncommitted: it has not been pushed, packaged, tagged, or published,
and v0.2.3 remains the latest accepted public release.

The Line Speed Library goal is now represented directly in the application:

- `Production speeds` is a first-class navigation destination;
- each user can create any number of production lines, and every line owns its
  ordered maximum-finished-OD speed bands, above-maximum speed, and known cable
  runs;
- known runs record process, core/finished ODs and tolerances, capstan and
  extruder machine-dial values, measured speed or produced length plus running
  minutes, and notes;
- capstan/extruder settings are calibration evidence and are never silently
  treated as metres per hour;
- estimates use up to three sufficiently similar measured runs with visible
  source/confidence/evidence, then fall back to the selected line's OD bands;
- the estimator refuses to invent a fallback speed when no usable measured run
  or OD band exists;
- the current COR, dual first extrusion, or dual second extrusion geometry can
  be copied into the estimator. The first dual scope includes its internal core
  start-up length; the second uses finished quote length only;
- an accepted estimate is applied only as a visible manual speed to COR or the
  chosen dual extrusion, so production time and labour continue through the
  existing traceable costing rules.

Clean installations now start with no production lines, observations, machine
settings, or estimator example values. The known general insulation rule
(1.00/15,000; 1.20/13,000; 2.00/8,000; 2.50/6,000; above 700 m/h) is available
only through a clearly labelled, confirmed **Add general starter profile**
action and remains editable. Runtime data is stored at the current user's
LocalAppData `production-speed-library.json`; that filename is Git-ignored and
the release script now rejects it if it ever enters a package.

The cumulative update notes were also refined:

- duplicate GitHub markdown version headings are removed by the Application
  composer;
- every release is displayed as a separate styled card;
- Settings and the automatic update prompt use a bounded independent changelog
  scroller, while `Download and restart` / `Later` stay outside the scroller;
- **Open full screen** opens an owned, resizable WinUI reader on the main app's
  display. It stays above Costing App but is not topmost over unrelated apps,
  and exposes an explicit exit-full-screen control.

Verification on this PC:

- x64 Debug solution build: 0 warnings, 0 errors;
- complete suite: 137 passed, 2 approval-gated workbook golden fixtures
  intentionally skipped, 0 failed;
- x64 public-review WinUI build: 0 warnings, 0 errors;
- `git diff --check` found no whitespace errors (only the existing exFAT
  CRLF-conversion notices);
- maintained source contains no fixed drive letter or machine-local database
  path introduced by this slice;
- the Windows Computer Use helper failed before it could enumerate windows with
  `EPERM` on the Codex application folder, including after its required reset
  and retry. Live visual acceptance of the two new views therefore remains
  pending; no guessed window or screen coordinate was used.

On continuation, preserve this dirty working tree, re-run build/tests, and
visually inspect `Production speeds`, the bounded Settings changelog, and the
owned full-screen changelog reader when Windows control is available. Do not
publish until the user accepts that UI. The next Line Speed Library expansion
should build on this model with real user-entered per-line bands and measured
runs; do not seed private production values or reinterpret machine dial values
as speeds.

## 2026-08-11 version 0.3.0 release candidate

The user authorised publishing the Production Speed Library and improved
cumulative changelog as the next public version, using their own application
screenshots for visual feedback while the Windows control bridge remains
unavailable. Version `0.3.0` was selected because this is a new application
module rather than a patch-only correction.

Release preparation completed in the working tree:

- `Directory.Build.props`, `CHANGELOG.md`, the public README, installation
  guide, scope document, and Production Speed Library guide describe 0.3.0;
- the normal x64 Debug solution build passed with zero warnings and errors;
- the x64 interface-only/public-review build passed with zero warnings and
  errors;
- the authoritative Release suite passed 137 tests, with the same 2
  approval-gated workbook fixtures intentionally skipped and no failures;
- `tools/Build-Release.ps1` completed the self-contained publish, privacy audit,
  Velopack 1.2.0 package, portable ZIP, one-file installer, feeds, and checksum
  manifest;
- all generated manifest hashes match their files. The local installer is
  100,853,209 bytes with SHA-256
  `b4c3b8c49d2e3aacd4e9b6578ca62ef5137419172ad6397dcd1d70834670e3ac`;
- both generated archives contain no database/workbook/saved-costing files,
  retained settings/data, production-speed library, environment file, CSV, or
  debug symbols;
- the publish contains neither the current workspace path nor current-user
  path, and `production-speed-library.json` is not tracked;
- the release remains unsigned, so Windows may still show Unknown publisher or
  SmartScreen.

This section records the local candidate only. The source commit, public push,
GitHub Actions release, final public asset hashes, and anonymous download
verification must be appended after they actually succeed. Do not describe
0.3.0 as public until that evidence exists.

## 2026-08-11 private per-user launch chooser follow-up

The initial 0.3.0 source slice was committed and pushed as
`30cf5ee7bc7535879416ed28c54f73bbaed6c272`. GitHub Actions run
`31516851021` was then cancelled before packaging or publication when the user
added this requirement. It finished with conclusion `cancelled`; no v0.3.0 tag
or Release was created by that run.

The app now supports a privacy-safe, current-Windows-profile test opt-in at
`HKCU\Software\Costing App\Developer Options`, DWORD
`ShowLaunchModeChooser = 1`. The installer does not create this value and no
user name, email address, SID, or identity hash is compiled or published. The
value is enabled on this PC for the user's profile. Each shortcut launch offers
**ATAG version** or **Blank test version**, and the choice is held only for that
process. ATAG mode uses the normal local data and branding. Blank mode reuses
the isolated interface-only services and paths, with no database navigators,
retained tables, JSON preferences, exchange service, production-speed library,
or ATAG quotation identity.

Exact-process UI Automation acceptance on this PC selected both accessible
buttons independently. ATAG mode opened a responsive window titled
`ATAG Costing App`. Blank mode opened a responsive window titled `Costing App`
and exposed the banner `Public review · no organisation data included`. Both
test processes were closed normally.

After this follow-up, normal and interface-only x64 Debug builds again passed
with zero warnings and errors. The authoritative Release suite passed 137
tests, with 2 approval-gated workbook fixtures intentionally skipped and no
failures. The rebuilt installer is 100,855,289 bytes with SHA-256
`600706174469e4882f4d9585886cdb71b6442e4500b7ec0d0aa60543a487bc3c`.
The full package is 96,262,137 bytes with SHA-256
`da928180ce21efa04881f5aadeeee3d0e44a28e9e7aec41747303a9b3a65b126`.
All seven manifest entries match. Both generated archives contain zero blocked
database/workbook/costing/cache/settings/production-library/environment/symbol
entries, no production library is tracked, and the publish contains no current
workspace path, current-user path, or tester identity.

This follow-up is not yet committed or released. Commit and push it, dispatch a
fresh v0.3.0 workflow, verify the exact public tag and seven assets, and
anonymously download and hash the installer before calling v0.3.0 accepted.

## 2026-08-11 public v0.3.0 accepted

The chooser follow-up was committed and pushed as
`f816f99d761994f60e7362c7bd1a9d574e1ec9c4`. Fresh GitHub Actions run
`31518402651` completed successfully, including restore, the authoritative
build/test/privacy/package gate, public Release creation, and retained build
evidence. Stable Release:
`https://github.com/Kiddabob/ATAG-Costing-App/releases/tag/v0.3.0`.
The lightweight `v0.3.0` tag points exactly to `f816f99d761994f60e7362c7bd1a9d574e1ec9c4`.
The Release is neither draft nor prerelease and is the repository's latest
stable release.

All seven assets are present. The public one-file installer is 100,855,276
bytes with SHA-256
`b832bf6601e5cbbbc8a0cedb126349397a561b984b4730ccda130a186999adcf`.
The public full update package is 96,262,124 bytes with SHA-256
`c22ae573fbb603b632007ece60ec1cee501be3130008c81b621bdb52f311cd15`.
Both were downloaded anonymously from the public Release and matched both the
published checksum manifest and GitHub's asset digest. The public Velopack feed
reports version 0.3.0, the same update-package size, and the same SHA-256. The
downloaded nupkg has 561 entries and zero blocked private-data entries.

Public v0.3.0 is therefore accepted. The user can install it directly or update
from an older installed version without a GitHub login. On this PC the generic
current-user test opt-in remains enabled, so the normal shortcut offers ATAG or
Blank for each launch; fresh installs and all other Windows profiles remain on
the normal automatic path. The installer is still unsigned and may show
Unknown publisher or SmartScreen.

## 2026-08-11 public v0.3.1 long-logo spacing update accepted

The user requested a deliberately small second update to exercise the new
cumulative changelog UI. Version 0.3.1 trims only the transparent frame around
the approved light/dark ATAG Design long logos and reduces the three WinUI image
margins in the navigation pane, Home banner, and Settings from 8/4/6 to 2 px.
Both source images changed from 870 x 293 to 828 x 251 with exactly 4 transparent
pixels on every edge. A direct pixel comparison before replacement proved every
visible artwork pixel and colour unchanged. The final source hashes are:

- light-text logo:
  `eb6e4d045ae530f37a3a8c3ffb1ea9b9bb1eb7015c9fac863ad8d1ea0c2ba82b`;
- dark-text logo:
  `117cbe4796af37df4012a0c87aa9cb180cc227401305cedac24d6f530f4e444b`.

Normal and interface-only x64 Debug builds passed with zero warnings/errors.
The authoritative Release gate passed 137 tests, with 2 approval-gated workbook
fixtures intentionally skipped and 0 failures. Local package audits found zero
blocked private entries, no current workspace/user path, and exact checksum-
manifest matches.

Commit/tag target `af714d72327c40f33b22d8d8eb1ef40f48aa7cef` was pushed and
GitHub Actions run `31520231191` completed successfully. Stable Release:
`https://github.com/Kiddabob/ATAG-Costing-App/releases/tag/v0.3.1`.
It is latest, neither draft nor prerelease, and contains all seven assets. The
public installer is 100,846,385 bytes with SHA-256
`e4cb494a0770bc211ef1a670eb6b351e4cce15978b3dedc416bfc9008a523f0e`.
The public update package is 96,253,233 bytes with SHA-256
`d0fc7212e3ca9d1b333c7eee8ecef865498e3b42d274ed6f135bf0d0583e41ba`.
Anonymous downloads matched the public checksum manifest and GitHub digests;
the public Velopack feed names exact version 0.3.1, size, and hash. The public
nupkg contains 561 entries and zero blocked private-data entries.

The Computer Use plugin was initialised exactly as documented and retried after
a kernel reset, but both attempts failed before window enumeration with `EPERM`
on the Codex application folder. Do not infer a WinUI screenshot acceptance
from that failure. The user is the final appearance check through the installed
v0.3.0-to-v0.3.1 update and can confirm that the cumulative 0.3.1 and 0.3.0
cards use the new changelog style. The installer remains unsigned.

## 2026-08-11 public v0.3.2 launch-chooser sizing update accepted

The user confirmed on installed v0.3.1 that the optional ATAG/blank launch
chooser clipped its second choice, disabled the maximise button, and could only
be maximised by double-clicking the title bar. The cause was explicit
`IsResizable = false` and `IsMaximizable = false` presenter configuration, a
fixed 510 px initial height, and a non-scrolling content stack.

Version 0.3.2 enables normal resize and maximise behaviour, opens at a
display-aware size up to 760 x 680 px, clamps that size to the current pointer
display's work area, and wraps the choice content in an independent vertical
`ScrollViewer`. Both choices should therefore be visible at the normal opening
size and remain reachable when the window is deliberately reduced or used on a
compact display. No costing, saved-document schema, database-link, production-
speed, or reporting rule changed.

The normal and blank-interface x64 Debug builds passed. The initial restore
reported only temporary NU1900 vulnerability-feed warnings; compilation had
zero source errors. The authoritative Release gate passed 137 tests, with 2
approval-gated workbook fixtures intentionally skipped and 0 failures, then
completed its public-data audit and Velopack packaging.

Commit/tag target `9453121aa6c4d92f4d089480425b157c1a8b7123` was pushed and
GitHub Actions run `31522144111` completed successfully. Stable Release:
`https://github.com/Kiddabob/ATAG-Costing-App/releases/tag/v0.3.2`.
It is latest, neither draft nor prerelease, and contains all seven assets. The
public installer is 100,851,998 bytes with SHA-256
`712a5eedb6027cf7a50bf1ddb5879d8e4992d75422bb794dd435d4486f0182cf`.
The public update package is 96,258,846 bytes with SHA-256
`83d4a874677ddd8f501984fcf0141189288522b7d442f9344589141d795a2e43`.
Anonymous downloads matched the public checksum manifest and GitHub digests;
the public Velopack feed names exact version 0.3.2, size, and hash. The
downloaded nupkg contains 561 entries and zero blocked private-data entries.

This is deliberately the first of two releases held above installed v0.3.1 so
the redesigned cumulative changelog can be tested with more than one pending
version. Keep v0.3.1 installed, publish one further small v0.3.3 update, and
then use the v0.3.1 updater to inspect separate v0.3.3 and v0.3.2 cards before
installing. The user will also perform the installed visual acceptance of the
chooser through that later update. The installer remains unsigned.

## 2026-08-11 public v0.3.3 app-owned Appearance update published

Version 0.3.3 is the deliberately held second update above the user's installed
v0.3.1. Settings > Appearance now uses large System, Light, and Dark preview
cards, six named accent swatches, a validated custom six-digit RGB hex colour,
and the existing Mica/Acrylic choice. The selected colour overrides WinUI's
`SystemAccentColor` and generated light/dark palette only inside Costing App;
it does not change Windows. Windows text-size and contrast links remain clearly
separate because those accessibility settings still take priority. Theme,
accent, custom colour, and material persist per Windows profile in the existing
`settings.json`. Older files default safely to the existing coral accent. The
isolated blank-review preference service remains memory-only and does not read
or write installed preferences.

The x64 WinUI build compiled, the authoritative release gate passed 137 tests
with 2 approval-gated workbook fixtures intentionally skipped and 0 failures,
and the normal package privacy/asset/icon checks passed. A hidden normal launch
remained running at the optional mode chooser. A separate interface-only smoke
publish bypassed that chooser, constructed and activated the full main window,
rendered with zero visible default input values, and logged no unhandled WinUI
exception. The Computer Use helper was initialised and retried once as its own
instructions require, but still failed before window enumeration with `EPERM`
on the Codex application folder, so do not claim screenshot acceptance.

Source commit/tag target
`fb738f7711f18a004599d205715d5662819ac271` was pushed and successful GitHub
Actions run `31524553262` published stable Release:
`https://github.com/Kiddabob/ATAG-Costing-App/releases/tag/v0.3.3`.
It is neither draft nor prerelease and contains all seven expected assets. The
public installer is 100,856,244 bytes with SHA-256
`20d671b67d4cc21839082275bb1438516b32ac9366aca990349b60d53368b501`.
The public update package is 96,263,092 bytes with SHA-256
`e3c3ad4f5443f37fc2aa86abedf1128021e3ab8d76383714596b14723c14f3ee`.
Anonymous downloads match the checksum manifest and GitHub asset digests. The
public Velopack feed names exact version 0.3.3, size, notes, and hash. The
downloaded nupkg contains 561 entries and zero blocked private-data entries.

Immediate installed acceptance: keep v0.3.1 open long enough to see the update
prompt, inspect that the compact scroll area contains separate 0.3.3 and 0.3.2
cards, and optionally expand the full-screen reader before choosing Download
and restart. After updating, confirm the launch chooser shows both choices and
has working resize/maximise behaviour, then open Settings > Appearance and test
System/Light/Dark, two preset accents, one valid custom colour, and one invalid
hex entry. The choice should survive a normal relaunch and must not alter the
Windows accent. The user is the final visual judge. Once accepted, resume the
Production Speed Library/line-speed refinement already recorded above rather
than reopening release infrastructure. The installer remains unsigned.


## 41. Public v0.3.4 appearance correction - 11 August 2026

The installed v0.3.3 screenshots accepted the redesigned cumulative changelog
(two separate 0.3.3 and 0.3.2 cards) and the resizable/maximisable launch
chooser, then exposed four appearance defects: the complete Settings column was
anchored too close to the navigation edge on wide windows, the primary launch
choice mixed black and white text on one accent surface, System theme did not
refresh when an accent changed, and a custom RGB value such as `#D50000` was
shown in the selector without becoming the exact primary control fill.

v0.3.4 centres the complete Settings workspace in the available content area
and raises its maximum width from 860 to 980 pixels. The primary launch choice
now lets both text lines inherit AccentButtonStyle's single contrast-aware
foreground. Appearance uses stable app-owned accent brush instances: changing
a preset or custom colour mutates those brushes directly, so System, Light, and
Dark respond immediately without a forced theme round-trip. The default fill
uses the exact entered RGB colour; only pointer/disabled states use alpha. The
active name and exact hex are displayed together, and WCAG luminance selects
black or white on-accent text without altering the requested colour. Existing
per-profile preferences are retained. No costing, saved-document schema,
database-link, production-speed, or reporting logic changed.

The x64 Debug solution build succeeded with zero errors. The authoritative
release gate passed 137 tests, with 2 approval-gated workbook fixtures
intentionally skipped and 0 failures. The package asset, executable-icon,
checksum, and privacy gates passed. A separate interface-only publish
constructed and activated the full WinUI main page, remained alive after six
seconds, logged no unhandled exception, and rendered zero visible default input
values.

Commit/tag `c03e3884a1a1efd4ee543ab7c49f01b43bb9560d` was published by
successful GitHub Actions run `31526850921`. Latest stable Release:
`https://github.com/Kiddabob/ATAG-Costing-App/releases/tag/v0.3.4`.
The public installer is 100,856,851 bytes, SHA-256
`36aa8bdeb41fef7cf0ecdd77f0c618cad2b9f01d2e4374bad60a4d9832825208`.
The public update package is 96,263,699 bytes, SHA-256
`ee64d88a07ba3a2b7d12d471ce68c1962af5a1b7a59901a5e02919e91f686c3f`.
Anonymous downloads match the public checksum manifest and GitHub asset
digests. The downloaded nupkg has 561 entries and zero blocked private-data
entries; the public feed reports exact version 0.3.4, package size, notes, and
hash.

Immediate installed acceptance: update v0.3.3 to v0.3.4, confirm Settings is
centred on a large window, select a preset while Theme is System and verify the
app changes immediately, apply `#D50000` and verify the active label includes
that exact value and the primary controls are exact red, then relaunch and
confirm persistence. Reopen the optional launch chooser and confirm both lines
on the ATAG choice use one readable foreground. The user remains the final
visual judge. Once accepted, resume the recorded Production Speed Library and
line-speed refinement rather than reopening updater foundations. The installer
remains unsigned.

## 2026-08-13 shared costing-page layout and preview goal

Before further construction types are treated as visually complete, repair the
Dual Insulation costing page so it matches the layout and interaction
improvements already made to the single-core COR page. This explicitly includes
the same usable live-preview experience and equivalent placement/hierarchy for
shared costing sections.

The maintainable target is a shared costing-page shell and reusable section
components. A general edit to shared areas such as the result header, costing
basis, material cards, production/labour presentation, preview rail, action
placement, validation/status treatment, and calculation trace should flow to
COR, Dual Insulation, and later cable constructions without copying XAML or
presentation logic between pages. Each construction keeps only its true
differences as modular content: for example a second extrusion layer, braid,
tape, foil, lapscreen, drain wire, flat/profile geometry, or other optional
physical modules.

Treat this as a recorded future layout/refactoring goal, not part of the focused
Braid Coverage/Buncher Lay release requested on 13 August 2026. Do not use the
refactor to change accepted costing formulae or saved-result evidence. Add
shared visual regression/smoke coverage when this goal is implemented so a
change to the common layout is demonstrated on every supported construction.

## 2026-08-13 Braid Coverage and Buncher Lay v0.4.0

The focused braid slice is implemented as version 0.4.0. The former Braid
calculator placeholder now opens `BraidCalculatorView`, backed by
`BraidCoverageCalculator` in the Domain project rather than page-owned maths.
It contains the workbook's 45-row core lay-up/combined-OD-factor table, the
0.1/0.2 mm effective-wire list, an intentionally expanded 1-to-10
ends-per-carrier list, target coverage/core OD/cable length inputs, live 16- and
24-carrier comparison, and a 16-step formula/substitution/unit/rounding trace.

The same page includes an exact Buncher Lay selector over the workbook's four
large-buncher and fourteen small-buncher rows. Choosing a target lay returns
its machine and Gear A/Gear B pair without interpolation. The domain calculator
deliberately preserves the workbook's existing perpendicular-angle formula and
labels that output explicitly; do not silently change it without business
approval. Reverse braid calculation and saved-costing insertion remain future
work over this reusable module.

Verification completed before publication:

- x64 Debug solution build: 0 warnings and 0 errors;
- 140 tests passed, 2 approval-gated workbook fixtures intentionally skipped,
  and 0 failed;
- the workbook reference case reproduces its target fill, both pitches,
  angles, reference coverage, and strand/bobbin length;
- the authoritative release builder completed its required-logo, embedded-icon,
  privacy-file, checksum, publish, and Velopack gates;
- the local unsigned v0.4.0 installer/update assets are in
  `artifacts/release/releases`.

After this section is committed and pushed, run the existing manual GitHub
release workflow, verify the public v0.4.0 tag/assets/feed anonymously, and
record the commit/run/hash results here and in the root handoff. The installer
remains unsigned. The recommended later development slice is the recorded
shared costing-page shell and Dual layout/live-preview parity goal, not a braid
formula rewrite.

## 2026-08-13 deferred Braid/Buncher UX and copper-data refinement

Record the following as a future slice only; do not fold it into the v0.4.1
release-note layout hotfix:

- replace the simple 1-to-45 core-layout ComboBox rows with a structured,
  styled selector whose columns separately present core count, cores-per-layer
  lay-up, and combined-OD multiplier;
- source ends-per-carrier and effective wire diameter from the application's
  complete copper reference data rather than two independent hard-coded lists;
- keep the loose/multi-wire bunch limit at 1 to 10 ends and include only
  available copper wire sizes up to 0.25 mm; after the end count changes,
  filter the effective-diameter choices to copper constructions that contain
  that number of ends;
- add `0.15 mm` to the effective-wire choices; the user confirmed that the
  earlier `1.5` wording meant `0.15 mm`, and `0.25 mm` is the firm maximum;
- visually identify the recommended 16- or 24-carrier result with a strong
  unambiguous accent and plain-language recommendation;
- make pitch and effective wire diameter the dominant large setup values,
  because both are settings used at the braider;
- split Target Lay Length/Buncher selection into its own module so Braid
  Coverage and Buncher Lay can each gain an independent LIVE Preview later;
- the Target Lay drop-down must show lay lengths only. Buncher size and Gear A/
  Gear B remain calculated/displayed results outside the drop-down.

Do not infer a best-carrier rule solely from colour. Define and test the
selection rule first (for example target attainment, usable pitch range,
material/length consequence, and machine availability), then make the colour
communicate that explained result. Preserve the full calculation trace.

## 2026-08-13 public v0.4.0 accepted and v0.4.1 card-layout hotfix

Public v0.4.0 is accepted. Commit/tag
`37711cd4296cef120022dd035a1188fe3f0c9911` was published by successful
GitHub Actions run `31714428415`. Stable Release:
`https://github.com/Kiddabob/ATAG-Costing-App/releases/tag/v0.4.0`.
The seven public assets target that exact commit. The installer is 100,872,001
bytes with SHA-256
`00072149c1c14d4fae461286e53a41498728e6662b8d7db5a763d0932e3f749d`;
the full updater package is 96,278,849 bytes with SHA-256
`644ed2b44c9b6c57d45055bd873192f00b7306a254bcac3e5395d17dea9d576d`.

The installed v0.4.0 update notes revealed that a long Markdown bullet split
across source lines was rendered as one ticked first line followed by separate,
over-indented paragraphs. v0.4.1 merges indented physical continuation lines
into the preceding bullet before creating the card, so WinUI wraps the complete
change inside one consistent text column beside the tick. No costing or braid
rule changes in this hotfix. The x64 Debug build has zero warnings/errors and
the suite has 140 passing tests, 2 intentionally skipped approval fixtures,
and no failures. Complete the normal release gate and public verification,
then record its exact commit/run/assets in this handoff.

### Public v0.4.1 verification completed - 14 August 2026

The hotfix is now fully published and verified. Exact release commit/tag:
`401adc3f9e4208600ab94bd76b201724b4c79769`. GitHub Actions run
`31778077408` completed successfully. Stable Release:
`https://github.com/Kiddabob/ATAG-Costing-App/releases/tag/v0.4.1`.

All seven public assets target that exact commit. The installer is 100,871,130
bytes with SHA-256
`4850cca5e083c6cb921b35deab329c2fbb470d1c6f21d88b134c852c6cd976e3`.
The full updater package is 96,277,978 bytes with SHA-256
`a92411ca4d8ce277d92c1925c0e0d99e1baa3c7f715e836e30d4706fd4adcb10`.
An anonymous download of that package matches the public checksum manifest,
GitHub asset digest, and `releases.win.json` feed. The downloaded package has
561 entries and zero blocked private-data entries. The release remains unsigned.

This completes the interrupted v0.4.1 work. Do not continue automatically into
the deferred braid/copper UX requirements or the shared costing-page refactor.
The next task should begin only when the user explicitly asks for it. Installed
visual acceptance of the corrected card wrapping can be performed by updating
v0.4.0 to v0.4.1.
