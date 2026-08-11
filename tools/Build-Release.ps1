[CmdletBinding()]
param(
    [string]$OutputRoot,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $root 'artifacts\release'
}

$outputRootFull = [System.IO.Path]::GetFullPath($OutputRoot)
$allowedArtifactRoot = [System.IO.Path]::GetFullPath((Join-Path $root 'artifacts'))
if (-not $outputRootFull.StartsWith($allowedArtifactRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Release output must stay beneath $allowedArtifactRoot"
}

[xml]$buildProps = Get-Content (Join-Path $root 'Directory.Build.props')
$version = [string]($buildProps.Project.PropertyGroup.CostingAppVersion | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'CostingAppVersion was not found in Directory.Build.props.'
}

$solution = Join-Path $root 'ATAG.Costing.sln'
$project = Join-Path $root 'src\ATAG.Costing.WinUI\ATAG.Costing.WinUI.csproj'
$publishDir = Join-Path $outputRootFull 'publish'
$releaseDir = Join-Path $outputRootFull 'releases'
$changelog = Join-Path $root 'CHANGELOG.md'
$releaseNotes = Join-Path $outputRootFull 'release-notes.md'
$icon = Join-Path $root 'src\ATAG.Costing.WinUI\Assets\AppIcon.ico'

if (Test-Path $outputRootFull) {
    Remove-Item -LiteralPath $outputRootFull -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

$changelogText = Get-Content -LiteralPath $changelog -Raw
$escapedVersion = [Regex]::Escape($version)
$releasePattern = "(?ms)^##\s+$escapedVersion\b.*?(?=^##\s+|\z)"
$releaseMatch = [Regex]::Match($changelogText, $releasePattern)
if (-not $releaseMatch.Success) {
    throw "CHANGELOG.md does not contain release notes for version $version."
}
Set-Content -LiteralPath $releaseNotes `
    -Value $releaseMatch.Value.Trim() `
    -Encoding UTF8

Push-Location $root
try {
    if (-not $SkipTests) {
        dotnet test $solution -c Release -p:Platform=x64 --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Release tests failed.' }
    }

    dotnet publish $project `
        -c Release `
        -r win-x64 `
        -p:Platform=x64 `
        -p:PublishTrimmed=false `
        -p:PublishReadyToRun=false `
        -p:DebugSymbols=false `
        -p:DebugType=None `
        --self-contained true `
        --no-restore `
        -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw 'Release publish failed.' }

    $requiredPublishedAssets = @(
        'Assets\AppIcon.ico',
        'Assets\Organisation\ATAGDesignLongLogoDarkText.png',
        'Assets\Organisation\ATAGDesignLongLogoLightText.png'
    )
    foreach ($relativeAssetPath in $requiredPublishedAssets) {
        $sourceAsset = Join-Path (Split-Path -Parent $project) $relativeAssetPath
        $publishedAsset = Join-Path $publishDir $relativeAssetPath
        if (-not (Test-Path -LiteralPath $publishedAsset -PathType Leaf)) {
            throw "Release publish is missing required app asset: $relativeAssetPath"
        }

        $sourceHash = (Get-FileHash -LiteralPath $sourceAsset -Algorithm SHA256).Hash
        $publishedHash = (Get-FileHash -LiteralPath $publishedAsset -Algorithm SHA256).Hash
        if (-not $sourceHash.Equals($publishedHash, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Published app asset does not match its source: $relativeAssetPath"
        }
    }

    Add-Type -AssemblyName System.Drawing
    $publishedExecutable = Join-Path $publishDir 'ATAG.Costing.WinUI.exe'
    $publishedIcon = [System.Drawing.Icon]::ExtractAssociatedIcon(
        $publishedExecutable)
    if ($null -eq $publishedIcon) {
        throw 'The published executable does not contain an application icon.'
    }

    $sourceIcon = New-Object System.Drawing.Icon -ArgumentList @(
        $icon,
        $publishedIcon.Size)
    $publishedBitmap = $publishedIcon.ToBitmap()
    $sourceBitmap = $sourceIcon.ToBitmap()
    try {
        $embeddedIconMatches =
            $publishedBitmap.Width -eq $sourceBitmap.Width -and
            $publishedBitmap.Height -eq $sourceBitmap.Height
        for ($x = 0; $embeddedIconMatches -and $x -lt $publishedBitmap.Width; $x++) {
            for ($y = 0; $y -lt $publishedBitmap.Height; $y++) {
                if ($publishedBitmap.GetPixel($x, $y).ToArgb() -ne
                    $sourceBitmap.GetPixel($x, $y).ToArgb()) {
                    $embeddedIconMatches = $false
                    break
                }
            }
        }

        if (-not $embeddedIconMatches) {
            throw 'The published executable icon does not match Assets\AppIcon.ico.'
        }
    }
    finally {
        $sourceBitmap.Dispose()
        $publishedBitmap.Dispose()
        $sourceIcon.Dispose()
        $publishedIcon.Dispose()
    }

    $blockedExtensions = @(
        '.accdb', '.mdb', '.mdf', '.ldf', '.bak', '.xls', '.xlsx', '.xlsm',
        '.atagcosting', '.pdb'
    )
    $blockedNames = @(
        'central-data-state.json', 'central-data-snapshot.json', 'settings.json',
        'window-placement.json', 'production-speed-library.json'
    )
    $unsafeFiles = Get-ChildItem -LiteralPath $publishDir -Recurse -File | Where-Object {
        $blockedExtensions -contains $_.Extension.ToLowerInvariant() -or
        $blockedNames -contains $_.Name.ToLowerInvariant() -or
        $_.Name -like '.env*'
    }
    if ($unsafeFiles) {
        $unsafeList = ($unsafeFiles.FullName -join [Environment]::NewLine)
        throw "Release safety audit found blocked files:$([Environment]::NewLine)$unsafeList"
    }

    dotnet tool restore --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { throw 'The pinned vpk tool could not be restored.' }

    dotnet tool run vpk -- pack `
        --packId Costing.App `
        --packVersion $version `
        --packDir $publishDir `
        --mainExe ATAG.Costing.WinUI.exe `
        --packTitle 'Costing App' `
        --packAuthors 'Costing App' `
        --outputDir $releaseDir `
        --icon $icon `
        --releaseNotes $releaseNotes
    if ($LASTEXITCODE -ne 0) { throw 'Velopack packaging failed.' }

    $setup = Get-ChildItem -LiteralPath $releaseDir -Filter '*Setup.exe' -File |
        Select-Object -First 1
    if (-not $setup) { throw 'Velopack did not create Setup.exe.' }
    $friendlySetup = Join-Path $releaseDir 'Costing-App-Setup.exe'
    if (-not $setup.FullName.Equals($friendlySetup, [StringComparison]::OrdinalIgnoreCase)) {
        Move-Item -LiteralPath $setup.FullName -Destination $friendlySetup
    }

    $checksumLines = Get-ChildItem -LiteralPath $releaseDir -File |
        Sort-Object Name |
        ForEach-Object {
            $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            "$hash  $($_.Name)"
        }
    Set-Content -LiteralPath (Join-Path $releaseDir 'SHA256SUMS.txt') `
        -Value $checksumLines `
        -Encoding Ascii

    Write-Host "Costing App $version release created."
    Write-Host "Installer: $friendlySetup"
}
finally {
    Pop-Location
}
