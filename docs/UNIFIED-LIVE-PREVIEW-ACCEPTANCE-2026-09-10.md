# Unified LIVE Preview and dot print — 10 September 2026

## Start here

Continue the existing `main` checkout. The user-reviewed implementation is
commit `6861735f875c08cb3e66bbb7633bac1a01f2ca90`, merged through PR #5 as
`25d3346a51423cb53c46e9563bbb4006395991be`. Local audited packaging passed for
v0.9.0. Stable workflow run `34492385967` / #20 completed successfully and
published v0.9.0. The installed app was not replaced in this pass; the ordinary
Stable updater remains the installation route. Public evidence is below.

Use relative paths from the checked-out repository; the USB drive letter is
not part of the implementation. Read `UNIFIED-LIVE-PREVIEW.md` for the modular
contract/code map instead of loading the entire historical handover.

## Implemented

- Shared Simple 2D and Detailed 2D, both cross-section and side, plus orbital
  3D for COR, Dual Insulation, Braid Coverage, Buncher Lay and Coil.
- One immutable physical scene and bounded geometry engine, no duplicated
  costing formulas. Shape-aware round/flat/D sections; equal cable-core lay,
  alternating braid crossings, tape/foil wrap helper and coil/bar/tail paths.
- Shared source debounce, worker-thread geometry, stale-result cancellation,
  mode cache, explicit inactive-page suspension and one transferable pop-out.
  Native SVG batches 2D rather than one XAML element per strand. On-demand
  Direct3D frames support hardware first and explicit/fallback WARP.
- Print merged into the COR cable scene, with exactly two full impressions.
  Dot diameter, dots high and both centre pitches are explicit; preview ink
  height is calculated and compared against retained legacy requested height.
  Quoted length and material calculations are unaffected. Fit/Inspect surface
  complement orbit, pan and zoom. New settings save/reopen; absent legacy
  fields take documented defaults without overwriting earlier pitch values.
- Original per-page COR drawing/print-strip code was removed. Other legacy
  preview classes remain unused and can be retired in a separate cleanup.

## Automated verification

Final pure regression: **228 passed, 2 intentional skips, 0 failed**:

- Application: 161 passed, including full print/save compatibility, dot-budget
  boundaries, geometry budgets, all supported core groups, adapter validity,
  signed print placement, coil tail orientation and braid crossing regressions.
- Domain: 65 passed.
- Workbook parity: 2 passed, 2 existing approval-gated fixture skips.

Commands: `dotnet test tests/<test-project>/<test-project>.csproj --no-restore
--verbosity minimal` for the three existing test projects. Routine workstation
builds retain the durable offline NuGet policy. The publication pass also ran
`tools/Build-Release.ps1` with online NuGet audit and NU1900–NU1904 treated as
errors. It passed the same 228 tests/2 skips, self-contained Release publish,
icon/logo/shader checks, private-file gate and Velopack packaging for 0.9.0.
Local evidence is in `artifacts/release-0.9.0-20260910` (ignored, not source).
All package/feed versions, sizes and SHA-1/SHA-256 values agree. A packaging-only
fix keeps `assets.win.json` consistent with the friendly installer rename;
all asset references and all six checksums were verified after that correction.
GitHub will repeat the complete release gate from the committed source.

## Native checks

Windows Computer Use targeted only the exact project Debug executable. Debug
opt-in `ATAG_COSTING_PREVIEW_FIXTURES=1` supplies in-memory physical dimensions,
two sample prints and coil tails/strips; it never saves projects or retained
business tables. `ATAG_COSTING_3D_SMOKE_TEST=1` opens COR 3D. Neither hook is
enabled in normal launches and the fixture implementation is Debug-only.

First native pass verified hardware COR 3D, readable `ATAG CORE 01` dot ink
through Inspect surface, WARP rendering of the same scene, Simple 2D drawing,
pop-out and close/redock, and clean application shutdown. Diagnostic frame
labels showed approximately 2.5–5.1 ms hardware and 8 ms WARP at 1024×288;
these are individual frame samples, not sustained performance guarantees.

The pass caught oversized 2D images in a wide pop-out and visibly polygonal
simple circles. Image height is now capped at 220 logical pixels and
cross-sections use 48/64 circle segments; mesh/side budgets are unchanged.
Pop-out initialization/activation failure now restores the docked presentation
and reports a warning rather than losing the preview or throwing from the
button handler.

A second pass verified Braid 3D and the six-core Buncher cross/side views, then
caught two shared usability defects: a compact COR dock could allocate all
height to fixed guidance, and window transfer could release a renderer after
reattachment. COR now keeps its controls fixed above one scrollable preview/
guidance area, with a usable Off-state height. Loaded/Unloaded events reconcile
against the settled XAML root to avoid destroying a newly attached renderer.
The Buncher group selector formerly embedded in its old preview is retained
above the unified control; replacing presentation must not remove module inputs.

Final build: `dotnet build src/ATAG.Costing.WinUI/ATAG.Costing.WinUI.csproj
-c Debug -p:Platform=x64 --no-restore` passed, **0 warnings, 0 errors**,
Latest incremental build: 1m 17.29s. It includes the circle/height polish,
tail/print/crossing fixes, pop-out recovery, compact dock, settled-root lifecycle
and restored Buncher selector.

Final native follow-up on that build (10 September, closed 14:45:58 BST):

- COR compact controls remain visible at 1306×853; Off leaves scrollable wall
  guidance available. Full-width Off guidance also remains available.
- COR hardware 3D detaches immediately without toggling WARP; Inspect surface
  shows the first complete print and wheel zoom changes its framing. Closing
  the pop-out redocks and recreates its surface automatically.
- Flat coil: supplied 2.5×4.8 mm section, 10 mm coil OD and 48 mm axial length
  produce the expected labelled 5 mm bar and ten turns. Simple cross-section,
  hardware 3D, detached rendering, pointer orbit, tails and separate strip
  allowances were visually checked. No recovery toggle was needed.
- Dual: valid physical dimensions still produce Simple/Detailed cross-section
  and cutaway side views when the separate pricing calculation lacks material
  density. Two black insulation selections remain black; no invented colours
  or density values were added to make the test pass.
- Owner shutdown while the Dual 2D view was detached exited cleanly. The final
  diagnostic process (PID 11892) is closed; startup log contains no unhandled
  exception or renderer failure in this run.

This is representative native coverage, not a completed all-mode/device
matrix. Braid's initial transfer fault was recovered with WARP before the
shared fix; the corrected transfer was then checked on COR and Coil. The
publication pass cold-launched the exact 0.9.0 Release publish with diagnostic
fixtures disabled, verified Braid Simple 2D and hardware 3D, then confirmed
the exact Braid pop-out case renders without a recovery toggle (1024×288,
one 3.20 ms hardware frame). All Buncher selector choices, Dual/Buncher 3D
and round/D coils remain part of the outstanding broader native matrix.
The long-logo spaces appeared blank in this publish-folder cold launch even
though branding was detected. Assets/hashes, loading code and containers are
unchanged from v0.8.0; installed-runtime image loading needs follow-up, and
the detection log alone must not be treated as proof of visible logo pixels.
Screenshots were reviewed directly through
Windows Computer Use; no PowerShell UI Automation audit was mixed into this
turn. The local startup log is `%TEMP%/ATAG-Costing-startup.log` (diagnostic,
not a portable release artifact).

## Pure geometry timings

Five warmed runs, median milliseconds on this development PC; the harness
used the current Debug Application assembly and a Release console runner:

| Scene | 3D mesh | Detailed side |
| --- | ---: | ---: |
| 24 carriers × 10 ends, 45 cable cores | 89 | 122 |
| 32-turn flat coil | 3 | 6 |
| Typical two-print, 133-strand cable | 4 | 2 |
| Synthetic 20,000-dot print | 60 | 38 |

Largest braid: 189,930 vertices, 1,095,120 indices; detailed side 14,925 paths
and 148,972 points. It allocates approximately 47–51 MB per generation. These
bounded worker timings exclude SVG decoding, GPU upload and native UI costs;
do not claim older-laptop performance from them. The ignored diagnostic
harness is under `artifacts/unified-geometry-benchmark` and is not a shipping
dependency. Profile worst-case inputs on target laptop hardware next.

## Explicit limits / next acceptance

1. The retained 26-core row says `3-9-15` (27 cores). The preview reports this
   inconsistency rather than silently dropping a core. Confirm/correct the
   approved reference source before enabling that row. Other 44 groups pass
   equal-core/count/non-overlap checks, including six = one centre plus five.
2. Dual's optional tape/chalk/foil/screen/drain flags lack physical layer data.
   Missing dimensions are visibly explained. Add approved modular layer inputs
   before drawing those options. Full flat/D costing pages remain placeholders.
3. Buncher has no core-OD input; its 2 mm diameter is visibly illustrative.
   Conductor detail above 384 strands is a labelled envelope; coils above
   32 turns show a labelled representative length, retaining exact results.
4. The authored reference bitmap font is not guaranteed to match the inkjet
   machine. Unsupported glyphs are reported, not substituted in saved text.
   Maximum two-print dot count is 20,000. Invalid print settings leave the
   cable available with an explanation. Verify actual printer settings/font.
5. Cross-page pin/follow, saved multi-monitor placement and a fully refreshed
   Dual costing layout remain separate work. The new pop-out remembers only
   in-session placement and clamps to the current available display. Native
   compact Dual still lets its shell guidance crowd the preview header; port
   the fixed-header/scrollable-guidance treatment to `ModuleWorkspaceShell`
   and check all consumers together. The COR-specific compact fix is accepted.
6. The full compact/wide/theme/accessibility matrix and genuine low-end GPU/
   older-laptop acceptance remain outstanding. Hardware-failure simulation,
   long-duration stress and translucent materials were not validated here.
7. Publication completed for v0.9.0 through the reviewed PR and existing Stable
   workflow. Local and GitHub online-audited Release/package/public-data gates
   passed. Installed update/restart and visible installed-logo acceptance were
   not performed in this publication-only pass.

Shared ATAG storage, generic-user chooser and the one-hour relink policy were
not changed. No source workbook, live database or saved costing was edited.

## Public v0.9.0 release evidence

- PR: [#5](https://github.com/Kiddabob/ATAG-Costing-App/pull/5), merged to main.
- Merge/tag target: `25d3346a51423cb53c46e9563bbb4006395991be`.
- [Stable Actions run #20](https://github.com/Kiddabob/ATAG-Costing-App/actions/runs/34492385967)
  completed successfully, including audited build/test/package, publication
  and retained installer evidence. Run timestamps: 14:56:43–15:01:03 UTC.
- [Public v0.9.0](https://github.com/Kiddabob/ATAG-Costing-App/releases/tag/v0.9.0)
  is latest Stable, not draft or prerelease, published 10 September 2026 at
  15:00:47 UTC / 16:00:47 BST, with seven release assets.
- Installer: 101,520,721 bytes; SHA-256
  `2ef7bdf602815eec8063ee133127b4b3c1013fe7e1d4297c92ddad4402f5d412`.
- `Costing.App-0.9.0-full.nupkg`: 96,927,569 bytes; SHA-256
  `23864f23e6d56e1cb110540faa5a34aa9367cebc3205d312c75694f064135f01`.
- Portable ZIP: 96,872,589 bytes; SHA-256
  `0eff2d5eab1b2cb746f14c6aa1cfd393720a1df2b898f6bd714e76d40ddceb69`.
- Four small metadata files were downloaded anonymously and their sizes/
  SHA-256 checked against GitHub asset digests. All six checksum entries agree
  with the public payload digests. The updater feed advertises 0.9.0 Full,
  its exact filename/size/SHA-256; legacy RELEASES agrees on SHA-1 metadata.
  `assets.win.json` now references the actual `Costing-App-Setup.exe`.
  Binary payloads were not re-downloaded; this is metadata/digest verification,
  not an installed update/restart test. CI rebuilt the payloads, so public
  hashes above differ from local package hashes and are authoritative.

Next development is Dual Layer calculating end to end, then Flat Cable. Inspect
the existing workbook core-count sheets and partial work first; their progress
is a user report, not a completed workbook audit. Reuse existing shared rules,
storage, shell and preview contracts. No Dual/Flat implementation started here.

The local publish-folder test process was left running/minimized after Windows
App Control detected user interaction; it was not forcibly stopped. Before
USB ejection, close any app launched from the USB and verify no such process
remains. No installed application or saved costing was replaced during release.
