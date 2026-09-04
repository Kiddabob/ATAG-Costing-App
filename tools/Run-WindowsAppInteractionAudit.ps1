[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ExecutablePath,

    [string]$OutputRoot,

    [switch]$ForceWarp,

    [switch]$CloseWhilePreviewDetached
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifactRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repoRoot 'artifacts'))
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputRoot = Join-Path $artifactRoot "app-interaction-audit-$stamp"
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
Add-Type -AssemblyName System.Windows.Forms
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class AtagNativeInteractionControl
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr insertAfter, int x, int y, int width, int height,
        uint flags);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(
        IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    public static IntPtr[] ProcessWindows(uint targetProcessId)
    {
        var windows = new List<IntPtr>();
        EnumWindows((window, state) =>
        {
            GetWindowThreadProcessId(window, out var processId);
            if (processId == targetProcessId && IsWindowVisible(window))
            {
                windows.Add(window);
            }
            return true;
        }, IntPtr.Zero);
        return windows.ToArray();
    }

    public static string WindowTitle(IntPtr window)
    {
        var text = new StringBuilder(512);
        GetWindowText(window, text, text.Capacity);
        return text.ToString();
    }
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
    return ,$Root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        $condition)
}

function Find-ElementByNameAndType {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name,
        [System.Windows.Automation.ControlType]$ControlType
    )

    $matches = Find-AllByName -Root $Root -Name $Name
    for ($index = 0; $index -lt $matches.Count; $index++) {
        $candidate = $matches.Item($index)
        if ($candidate.Current.ControlType -eq $ControlType) {
            return $candidate
        }
    }
    throw "$ControlType was not available with name '$Name'."
}

function Select-NavigationItem {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $candidate = Find-ElementByNameAndType `
        -Root $Root `
        -Name $Name `
        -ControlType ([System.Windows.Automation.ControlType]::ListItem)
    $selection = [System.Windows.Automation.SelectionItemPattern](
        $candidate.GetCurrentPattern(
            [System.Windows.Automation.SelectionItemPattern]::Pattern))
    $selection.Select()
    Start-Sleep -Milliseconds 850
}

function Get-ComboSelection {
    param([System.Windows.Automation.AutomationElement]$ComboBox)

    $selection = [System.Windows.Automation.SelectionPattern](
        $ComboBox.GetCurrentPattern(
            [System.Windows.Automation.SelectionPattern]::Pattern))
    return (($selection.Current.GetSelection() |
        ForEach-Object { $_.Current.Name }) -join ', ')
}

function Set-ComboSelection {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$ComboName,
        [string]$ItemName
    )

    $combo = Find-ElementByNameAndType `
        -Root $Root `
        -Name $ComboName `
        -ControlType ([System.Windows.Automation.ControlType]::ComboBox)
    try {
        $scrollItem = [System.Windows.Automation.ScrollItemPattern](
            $combo.GetCurrentPattern(
                [System.Windows.Automation.ScrollItemPattern]::Pattern))
        $scrollItem.ScrollIntoView()
    }
    catch {
        # Already visible controls normally do not expose ScrollItemPattern.
    }
    $expand = [System.Windows.Automation.ExpandCollapsePattern](
        $combo.GetCurrentPattern(
            [System.Windows.Automation.ExpandCollapsePattern]::Pattern))
    $expand.Expand()
    Start-Sleep -Milliseconds 400

    $desktop = [System.Windows.Automation.AutomationElement]::RootElement
    $listItems = $desktop.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::ListItem))
    for ($pass = 0; $pass -lt 2; $pass++) {
        for ($index = 0; $index -lt $listItems.Count; $index++) {
            $candidate = $listItems.Item($index)
            $candidateName = $candidate.Current.Name
            $isMatch = if ($pass -eq 0) {
                $candidateName -eq $ItemName
            }
            else {
                $candidateName.StartsWith(
                    $ItemName,
                    [StringComparison]::OrdinalIgnoreCase)
            }
            if (-not $isMatch -or $candidate.Current.IsOffscreen) {
                continue
            }

            try {
                $selectionItem = [System.Windows.Automation.SelectionItemPattern](
                    $candidate.GetCurrentPattern(
                        [System.Windows.Automation.SelectionItemPattern]::Pattern))
                $selectionItem.Select()
                Start-Sleep -Milliseconds 650
                return Get-ComboSelection -ComboBox $combo
            }
            catch {
                # Continue to any other matching popup item.
            }
        }
    }

    try { $expand.Collapse() } catch { }
    throw "Combo '$ComboName' did not expose item '$ItemName'."
}

function Set-ComboSelectionByCurrentItem {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$CurrentItem,
        [string]$ItemName
    )

    $all = $Root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::ComboBox))
    for ($index = 0; $index -lt $all.Count; $index++) {
        $combo = $all.Item($index)
        if ((Get-ComboSelection -ComboBox $combo) -ne $CurrentItem) {
            continue
        }

        $expand = [System.Windows.Automation.ExpandCollapsePattern](
            $combo.GetCurrentPattern(
                [System.Windows.Automation.ExpandCollapsePattern]::Pattern))
        $expand.Expand()
        Start-Sleep -Milliseconds 400
        $desktop = [System.Windows.Automation.AutomationElement]::RootElement
        $matches = Find-AllByName -Root $desktop -Name $ItemName
        for ($itemIndex = 0; $itemIndex -lt $matches.Count; $itemIndex++) {
            $candidate = $matches.Item($itemIndex)
            if ($candidate.Current.ControlType -eq
                [System.Windows.Automation.ControlType]::ListItem -and
                -not $candidate.Current.IsOffscreen) {
                $selectionItem = [System.Windows.Automation.SelectionItemPattern](
                    $candidate.GetCurrentPattern(
                        [System.Windows.Automation.SelectionItemPattern]::Pattern))
                $selectionItem.Select()
                Start-Sleep -Milliseconds 1100
                return Get-ComboSelection -ComboBox $combo
            }
        }
        try { $expand.Collapse() } catch { }
    }
    throw "No combo selected '$CurrentItem' or item '$ItemName' was unavailable."
}

function Select-FirstComboItemByKeyboard {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$ComboName
    )

    $combo = Find-ElementByNameAndType `
        -Root $Root `
        -Name $ComboName `
        -ControlType ([System.Windows.Automation.ControlType]::ComboBox)
    $combo.SetFocus()
    Start-Sleep -Milliseconds 250
    [System.Windows.Forms.SendKeys]::SendWait('%{DOWN}')
    Start-Sleep -Milliseconds 300
    [System.Windows.Forms.SendKeys]::SendWait('{HOME}{ENTER}')
    Start-Sleep -Milliseconds 700
    return Get-ComboSelection -ComboBox $combo
}

function Set-NumericValue {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name,
        [string]$Value
    )

    $matches = Find-AllByName -Root $Root -Name $Name
    for ($index = 0; $index -lt $matches.Count; $index++) {
        $candidate = $matches.Item($index)
        try {
            $valuePattern = [System.Windows.Automation.ValuePattern](
                $candidate.GetCurrentPattern(
                    [System.Windows.Automation.ValuePattern]::Pattern))
            $valuePattern.SetValue($Value)
            Start-Sleep -Milliseconds 260
            return $valuePattern.Current.Value
        }
        catch {
            # NumberBox can expose its ValuePattern on a nested edit element.
        }

        $children = $candidate.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.Condition]::TrueCondition)
        for ($childIndex = 0; $childIndex -lt $children.Count; $childIndex++) {
            $child = $children.Item($childIndex)
            try {
                $valuePattern = [System.Windows.Automation.ValuePattern](
                    $child.GetCurrentPattern(
                        [System.Windows.Automation.ValuePattern]::Pattern))
                $valuePattern.SetValue($Value)
                Start-Sleep -Milliseconds 260
                return $valuePattern.Current.Value
            }
            catch {
                # Try the next descendant.
            }
        }
    }
    throw "Numeric control '$Name' did not expose ValuePattern."
}

function Toggle-Control {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $matches = Find-AllByName -Root $Root -Name $Name
    for ($index = 0; $index -lt $matches.Count; $index++) {
        $candidate = $matches.Item($index)
        try {
            $toggle = [System.Windows.Automation.TogglePattern](
                $candidate.GetCurrentPattern(
                    [System.Windows.Automation.TogglePattern]::Pattern))
            $toggle.Toggle()
            Start-Sleep -Milliseconds 850
            return $toggle.Current.ToggleState.ToString()
        }
        catch {
            # Continue to another matching automation element.
        }
    }
    throw "Toggle '$Name' was not available."
}

function Invoke-Button {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $button = Find-ElementByNameAndType `
        -Root $Root `
        -Name $Name `
        -ControlType ([System.Windows.Automation.ControlType]::Button)
    $invoke = [System.Windows.Automation.InvokePattern](
        $button.GetCurrentPattern(
            [System.Windows.Automation.InvokePattern]::Pattern))
    $invoke.Invoke()
    Start-Sleep -Milliseconds 900
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
            Start-Sleep -Milliseconds 900
            return
        }
        $candidate = $walker.GetParent($candidate)
    }
    throw "No invokable parent button was found for: $ChildName"
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
        $names.Add($candidate.Current.Name)
    }
    return ,$names
}

function Save-WindowCapture {
    param(
        [IntPtr]$Handle,
        [string]$Path
    )

    [AtagNativeInteractionControl]::SetForegroundWindow($Handle) | Out-Null
    Start-Sleep -Milliseconds 200
    $rect = [AtagNativeInteractionControl+RECT]::new()
    if (-not [AtagNativeInteractionControl]::GetWindowRect(
        $Handle, [ref]$rect)) {
        throw "GetWindowRect failed for HWND $Handle"
    }
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $bitmap = [System.Drawing.Bitmap]::new($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen(
            $rect.Left, $rect.Top, 0, 0, $bitmap.Size)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Get-TitledProcessWindows {
    param([uint32]$ProcessId)

    return ,@([AtagNativeInteractionControl]::ProcessWindows($ProcessId) |
        Where-Object {
            $title = [AtagNativeInteractionControl]::WindowTitle($_)
            -not [string]::IsNullOrWhiteSpace($title) -and
                $title -ne 'Pop-upHost'
        })
}

function Add-TestResult {
    param(
        [System.Collections.Generic.List[object]]$Results,
        [string]$Name,
        [scriptblock]$Body
    )

    try {
        $detail = & $Body
        $Results.Add([pscustomobject]@{
            Name = $Name
            Passed = $true
            Detail = $detail
        })
    }
    catch {
        $Results.Add([pscustomobject]@{
            Name = $Name
            Passed = $false
            Detail = $_.Exception.Message
        })
    }
}

$process = $null
$originalWarp = $env:ATAG_COSTING_3D_FORCE_WARP
$results = [System.Collections.Generic.List[object]]::new()
$startedAt = [DateTimeOffset]::Now
$startupLogPath = Join-Path $env:TEMP 'ATAG-Costing-startup.log'
$startupLogLineCount = if (Test-Path -LiteralPath $startupLogPath) {
    @(Get-Content -LiteralPath $startupLogPath).Count
}
else {
    0
}
try {
    if ($ForceWarp) {
        $env:ATAG_COSTING_3D_FORCE_WARP = '1'
    }
    else {
        Remove-Item Env:ATAG_COSTING_3D_FORCE_WARP -ErrorAction SilentlyContinue
    }

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
    [AtagNativeInteractionControl]::SetWindowPos(
        $handle, [IntPtr]::Zero, 24, 24, 1600, 940, 0x0004) | Out-Null
    Start-Sleep -Milliseconds 900
    $root = Get-AppRoot -Handle $handle

    if (-not $ForceWarp) {
        Add-TestResult -Results $results -Name 'Dual insulation LIVE preview' -Body {
            Select-NavigationItem -Root $root -Name 'Home'
            Invoke-ContainingButton -Root $root -ChildName 'Dual insulated'
            $toggle = Toggle-Control -Root $root -Name 'Preview Off'
            if ((Find-AllByName -Root $root -Name 'LIVE Dual Preview').Count -eq 0 -or
                (Find-AllByName -Root $root -Name 'Scaled cross-section').Count -eq 0 -or
                (Find-AllByName -Root $root -Name 'Inside-to-outside side profile').Count -eq 0) {
                throw 'The Dual cross-section and side profile were not available.'
            }
            Save-WindowCapture -Handle $handle `
                -Path (Join-Path $output 'dual-live-preview.png')
            [pscustomobject]@{ Preview = $toggle; Views = 'Cross-section and side profile' }
        }

        Add-TestResult -Results $results -Name 'Braid ends filter and preview' -Body {
            Select-NavigationItem -Root $root -Name 'Braid calculator'
            $ends = Set-ComboSelection `
                -Root $root -ComboName 'Ends per carrier' -ItemName '10'
            $wireCombo = Find-ElementByNameAndType `
                -Root $root `
                -Name 'Retained Copper braid wire' `
                -ControlType ([System.Windows.Automation.ControlType]::ComboBox)
            $wire = Get-ComboSelection -ComboBox $wireCombo
            if ($ends -ne '10' -or [string]::IsNullOrWhiteSpace($wire)) {
                throw "Ends='$ends'; retained wire='$wire'."
            }
            $toggle = Toggle-Control -Root $root -Name 'Preview Off'
            Save-WindowCapture -Handle $handle `
                -Path (Join-Path $output 'braid-10-ends-preview.png')
            [pscustomobject]@{ Ends = $ends; Wire = $wire; Preview = $toggle }
        }

        Add-TestResult -Results $results -Name 'Buncher retained machine and preview' -Body {
            Select-NavigationItem -Root $root -Name 'Buncher lay'
            $lay = Select-FirstComboItemByKeyboard `
                -Root $root -ComboName 'Target lay length'
            if ($lay -ne '120 mm' -or
                (Find-AllByName -Root $root -Name 'Large buncher').Count -eq 0 -or
                (Find-AllByName -Root $root -Name 'Gear A 57 · Gear B 20').Count -eq 0) {
                throw "Lay='$lay'; expected Large buncher and gears 57/20."
            }
            $toggle = Toggle-Control -Root $root -Name 'Preview Off'
            Start-Sleep -Milliseconds 500
            $renderedCoreGroup = Find-AllByName `
                -Root $root `
                -Name '6 equal cable cores · group 1-5'
            if ($renderedCoreGroup.Count -eq 0) {
                throw 'The default 6-core 1-5 group was not rendered as equal cable cores.'
            }
            Save-WindowCapture -Handle $handle `
                -Path (Join-Path $output 'buncher-120mm-preview.png')
            [pscustomobject]@{
                Lay = $lay
                Machine = 'Large buncher'
                Gears = '57 & 20'
                Preview = $toggle
                CoreGroup = '6 cores · 1-5'
            }
        }

        Add-TestResult -Results $results -Name 'Coil approved flat-cable example' -Body {
            Select-NavigationItem -Root $root -Name 'Coil calculator'
            Set-NumericValue -Root $root `
                -Name 'Cable height · radial (mm)' -Value '2.5' | Out-Null
            Set-NumericValue -Root $root `
                -Name 'Cable width · axial pitch (mm)' -Value '4.8' | Out-Null
            Set-NumericValue -Root $root `
                -Name 'Finished coil outside diameter (mm)' -Value '13' | Out-Null
            Set-NumericValue -Root $root `
                -Name 'Required axial length (mm)' -Value '90' | Out-Null
            Set-NumericValue -Root $root -Name 'Tail 1 (mm)' -Value '50' | Out-Null
            Set-NumericValue -Root $root -Name 'Tail 2 (mm)' -Value '50' | Out-Null
            Set-NumericValue -Root $root `
                -Name 'Strip length 1 (mm) · optional' -Value '0' | Out-Null
            Set-NumericValue -Root $root -Name 'Number of coils' -Value '1100' | Out-Null
            Set-NumericValue -Root $root `
                -Name 'Strip length 2 (mm) · optional' -Value '0' | Out-Null
            Start-Sleep -Milliseconds 900
            $required = @('8 mm', '19 full turns', '0.733 m', '806.683 m')
            $missing = @($required | Where-Object {
                (Find-AllByName -Root $root -Name $_).Count -eq 0
            })
            if ($missing.Count -gt 0) {
                throw "Missing calculated outputs: $($missing -join ', ')."
            }
            $toggle = Toggle-Control -Root $root -Name 'Preview Off'
            Start-Sleep -Milliseconds 500
            $renderedCoil = Find-AllByName `
                -Root $root `
                -Name 'Bar 8 mm · 19 complete turns · wound width 91.2 mm'
            if ($renderedCoil.Count -eq 0) {
                throw 'The LIVE coil did not expose the approved calculated geometry.'
            }
            Save-WindowCapture -Handle $handle `
                -Path (Join-Path $output 'coil-approved-example.png')
            [pscustomobject]@{
                Bar = '8 mm'
                Turns = '19 full turns'
                PerCoil = '0.733 m'
                Total = '806.683 m'
                Preview = $toggle
            }
        }
    }

    Add-TestResult -Results $results -Name 'COR interactive 3D renderer' -Body {
        Select-NavigationItem -Root $root -Name 'Live result'
        Toggle-Control -Root $root -Name 'Off' | Out-Null
        $mode = Set-ComboSelectionByCurrentItem `
            -Root $root -CurrentItem 'Simple' -ItemName 'Interactive 3D'
        $names = Get-VisibleNames -Root $root
        Save-WindowCapture -Handle $handle `
            -Path (Join-Path $output $(if ($ForceWarp) {
                'cor-3d-warp.png'
            } else {
                'cor-3d-hardware.png'
            }))
        [pscustomobject]@{ Mode = $mode }
    }

    Add-TestResult -Results $results -Name 'COR preview detach and redock' -Body {
        Invoke-Button -Root $root -Name 'Open in window'
        $windows = Get-TitledProcessWindows -ProcessId ([uint32]$process.Id)
        if ($windows.Count -ne 2) {
            throw "Expected two visible app windows after detach, got $($windows.Count)."
        }
        $detached = $windows | Where-Object { $_ -ne $handle } |
            Select-Object -First 1
        $detachedRoot = Get-AppRoot -Handle $detached
        $names = Get-VisibleNames -Root $detachedRoot
        $driver = $names | Where-Object {
            $_ -match '^(Hardware|WARP software) · [0-9]'
        } | Select-Object -First 1
        if ([string]::IsNullOrWhiteSpace([string]$driver)) {
            throw 'The detached renderer did not expose its driver status.'
        }
        if ($ForceWarp -and $driver -notmatch '^WARP software') {
            throw "Expected WARP software, got '$driver'."
        }
        if (-not $ForceWarp -and $driver -notmatch '^Hardware') {
            throw "Expected Hardware, got '$driver'."
        }
        Save-WindowCapture -Handle $detached `
            -Path (Join-Path $output $(if ($ForceWarp) {
                'cor-3d-warp-detached.png'
            } else {
                'cor-3d-hardware-detached.png'
            }))
        if ($CloseWhilePreviewDetached) {
            [AtagNativeInteractionControl]::PostMessage(
                $handle, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
            for ($attempt = 0; $attempt -lt 40; $attempt++) {
                Start-Sleep -Milliseconds 250
                if ($process.HasExited) { break }
                $process.Refresh()
            }
            return [pscustomobject]@{
                DetachedWindows = 2
                ClosedMainWhileDetached = $true
                ProcessExited = $process.HasExited
                Driver = $driver
            }
        }

        Invoke-Button -Root $detachedRoot -Name 'Return to dock'
        for ($attempt = 0; $attempt -lt 24; $attempt++) {
            Start-Sleep -Milliseconds 250
            $windows = Get-TitledProcessWindows `
                -ProcessId ([uint32]$process.Id)
            if ($windows.Count -eq 1) { break }
        }
        if ($windows.Count -ne 1) {
            $titles = $windows | ForEach-Object {
                [AtagNativeInteractionControl]::WindowTitle($_)
            }
            throw "Expected one titled app window after redock, got $($windows.Count): $($titles -join ' | ')."
        }
        [pscustomobject]@{
            DetachedWindows = 2
            RedockedWindows = 1
            Driver = $driver
        }
    }

    if ($CloseWhilePreviewDetached) {
        Add-TestResult -Results $results -Name 'Detached-preview shutdown log' -Body {
            $newLogLines = if (Test-Path -LiteralPath $startupLogPath) {
                @(Get-Content -LiteralPath $startupLogPath |
                    Select-Object -Skip $startupLogLineCount)
            }
            else {
                @()
            }
            $unhandled = @($newLogLines | Where-Object {
                $_ -match 'Unhandled WinUI exception'
            })
            if ($unhandled.Count -gt 0) {
                throw ($unhandled -join [Environment]::NewLine)
            }
            'No unhandled WinUI exception was logged during shutdown.'
        }
    }

    $process.Refresh()
    [pscustomobject]@{
        StartedAt = $startedAt
        FinishedAt = [DateTimeOffset]::Now
        Executable = $executable
        ProcessId = $process.Id
        ForceWarp = [bool]$ForceWarp
        CloseWhilePreviewDetached = [bool]$CloseWhilePreviewDetached
        Responding = -not $process.HasExited -and $process.Responding
        WorkingSetBytes = if ($process.HasExited) { 0 } else { $process.WorkingSet64 }
        Results = @($results)
    } | ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath (Join-Path $output 'interaction-audit.json') `
            -Encoding UTF8
}
finally {
    if ($null -eq $originalWarp) {
        Remove-Item Env:ATAG_COSTING_3D_FORCE_WARP -ErrorAction SilentlyContinue
    }
    else {
        $env:ATAG_COSTING_3D_FORCE_WARP = $originalWarp
    }

    if ($null -ne $process -and -not $process.HasExited) {
        [AtagNativeInteractionControl]::PostMessage(
            $process.MainWindowHandle,
            0x0010,
            [IntPtr]::Zero,
            [IntPtr]::Zero) | Out-Null
        for ($attempt = 0; $attempt -lt 40; $attempt++) {
            Start-Sleep -Milliseconds 250
            if ($process.HasExited) { break }
            $process.Refresh()
        }
    }
}

$auditPath = Join-Path $output 'interaction-audit.json'
if (-not (Test-Path -LiteralPath $auditPath -PathType Leaf)) {
    throw 'The Windows App interaction audit did not produce JSON evidence.'
}
$audit = Get-Content -LiteralPath $auditPath -Raw | ConvertFrom-Json
$failed = @($audit.Results | Where-Object { -not $_.Passed })
Write-Host "Windows App interaction audit complete."
Write-Host "Passed: $($audit.Results.Count - $failed.Count)"
Write-Host "Failed: $($failed.Count)"
Write-Host "Output: $output"
if ($failed.Count -gt 0) {
    $failed | Select-Object Name, Detail | Format-Table -AutoSize -Wrap
    exit 1
}
