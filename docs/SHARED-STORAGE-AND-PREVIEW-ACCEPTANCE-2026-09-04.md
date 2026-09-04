# Shared storage and LIVE Preview acceptance - 4 September 2026

This is the concise continuation record for the work completed after the
25 August test baseline and the 28 August shared-storage handover.
Read this file before reopening the longer historical handovers.

## Release and working-tree boundary

- The installed package updated successfully to `0.8.0`; its product version is
  `0.8.0+f82cf6d27fb475a7c335b3841b8dab6e295b6b3f`.
- The latest public Stable release is `v0.8.0`, published against merge commit
  `f82cf6d27fb475a7c335b3841b8dab6e295b6b3f`.
- USB `main` contains the release and final evidence commits, matches
  `origin/main`, and was clean at final handover.
- The retained 25 August reliability work, 28 August shared-storage work, and
  preview changes described below are committed, pushed, packaged, published,
  and installed.

## ATAG shared application-data acceptance

An actual Debug application launch on this ATAG-detected PC selected
`\\atagdesign\database\ATAG Costing App` and logged **Using ATAG managed
shared application data**. Native Windows UI Automation then verified:

- Live Data loaded `325 copper · 74 compounds · 348 masterbatches · 568
  contacts · 23 operators` from the retained shared snapshot;
- all five database links were configured;
- Production Speeds loaded the shared `Line 1` entry;
- the process remained responsive.

The accepted shared files remain:

- `central-data-state.json`: 7,928,960 bytes, SHA-256
  `3C007DE93E8F3C18D8153AF440655242FD70EA9B8E4ADD1A990C717D00505C46`;
- `production-speed-library.json`: 3,222 bytes, SHA-256
  `6CB19FEF022EC3242C1B035F4834717828FCF18D9FDB99ACCFEC411D14F941FF`.

The existing local copies remain recovery backups. Central-table and
production-library writes are cross-process locked and atomic, and production
lines merge against each client's loaded baseline. Automatic database-link
refresh is once per hour; manual refresh remains immediate. Costing documents,
quotes and reports still use the separate user-selected output folder. UI
preferences, window placement, updater preference and the disposable exchange-
rate cache remain per PC.

The generic first-launch folder chooser is protected by automated policy tests,
but its completely blank/non-ATAG native startup path cannot be reproduced on
this ATAG-detected PC without deliberately altering the account environment.

Evidence: `artifacts/shared-storage-acceptance-20260904.json` and reusable
runner `tools/Run-SharedStorageAcceptance.ps1`.

## Accepted preview slice

Preview drawing remains presentation-only. Domain/Application results are the
authority and no visual calculates or corrects business values.

| Module | Current local preview state |
| --- | --- |
| COR | Existing shared Simple, Detailed and hardware D3D11/WARP Interactive 3D modes; detach and redock retained. |
| Dual insulation | Existing cross-section and side profile retained; redraws are now coalesced on a 50 ms trailing timer and stop while unloaded. |
| Braid Coverage | Existing responsive engineering preview retained and accepted with the ends/wire filter interaction. |
| Buncher Lay | New end and side views use the retained core-layout table. Six cores render as one centre plus five around it, with equal cable cores and no invented central former/sheath. The core group and selected lay both drive the visual. |
| Coil calculator | New bounded vector preview shows the bar, representative complete turns, parallel tails, selected cable shape and authoritative result labels. It consumes `coil-cable-length/v1` results and contains no pricing. |
| Flat and D-shape | Still construction placeholders without calculation inputs, so a real live geometry preview is deferred until those modules exist. |
| Production Speeds | Data-entry/reference module, not a cable-construction geometry preview. |

`CoreLayupPreviewLayoutBuilder` is the pure shared layout boundary for retained
Buncher core groups. Buncher, Coil and Dual redraw only after a short trailing
delay so rapid edits do not force one full render per property notification.
COR remains the only accelerated 3D consumer; extending one reusable 3D scene
contract to the other completed calculation modules remains later work after
lower-spec measurement and the confirmed preview-window shutdown defect.

## Verification

- Domain: 65 passed.
- Application: 104 passed.
- Workbook parity: 2 passed and 2 intentionally approval-gated skips.
- Total: **171 passed, 2 skipped, 0 failed**.
- x64 Debug WinUI build: **0 warnings, 0 errors**.
- Native interaction audit: **6 passed, 0 failed** for Dual, Braid, Buncher,
  Coil, COR Interactive 3D and COR detach/redock.
- The final exact executable remained responsive with a 384,425,984-byte
  working set after the interaction audit.
- The online-audited v0.8.0 Release gate repeated all 171 passing tests and 2
  intentional skips, verified the embedded icon and packaged assets, completed
  the public-data safety audit, and created every Velopack payload.
- The exact Release-published executable then completed a second 48-route
  wide/compact native audit with zero navigation failures. Both long ATAG logos
  were visibly present and the new startup-log segment contained no branding
  load failure.

Evidence: `artifacts/app-interaction-audit-20260904-live-previews-v5` and
`tools/Run-WindowsAppInteractionAudit.ps1`.

During an earlier 46-page visual capture, Windows bitmap-resource pressure
caused two screenshot-allocation failures. The installed v0.7.0 process, which
still contains the old refresh path, also logged an `OutOfMemoryException`
while reading the 7.9 MB retained JSON snapshot under that pressure. The local
`JsonCentralDataStore` now streams serialization and deserialization instead of
allocating one complete JSON string. This is an optimisation and reliability
fix in the unreleased tree, not evidence that installed v0.7.0 was updated.

## Release-candidate follow-up

The three confirmed 25 August defects were corrected during v0.8.0 preparation:
long logos use packaged resource URIs, detached shutdown releases its retained
`AppWindow` event source safely, and Braid/Buncher headers use a compact-safe
stacked layout. Forced-WARP detached shutdown passed 3/3 native checks with no
unhandled exception. The subsequent wide/compact audit captured 48 routes with
zero navigation failures and no narrow Braid/Buncher header text.

The generic/non-ATAG folder-selection path still needs native acceptance on a
suitable PC. Local v0.8.0 package evidence is:

- `Costing-App-Setup.exe`: 101,357,406 bytes, SHA-256
  `C8DAA505E4C1E609691C2DD2789BF0A9BD7086FD9CAF816400BE7FBE50224CB3`;
- `Costing.App-0.8.0-full.nupkg`: 96,764,254 bytes, SHA-256
  `69494CD5D22E47FE15BA9A9EC689349AF722100094062607BEEC1F62294F2907`;
- `Costing.App-win-Portable.zip`: 96,709,096 bytes, SHA-256
  `A7643DA223B7BFB6E742087086B3A0884F1828457898483FC2F97030F69A0C65`.

GitHub merge, Actions and public updater evidence must be appended after they
complete. The packages are not code-signed; this is unchanged from v0.7.0.

## Public v0.8.0 release evidence

- Pull request: `#4`, merged into `main`.
- Merge/tag target: `f82cf6d27fb475a7c335b3841b8dab6e295b6b3f`.
- Stable workflow: Actions run `33864222647` / run number `19`, successful in
  3m 3s.
- Public release: `v0.8.0`, latest, not draft, not prerelease, published
  2026-09-04 10:41:44 UTC.
- Public release assets: 7, including installer, full NuGet package, portable
  ZIP, both Velopack feeds, `RELEASES`, and `SHA256SUMS.txt`.
- Public installer: 101,411,942 bytes, SHA-256
  `427AE5E128093CED212664566E37F58874123220E9236E2B61776BF9DC2D85A4`.
- Public full package: 96,818,790 bytes, SHA-256
  `CF488042989F2694D0D20966FA7291982C54913F388C0ED6A915A8D9C604192F`.
- Public portable ZIP: 96,763,612 bytes, SHA-256
  `63E17F423143B70BB2AEFCA6086F355BC45BFCFFB8644E9ABBE5FE5D8BFD48DB`.
- Anonymous updater check: `releases.win.json` advertises `0.8.0`,
  `Costing.App-0.8.0-full.nupkg`, and the same full-package SHA-256.

The small size/hash differences between local and public payloads are expected
because GitHub Actions rebuilt the packages rather than uploading the local
build. The public hashes above are authoritative for users. The workflow's only
annotation was a non-blocking Node.js-runtime deprecation warning for the v4
GitHub Actions dependencies; the build, tests, packaging and publication all
completed successfully.
