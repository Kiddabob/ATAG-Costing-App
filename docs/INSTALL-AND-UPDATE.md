# Install and update contract

## User installation

`Costing-App-Setup.exe` is the normal user-facing installer. It installs for
the current Windows user under LocalAppData, creates Start menu and desktop
shortcuts, adds an uninstall entry, and opens Costing App when installation
finishes. It does not require administrator rights.

The first release is not code-signed. Windows can therefore show **Unknown
publisher** or a Microsoft Defender SmartScreen warning. Signing can be added
to the same packaging process when a trusted organisation certificate becomes
available; a OneDrive or Microsoft 365 login is not a signing certificate.

## Clean package boundary

The installer contains application binaries and static interface assets only.
It must not contain:

- Access, SQL Server, workbook, backup, or environment files;
- retained central-data JSON or database connection details;
- settings, window placement, production-speed-library rows and machine
  settings, customer/operator/material rows, saved costings, quotation PDFs,
  or other user documents;
- developer symbols or machine-local source paths.

First run asks the user to choose business-file storage and import the five LIVE
data areas. Runtime state stays under `%LOCALAPPDATA%\ATAG Design Ltd\ATAG
Costing` and business files stay in the user-selected folder. Both locations
are outside Velopack's replaceable `current` directory and must survive install,
update, repair, and uninstall unless the user deliberately removes them.

## Update behaviour

Settings shows the installed version, automatic-check toggle, Stable/Beta
choice, the notes for every applicable release after the installed version,
download progress, and explicit download/restart action.
The cumulative changelog is presented as separate version cards inside its own
bounded scroll area, so long notes cannot move the update buttons off screen.
It can also open in an owned, resizable full-screen WinUI reader on the same
display as the main app.
The client uses the public GitHub repository without a token, so users do not
need a GitHub account. Stable ignores GitHub pre-releases; Beta may also offer
them. Package size and SHA-256 are supplied by the Velopack release feed and the
download is verified before it is applied.

An update failure never blocks launch and leaves the current installed version
unchanged. The app checks only after its main window is visible. Installation
is explicit because an update restarts the app and users may need to save a
working costing first.

## Maintainer release flow

1. Change `CostingAppVersion` in `Directory.Build.props` and update
   `CHANGELOG.md`.
2. Run `tools/Build-Release.ps1` on Windows, or run the **Build and publish
   release** GitHub Actions workflow.
3. Confirm the safety audit, tests, installer launch, and update behaviour from
   an installed older version.
4. Publish every file from `artifacts/release/releases`; users only need the
   `Costing-App-Setup.exe` asset.

The GitHub workflow creates the public release and uploads the installer,
Velopack package/feed files, and `SHA256SUMS.txt`. Do not publish a release from
a working tree that contains cached business data.
