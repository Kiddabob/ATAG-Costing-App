# ATAG Costing test baseline — 25 August 2026

This is the concise current-state document to read before beginning the next
modularity or optimisation slice. It records the tested local state of public
version `0.7.0` plus the uncommitted NuGet and central-data cadence changes.
Historical release detail remains in `CONTINUE-ATAG-COSTING.md`.

## Executive result

The calculation libraries, Release gate, native page navigation, key
engineering interactions, hardware 3D path and WARP fallback all pass. The app
is usable as a development baseline, but it is not yet at the desired polished
laptop-ready level. Three presentation/lifecycle defects should be fixed before
the modularity pass begins:

1. Release-published builds do not show the long ATAG logo. The asset is in the
   package, but the current `StorageFile.GetFileFromPathAsync` path logs
   `UnauthorizedAccessException`. Debug builds show the logo correctly.
2. Closing the main app while the LIVE Preview window is detached logs an
   unhandled `NullReferenceException` in
   `LivePreviewWindow.LivePreviewWindow_Closed`.
3. At a 1000 x 800 compact viewport, the Braid header's two-column layout
   leaves the explanation in a near single-word/single-line column. The
   Buncher header and one Production Speeds supporting line are also cramped,
   but less severely.

## NuGet reliability boundary

The repeated local NuGet issue was a cached `NU1900` advisory-feed failure in
generated assets after a restricted/offline restore. It was not a broken
package graph. A forced online restore reached both configured sources and
restored all eight projects cleanly.

The durable policy is now:

- routine local restore/build: `NuGetAudit=false` by default in
  `Directory.Build.props`, so an offline USB workspace does not repeatedly
  fail while trying to reach the advisory service;
- Release gate: `tools/Build-Release.ps1` forces `NuGetAudit=true`, refreshes
  restore state, and treats `NU1900` through `NU1904` as errors;
- consequence: local development is deterministic, while a package cannot be
  produced when the live security audit is unavailable or reports a known
  vulnerability warning.

The local SDK used for this baseline is .NET SDK `10.0.301` with runtime host
`10.0.9`.

## Central-data relink cadence

Before this pass, the app attempted one combined automatic refresh of all
configured links every **30 seconds**. It is now exactly **once per hour** via
`CentralDataRefreshPolicy.AutomaticRefreshInterval`.

- The timer begins with `MainPage`; there is no extra immediate timer-driven
  refresh on startup.
- All configured Copper, Compound, Masterbatch, Supplier and Yield links are
  processed together on a tick.
- Manual refresh remains immediate.
- A partial or failed automatic refresh pauses further automatic attempts
  until the user manually retries, so an unavailable database is not hammered
  once an hour indefinitely.
- The exact one-hour policy has an automated regression test.

This PC currently shows `0 of 5 LIVE`, so no real external database was
mutated or used for an integration refresh during this audit. The policy,
timer wiring and failure-pause behavior were verified by source inspection and
automated tests; a connected one-hour soak remains an environment test for a
PC with approved data sources.

## Automated verification

The final Debug and Release results are:

| Area | Result |
| --- | --- |
| Domain tests | 65 passed |
| Application tests | 93 passed |
| Workbook parity | 2 passed, 2 intentionally approval-gated/skipped |
| Combined | **160 passed, 2 skipped, 0 failed** |
| Debug x64 build | 0 warnings, 0 errors |
| Release online NuGet audit/restore | passed |
| Release publish asset/safety checks | passed |
| Velopack installer/package/portable build | passed |

The Release gate warns that local test artifacts are unsigned. Signing remains
a publication/deployment concern; it did not invalidate this engineering
baseline.

## Native Windows UI verification

`tools/Run-WindowsAppControlAudit.ps1` uses Windows UI Automation and native
window control, not screenshot-only inference. It exercised 24 routes at both
1600 x 940 and 1000 x 800, producing 48 screenshots:

- Home and Dual/Flat/D-shape construction tiles;
- all ten COR workspace subpages;
- Contract review;
- Live Data and Production speeds;
- Braid, Buncher and Coil;
- Reports and Settings.

Final Release result: **48 pages captured, 0 navigation failures**, still
responding after the sweep, with approximately **387.6 MB working set**. The
working-set figure is a high-end-PC snapshot after visiting every route, not a
representative low-spec measurement or a leak conclusion. It is high enough to
justify the requested profiling/optimisation pass.

The interaction harness `tools/Run-WindowsAppInteractionAudit.ps1` then
verified the exact Debug and Release executables:

| Interaction | Result |
| --- | --- |
| Braid ends changed to 10 | filtered to retained `10/0.10` TCW and recalculated |
| Braid LIVE Preview | on and rendered |
| Buncher target `120 mm` | `Large buncher`, gears 57/20 |
| Buncher LIVE Preview | on and rendered |
| Approved flat-coil example | 8 mm bar; 19 full turns; 0.733 m/coil; 806.683 m total |
| COR Interactive 3D | hardware D3D11 rendered successfully |
| Detached 3D preview | one owned preview window and successful redock |
| Forced software path | WARP rendered successfully and redocked |
| Close while detached | **failed** with the confirmed unhandled exception above |

Representative detached measurements on this PC were about 1–4 ms per
hardware redraw and about 0.8–1.8 ms after WARP warm-up. The renderer is
event-driven rather than continuously redrawing while idle, which is the right
performance model. A lower-spec laptop pass is still required.

## Page/readiness observations

### Working development modules

- COR costing workflow and calculation trace;
- Dual Insulation calculation workflow, although its layout still needs to be
  brought onto the current shared COR layout contract;
- Contract review;
- central-data configuration/status;
- production speed library;
- Braid coverage, Buncher lay and Coil planning;
- settings, theme controls and update settings.

### Intentionally incomplete surfaces

- Flat cable and D-shape costing remain construction placeholders;
- Reports is still a placeholder rather than the full printable/reporting
  suite;
- live database integration was not testable with `0 of 5` links configured;
- installed-app update download/apply, save-folder picker, PDF generation and
  physical printing were outside this read-only runtime audit.

### Accessibility/layout findings

- Home construction tiles expose empty automation names; the visible tile text
  is not currently sufficient as a Button automation name.
- Most pages expose one additional unnamed Button, most likely the navigation
  pane toggle. COR exposes more unnamed toggle/button elements.
- ComboBoxes all exposed names and current selections in the final sweep.
- The severe compact Braid header should use the shared responsive header
  arrangement instead of retaining its fixed `*` plus `Auto` columns.

## Recommended next slice

Do not add another large module first. Use this order:

1. Fix the Release logo loader and add a Release-publish branding smoke test.
2. Fix detached-preview shutdown and keep the failing native shutdown test as
   the regression test.
3. Make the module header responsive, applying the correction to Braid,
   Buncher and Dual through one shared component/style.
4. Add explicit automation names to tile buttons, navigation toggle and preview
   controls.
5. Profile startup, route changes, bindings, retained-data projection and
   preview allocation on a representative laptop. Treat 387.6 MB as the
   current high-water baseline to beat.
6. Begin the modularity pass described below, then update Dual to consume it.

## Modularity/context boundary for the next task

The main context hotspots are still too large:

- `MainPage.xaml`: about 5,784 lines;
- `MainPage.xaml.cs`: about 3,601 lines;
- `MainPage.CentralDataImport.cs`: about 1,083 lines;
- `SingleCoreCostingViewModel.cs`: about 3,608 lines;
- `DualInsulationCostingViewModel.cs`: about 1,196 lines;
- the primary handoff: more than 3,200 lines.

The next architecture pass should establish a small module contract containing
module identity/navigation metadata, responsive header content, editor/result
content, optional preview provider and save/report capabilities. Each module
should own its View + ViewModel + application service. `MainPage` should become
an application shell/router, not the implementation location for every
costing page. Shared formula logic stays in Domain/Application; shared visual
layout belongs in focused WinUI controls; module-specific formula and input
state must not move into the shell.

For future tasks, start with this file, `docs/SCOPE.md`, and only the module's
own documentation/source. Read the historical handoff only when an unresolved
decision requires it. This keeps routine task context substantially lower.

## Evidence retained locally

- Final Release page sweep:
  `artifacts/app-control-audit-20260825-v070-release/audit.json`
- Passing Debug interactions:
  `artifacts/app-interaction-audit-20260825-v070-v4/interaction-audit.json`
- Passing Release interactions:
  `artifacts/app-interaction-audit-20260825-v070-release/interaction-audit.json`
- Passing WARP interactions:
  `artifacts/app-interaction-audit-20260825-v070-warp/interaction-audit.json`
- Expected failing detached-shutdown regression:
  `artifacts/app-interaction-audit-20260825-v070-close-detached-v2/interaction-audit.json`

These artifact folders are local test evidence and are not release inputs.
