param(
    [string]$WorkspaceRoot = (
        Join-Path $PSScriptRoot "..\.."
    )
)

$ErrorActionPreference = "Stop"

$resolvedWorkspace = (Resolve-Path -LiteralPath $WorkspaceRoot).Path
$launcherPath = Join-Path $resolvedWorkspace "Open ATAG Costing.cmd"
$shortcutPath = Join-Path $resolvedWorkspace "Open ATAG Costing.lnk"
$iconPath = Join-Path $resolvedWorkspace (
    "ATAG Costing\src\ATAG.Costing.WinUI\Assets\AppIcon.ico"
)

if (-not (Test-Path -LiteralPath $launcherPath -PathType Leaf))
{
    throw "The portable launcher was not found: $launcherPath"
}

if (-not (Test-Path -LiteralPath $iconPath -PathType Leaf))
{
    throw "The ATAG application icon was not found: $iconPath"
}

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $launcherPath
$shortcut.WorkingDirectory = $resolvedWorkspace
$shortcut.IconLocation = "$iconPath,0"
$shortcut.Description = "Open ATAG Costing"
$shortcut.WindowStyle = 1
$shortcut.Save()

$savedShortcut = $shell.CreateShortcut($shortcutPath)

[PSCustomObject]@{
    Shortcut = $shortcutPath
    Target = $savedShortcut.TargetPath
    WorkingDirectory = $savedShortcut.WorkingDirectory
    Icon = $savedShortcut.IconLocation
}
