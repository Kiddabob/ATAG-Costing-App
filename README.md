# Costing App

Costing App is a Windows desktop application for transparent cable costing,
quotations, and contract review. It is replacing a macro-enabled Excel costing
workbook with traceable calculations, retained offline data, and guided cable
construction workflows.

## Download and install

Download the current Windows installer from the
[latest GitHub Release](https://github.com/Kiddabob/ATAG-Costing-App/releases/latest),
or download
[Costing-App-Setup.exe directly](https://github.com/Kiddabob/ATAG-Costing-App/releases/latest/download/Costing-App-Setup.exe).

Run `Costing-App-Setup.exe`. It installs for the current Windows user, creates
Start menu and desktop shortcuts, and does not require administrator rights.

The first release is not code-signed, so Windows may show **Unknown publisher**
or a SmartScreen warning. Check the `SHA256SUMS.txt` file on the Release page if
you want to verify the download before running it.

## First run

A clean installation contains the application only. It does not include ATAG
database links, cached material/customer records, saved costings, settings, or
workbooks.

On first run:

1. Choose the folder used for costings, quotations, reports, and backups.
2. Link and import the five central-data areas: Copper, Compounds,
   Masterbatch, Contacts, and Operators.
3. Review and transform the imported tables before using them in a costing.

Successful imports are retained in that Windows user's LocalAppData. If a
database later becomes unavailable, the last successful tables remain usable
offline and can be refreshed when the connection returns.

## Updates

Installed builds check the public GitHub Releases feed anonymously; users do
not need a GitHub account. Settings provides Stable/Beta selection, manual and
automatic checks, cumulative release notes for every missed version, download
progress, and an explicit **Download and restart** action.

Application updates replace application files only. Per-user settings,
database-link definitions, retained offline tables, and saved business files
remain outside the replaceable application directory.

## Current scope

Version 0.1.0 includes:

- a working single insulated core (COR) costing flow;
- conductor, compound, masterbatch, production-labour, risk, and markup
  calculations with visible calculation traces;
- searchable Access/SQL table linking and transformation with retained offline
  copies;
- costing revisions, customer/core naming, contract review, and A4 quotation
  PDF generation;
- an opt-in scaled cross-section and side-profile preview;
- tested dual-insulation calculation and construction-planning engines.

The complete guided dual-insulation editor is the next development slice.
Flat and D-shape constructions remain future modules. Real ATAG schema and
business acceptance are still required before production rollout.

See [docs/SCOPE.md](docs/SCOPE.md) for the migration boundary and planned work.

## Development setup

This repository targets Windows and currently requires:

- Windows with the Windows 11 SDK `10.0.26100` available;
- .NET SDK `10.0.x` (the verified workspace uses `10.0.302`);
- Visual Studio **2022** (solution format/version 17) for IDE development;
- the x64 platform for the supported installer/release path.

The previous README incorrectly referred to “Visual Studio 2026.” The checked-in
solution is a Visual Studio 2022-format `.sln` and should be opened accordingly.

### Visual Studio

1. Open `ATAG.Costing.sln` in Visual Studio 2022.
2. Select the `x64` solution platform.
3. Set `ATAG.Costing.WinUI` as the startup project.
4. Restore packages, then build or start debugging.

Do not try to run the `.sln` file itself; it is a solution container, not the
application executable.

### Command line

From the repository root:

```powershell
dotnet restore ".\ATAG.Costing.sln" -p:Platform=x64
dotnet build ".\ATAG.Costing.sln" -c Debug -p:Platform=x64 --no-restore
dotnet test ".\ATAG.Costing.sln" -c Debug -p:Platform=x64 --no-build
dotnet run --project ".\src\ATAG.Costing.WinUI\ATAG.Costing.WinUI.csproj" -c Debug -p:Platform=x64 --no-build
```

`dotnet run` must target the WinUI `.csproj`; `dotnet run ATAG.Costing.sln` is
not a valid application launch command.

The development build is unpackaged and self-contained so it can run from a
portable workspace without depending on a fixed drive letter.

## Build a release

The version is set once in `Directory.Build.props`. Release notes for that
version must exist in `CHANGELOG.md`.

After restoring packages, run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\tools\Build-Release.ps1"
```

The script runs the Release tests, publishes the self-contained x64 app, audits
the package for private/business data, and writes the installer, update feed,
portable ZIP, and checksums beneath `artifacts\release`.

Release and updater details are in
[docs/INSTALL-AND-UPDATE.md](docs/INSTALL-AND-UPDATE.md).

## Repository guide

- `src/ATAG.Costing.Domain` — calculation concepts and business rules.
- `src/ATAG.Costing.Application` — use cases, preferences, and orchestration.
- `src/ATAG.Costing.Infrastructure` — persistence and central-data adapters.
- `src/ATAG.Costing.Reporting` — quotation and review document generation.
- `src/ATAG.Costing.WinUI` — WinUI 3 desktop application.
- `tests` — domain, application, and approval-gated workbook parity tests.
- `docs` — calculation evidence, storage, data-link, revision, and release
  contracts.
- `CONTINUE-ATAG-COSTING.md` — current handoff and recommended next slice.

No database path or removable-drive letter should be hard-coded. Linked data
and user state belong in per-user storage, not in Git or release packages.
