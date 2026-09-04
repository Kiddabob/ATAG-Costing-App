[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ExecutablePath,

    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputPath = Join-Path $artifactRoot "shared-storage-acceptance-$stamp.json"
}

$executable = [System.IO.Path]::GetFullPath($ExecutablePath)
$output = [System.IO.Path]::GetFullPath($OutputPath)
if (-not $output.StartsWith(
    $artifactRoot + [System.IO.Path]::DirectorySeparatorChar,
    [StringComparison]::OrdinalIgnoreCase)) {
    throw "Acceptance output must stay beneath $artifactRoot"
}
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Application executable was not found: $executable"
}
if (Test-Path -LiteralPath $output) {
    throw "Acceptance output already exists: $output"
}

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Find-AllByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name)
    return ,$Root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        $condition)
}

function Select-NavigationItem {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $matches = Find-AllByName -Root $Root -Name $Name
    for ($index = 0; $index -lt $matches.Count; $index++) {
        $candidate = $matches.Item($index)
        if ($candidate.Current.ControlType -ne
            [System.Windows.Automation.ControlType]::ListItem) {
            continue
        }

        $selection = [System.Windows.Automation.SelectionItemPattern](
            $candidate.GetCurrentPattern(
                [System.Windows.Automation.SelectionItemPattern]::Pattern))
        $selection.Select()
        Start-Sleep -Milliseconds 850
        return
    }

    throw "Navigation item was not available: $Name"
}

function Get-VisibleNames {
    param([System.Windows.Automation.AutomationElement]$Root)

    $all = $Root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)
    $names = [System.Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt $all.Count; $index++) {
        $candidate = $all.Item($index)
        if ($candidate.Current.IsOffscreen -or
            [string]::IsNullOrWhiteSpace($candidate.Current.Name)) {
            continue
        }
        if (-not $names.Contains($candidate.Current.Name)) {
            $names.Add($candidate.Current.Name)
        }
    }
    return ,$names.ToArray()
}

$originalSkip = $env:ATAG_COSTING_SKIP_LAUNCH_MODE_CHOICE
$process = $null
try {
    $env:ATAG_COSTING_SKIP_LAUNCH_MODE_CHOICE = '1'
    $process = Start-Process -FilePath $executable -PassThru
    for ($attempt = 0; $attempt -lt 80; $attempt++) {
        Start-Sleep -Milliseconds 125
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowHandle -ne 0) {
            break
        }
    }
    if ($process.HasExited -or $process.MainWindowHandle -eq 0) {
        throw 'The application did not expose a main window.'
    }

    $root = [System.Windows.Automation.AutomationElement]::FromHandle(
        $process.MainWindowHandle)
    if ($null -eq $root) {
        throw 'Windows UI Automation could not attach to the main window.'
    }

    Select-NavigationItem -Root $root -Name 'Live Data'
    $liveDataNames = Get-VisibleNames -Root $root
    $snapshotSummary = $liveDataNames | Where-Object {
        $_ -match '^\d+ copper · \d+ compounds · \d+ masterbatches'
    } | Select-Object -First 1
    $linkSummary = $liveDataNames | Where-Object {
        $_ -match '^5 database table link\(s\) configured\.'
    } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($snapshotSummary) -or
        [string]::IsNullOrWhiteSpace($linkSummary)) {
        throw 'The shared retained central-data snapshot was not visible.'
    }

    Select-NavigationItem -Root $root -Name 'Production speeds'
    $productionNames = Get-VisibleNames -Root $root
    if (-not ($productionNames -contains 'Production data library') -or
        -not ($productionNames -contains
            'Production lines and known-run evidence use the configured application-data location.')) {
        throw 'The shared production-library status was not visible.'
    }
    $selectedLine = $null
    $lineMatches = Find-AllByName -Root $root -Name 'Selected production line'
    for ($index = 0; $index -lt $lineMatches.Count; $index++) {
        $candidate = $lineMatches.Item($index)
        if ($candidate.Current.ControlType -ne
            [System.Windows.Automation.ControlType]::ComboBox) {
            continue
        }
        $selection = [System.Windows.Automation.SelectionPattern](
            $candidate.GetCurrentPattern(
                [System.Windows.Automation.SelectionPattern]::Pattern))
        $selectedLine = (($selection.Current.GetSelection() |
            ForEach-Object { $_.Current.Name }) -join ', ')
        break
    }
    if ([string]::IsNullOrWhiteSpace($selectedLine)) {
        throw 'No shared production line was selected.'
    }

    $process.Refresh()
    $result = [ordered]@{
        TestedAt = [DateTimeOffset]::Now
        Executable = $executable
        ProductName = $process.MainWindowTitle
        Responding = $process.Responding
        SharedSnapshot = $snapshotSummary
        SharedLinks = $linkSummary
        SelectedProductionLine = $selectedLine
        WorkingSetBytes = $process.WorkingSet64
        Passed = $true
    }
    $directory = Split-Path -Parent $output
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $output
    $result | ConvertTo-Json -Depth 5
}
finally {
    if ($null -eq $originalSkip) {
        Remove-Item Env:ATAG_COSTING_SKIP_LAUNCH_MODE_CHOICE -ErrorAction SilentlyContinue
    }
    else {
        $env:ATAG_COSTING_SKIP_LAUNCH_MODE_CHOICE = $originalSkip
    }

    if ($null -ne $process -and -not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(5000)) {
            $process.Kill()
            $process.WaitForExit()
        }
    }
}
