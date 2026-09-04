[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ExecutablePath,

    [string]$OutputRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifactRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repoRoot 'artifacts'))
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputRoot = Join-Path $artifactRoot "app-control-audit-$stamp"
}

$executable = [System.IO.Path]::GetFullPath($ExecutablePath)
$output = [System.IO.Path]::GetFullPath($OutputRoot)
$artifactPrefix = $artifactRoot + [System.IO.Path]::DirectorySeparatorChar
if (-not $output.StartsWith(
    $artifactPrefix,
    [StringComparison]::OrdinalIgnoreCase)) {
    throw "Audit output must stay beneath $artifactRoot"
}
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Application executable was not found: $executable"
}
if (Test-Path -LiteralPath $output) {
    throw "Audit output already exists: $output"
}

New-Item -ItemType Directory -Path $output | Out-Null
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class AtagNativeAppControl
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(
        IntPtr hWnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam);
}
'@

function Get-AppRoot {
    param([IntPtr]$Handle)

    $root = [System.Windows.Automation.AutomationElement]::FromHandle($Handle)
    if ($null -eq $root) {
        throw "Windows UI Automation could not attach to HWND $Handle"
    }
    return $root
}

function Find-AllByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name)
    $matches = $Root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        $condition)
    return ,$matches
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
        Start-Sleep -Milliseconds 650
        return
    }

    throw "Navigation item was not available: $Name"
}

function Invoke-ContainingButton {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$ChildName
    )

    $matches = Find-AllByName -Root $Root -Name $ChildName
    if ($matches.Count -eq 0) {
        throw "Button content was not available: $ChildName"
    }

    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $candidate = $matches.Item(0)
    while ($null -ne $candidate) {
        if ($candidate.Current.ControlType -eq
            [System.Windows.Automation.ControlType]::Button) {
            $invoke = [System.Windows.Automation.InvokePattern](
                $candidate.GetCurrentPattern(
                    [System.Windows.Automation.InvokePattern]::Pattern))
            $invoke.Invoke()
            Start-Sleep -Milliseconds 800
            return
        }
        $candidate = $walker.GetParent($candidate)
    }

    throw "No invokable parent button was found for: $ChildName"
}

function Save-WindowCapture {
    param(
        [IntPtr]$Handle,
        [string]$Path
    )

    [AtagNativeAppControl]::SetForegroundWindow($Handle) | Out-Null
    Start-Sleep -Milliseconds 180
    $rect = [AtagNativeAppControl+RECT]::new()
    if (-not [AtagNativeAppControl]::GetWindowRect($Handle, [ref]$rect)) {
        throw "GetWindowRect failed for HWND $Handle"
    }

    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $bitmap = [System.Drawing.Bitmap]::new($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen(
            $rect.Left,
            $rect.Top,
            0,
            0,
            $bitmap.Size)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Get-PageEvidence {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Viewport,
        [string]$Page,
        [string]$Screenshot
    )

    $all = $Root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)
    $visibleNames = [System.Collections.Generic.List[string]]::new()
    $emptyButtons = 0
    $emptyComboBoxes = 0
    $emptySelections = [System.Collections.Generic.List[string]]::new()
    $narrowLongText = [System.Collections.Generic.List[string]]::new()
    $toggles = [System.Collections.Generic.List[object]]::new()
    $comboBoxes = [System.Collections.Generic.List[object]]::new()

    for ($index = 0; $index -lt $all.Count; $index++) {
        $element = $all.Item($index)
        if ($element.Current.IsOffscreen) {
            continue
        }

        $name = $element.Current.Name
        $type = $element.Current.ControlType
        $bounds = $element.Current.BoundingRectangle
        if (-not [string]::IsNullOrWhiteSpace($name) -and
            -not $visibleNames.Contains($name)) {
            $visibleNames.Add($name)
        }

        if ($type -eq [System.Windows.Automation.ControlType]::Button -and
            [string]::IsNullOrWhiteSpace($name)) {
            $emptyButtons++
        }
        if ($type -eq [System.Windows.Automation.ControlType]::ComboBox) {
            if ([string]::IsNullOrWhiteSpace($name)) {
                $emptyComboBoxes++
            }
            $selected = ''
            try {
                $selection = [System.Windows.Automation.SelectionPattern](
                    $element.GetCurrentPattern(
                        [System.Windows.Automation.SelectionPattern]::Pattern))
                $selected = ($selection.Current.GetSelection() |
                    ForEach-Object { $_.Current.Name }) -join ', '
            }
            catch {
                $selected = '<selection pattern unavailable>'
            }
            $comboBoxes.Add([pscustomobject]@{
                Name = $name
                Selection = $selected
            })
            if ([string]::IsNullOrWhiteSpace($selected)) {
                $emptySelections.Add($name)
            }
        }
        if ($type -eq [System.Windows.Automation.ControlType]::Button) {
            try {
                $toggle = [System.Windows.Automation.TogglePattern](
                    $element.GetCurrentPattern(
                        [System.Windows.Automation.TogglePattern]::Pattern))
                $toggles.Add([pscustomobject]@{
                    Name = $name
                    State = $toggle.Current.ToggleState.ToString()
                })
            }
            catch {
                # Normal buttons do not expose TogglePattern.
            }
        }
        if ($type -eq [System.Windows.Automation.ControlType]::Text -and
            $name.Length -ge 30 -and
            $bounds.Width -gt 0 -and
            $bounds.Width -lt 125 -and
            $bounds.Height -gt 38) {
            $narrowLongText.Add($name)
        }
    }

    return [pscustomobject]@{
        Viewport = $Viewport
        Page = $Page
        Screenshot = $Screenshot
        VisibleNamedElements = $visibleNames.Count
        EmptyButtonNames = $emptyButtons
        EmptyComboBoxNames = $emptyComboBoxes
        EmptySelections = @($emptySelections)
        NarrowLongText = @($narrowLongText)
        Toggles = @($toggles)
        ComboBoxes = @($comboBoxes)
        VisibleNames = @($visibleNames)
    }
}

$process = $null
$evidence = [System.Collections.Generic.List[object]]::new()
$failures = [System.Collections.Generic.List[object]]::new()
$startedAt = [DateTimeOffset]::Now
try {
    $process = Start-Process -FilePath $executable -PassThru
    for ($attempt = 0; $attempt -lt 80; $attempt++) {
        Start-Sleep -Milliseconds 250
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowHandle -ne 0) {
            break
        }
    }
    if ($process.HasExited -or $process.MainWindowHandle -eq 0) {
        throw 'The application did not expose a main window.'
    }

    $handle = $process.MainWindowHandle
    $root = Get-AppRoot -Handle $handle
    $routes = @(
        @{ Kind = 'Navigation'; Name = 'Home'; Label = 'home' },
        @{ Kind = 'Tile'; Name = 'Dual insulated'; Label = 'dual-insulated' },
        @{ Kind = 'Navigation'; Name = 'Home'; Label = 'home-before-flat' },
        @{ Kind = 'Tile'; Name = 'Flat cable'; Label = 'flat-placeholder' },
        @{ Kind = 'Navigation'; Name = 'Home'; Label = 'home-before-dshape' },
        @{ Kind = 'Tile'; Name = 'D-shape cable'; Label = 'dshape-placeholder' },
        @{ Kind = 'Navigation'; Name = 'Live result'; Label = 'costing-live-result' },
        @{ Kind = 'Navigation'; Name = 'Costing basis'; Label = 'costing-basis' },
        @{ Kind = 'Navigation'; Name = 'Conductor'; Label = 'conductor' },
        @{ Kind = 'Navigation'; Name = 'Insulation compound'; Label = 'compound' },
        @{ Kind = 'Navigation'; Name = 'Masterbatch colour'; Label = 'masterbatch' },
        @{ Kind = 'Navigation'; Name = 'Core print'; Label = 'core-print' },
        @{ Kind = 'Navigation'; Name = 'Production and labour'; Label = 'production-labour' },
        @{ Kind = 'Navigation'; Name = 'Customer and core name'; Label = 'customer-core-name' },
        @{ Kind = 'Navigation'; Name = 'Quotation'; Label = 'quotation' },
        @{ Kind = 'Navigation'; Name = 'Calculation trace'; Label = 'calculation-trace' },
        @{ Kind = 'Navigation'; Name = 'Contract review'; Label = 'contract-review' },
        @{ Kind = 'Navigation'; Name = 'Live Data'; Label = 'live-data' },
        @{ Kind = 'Navigation'; Name = 'Production speeds'; Label = 'production-speeds' },
        @{ Kind = 'Navigation'; Name = 'Braid calculator'; Label = 'braid' },
        @{ Kind = 'Navigation'; Name = 'Buncher lay'; Label = 'buncher' },
        @{ Kind = 'Navigation'; Name = 'Coil calculator'; Label = 'coil' },
        @{ Kind = 'Navigation'; Name = 'Reports'; Label = 'reports-placeholder' },
        @{ Kind = 'Navigation'; Name = 'Settings'; Label = 'settings' }
    )
    $viewports = @(
        @{ Name = 'wide'; Width = 1600; Height = 940 },
        @{ Name = 'compact'; Width = 1000; Height = 800 }
    )

    foreach ($viewport in $viewports) {
        [AtagNativeAppControl]::SetWindowPos(
            $handle,
            [IntPtr]::Zero,
            24,
            24,
            $viewport.Width,
            $viewport.Height,
            0x0004) | Out-Null
        Start-Sleep -Milliseconds 750

        foreach ($route in $routes) {
            try {
                if ($route.Kind -eq 'Navigation') {
                    Select-NavigationItem -Root $root -Name $route.Name
                }
                else {
                    Invoke-ContainingButton -Root $root -ChildName $route.Name
                }

                $fileName = "{0}-{1}.png" -f $viewport.Name, $route.Label
                $path = Join-Path $output $fileName
                Save-WindowCapture -Handle $handle -Path $path
                $evidence.Add((Get-PageEvidence `
                    -Root $root `
                    -Viewport $viewport.Name `
                    -Page $route.Label `
                    -Screenshot $fileName))
            }
            catch {
                $failures.Add([pscustomobject]@{
                    Viewport = $viewport.Name
                    Page = $route.Label
                    Error = $_.Exception.Message
                })
            }
        }
    }

    $process.Refresh()
    $result = [pscustomobject]@{
        StartedAt = $startedAt
        FinishedAt = [DateTimeOffset]::Now
        Executable = $executable
        ProcessId = $process.Id
        Responding = $process.Responding
        WorkingSetBytes = $process.WorkingSet64
        Evidence = @($evidence)
        Failures = @($failures)
    }
    $result | ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath (Join-Path $output 'audit.json') -Encoding UTF8
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        [AtagNativeAppControl]::PostMessage(
            $process.MainWindowHandle,
            0x0010,
            [IntPtr]::Zero,
            [IntPtr]::Zero) | Out-Null
        for ($attempt = 0; $attempt -lt 40; $attempt++) {
            Start-Sleep -Milliseconds 250
            if ($process.HasExited) {
                break
            }
            $process.Refresh()
        }
    }
}

$auditPath = Join-Path $output 'audit.json'
if (-not (Test-Path -LiteralPath $auditPath -PathType Leaf)) {
    throw 'The Windows App Control audit did not produce audit.json.'
}

$audit = Get-Content -LiteralPath $auditPath -Raw | ConvertFrom-Json
Write-Host "Windows App Control audit complete."
Write-Host "Pages captured: $($audit.Evidence.Count)"
Write-Host "Navigation failures: $($audit.Failures.Count)"
Write-Host "Output: $output"
if ($audit.Failures.Count -gt 0) {
    $audit.Failures | Format-Table -AutoSize
    exit 1
}
