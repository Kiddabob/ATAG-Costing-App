param(
    [string]$SourceIconPath = (
        Join-Path $PSScriptRoot "..\..\ATAG Design LTD. Icon.ico"
    ),
    [string]$AssetsDirectory = (
        Join-Path $PSScriptRoot "..\src\ATAG.Costing.WinUI\Assets"
    )
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$resolvedSourceIcon = (Resolve-Path -LiteralPath $SourceIconPath).Path
$resolvedAssetsDirectory = (Resolve-Path -LiteralPath $AssetsDirectory).Path
$iconBytes = [System.IO.File]::ReadAllBytes($resolvedSourceIcon)
$iconImageCount = [BitConverter]::ToUInt16($iconBytes, 4)
$pngFrames = @()

for ($index = 0; $index -lt $iconImageCount; $index++)
{
    $entryOffset = 6 + ($index * 16)
    $width = if ($iconBytes[$entryOffset] -eq 0)
    {
        256
    }
    else
    {
        [int]$iconBytes[$entryOffset]
    }

    $height = if ($iconBytes[$entryOffset + 1] -eq 0)
    {
        256
    }
    else
    {
        [int]$iconBytes[$entryOffset + 1]
    }

    $dataLength = [BitConverter]::ToUInt32($iconBytes, $entryOffset + 8)
    $dataOffset = [BitConverter]::ToUInt32($iconBytes, $entryOffset + 12)
    $isPng =
        $dataLength -ge 8 -and
        $iconBytes[$dataOffset] -eq 0x89 -and
        $iconBytes[$dataOffset + 1] -eq 0x50 -and
        $iconBytes[$dataOffset + 2] -eq 0x4E -and
        $iconBytes[$dataOffset + 3] -eq 0x47

    if ($isPng)
    {
        $pngFrames += [PSCustomObject]@{
            Width = $width
            Height = $height
            DataLength = [int]$dataLength
            DataOffset = [int]$dataOffset
        }
    }
}

$largestPngFrame = $pngFrames |
    Sort-Object { $_.Width * $_.Height } -Descending |
    Select-Object -First 1

if ($null -eq $largestPngFrame)
{
    throw "The source icon does not contain a PNG frame."
}

$frameBytes = [byte[]]::new($largestPngFrame.DataLength)
[Array]::Copy(
    $iconBytes,
    $largestPngFrame.DataOffset,
    $frameBytes,
    0,
    $largestPngFrame.DataLength)

$frameStream = [System.IO.MemoryStream]::new($frameBytes, $false)
$sourceImage = [System.Drawing.Image]::FromStream($frameStream, $true, $true)

function Write-PngAsset
{
    param(
        [Parameter(Mandatory)]
        [string]$FileName,

        [Parameter(Mandatory)]
        [int]$CanvasWidth,

        [Parameter(Mandatory)]
        [int]$CanvasHeight,

        [Parameter(Mandatory)]
        [int]$LogoEdge
    )

    $outputPath = Join-Path $resolvedAssetsDirectory $FileName
    $bitmap = [System.Drawing.Bitmap]::new(
        $CanvasWidth,
        $CanvasHeight,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try
    {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.CompositingMode =
            [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.CompositingQuality =
            [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode =
            [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode =
            [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.SmoothingMode =
            [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

        $left = [int](($CanvasWidth - $LogoEdge) / 2)
        $top = [int](($CanvasHeight - $LogoEdge) / 2)
        $destination = [System.Drawing.Rectangle]::new(
            $left,
            $top,
            $LogoEdge,
            $LogoEdge)

        $graphics.DrawImage($sourceImage, $destination)
        $bitmap.Save(
            $outputPath,
            [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally
    {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

try
{
    [System.IO.File]::Copy(
        $resolvedSourceIcon,
        (Join-Path $resolvedAssetsDirectory "AppIcon.ico"),
        $true)
    [System.IO.File]::SetLastWriteTimeUtc(
        (Join-Path $resolvedAssetsDirectory "AppIcon.ico"),
        [DateTime]::UtcNow)

    Write-PngAsset "LockScreenLogo.scale-200.png" 48 48 48
    Write-PngAsset "Square150x150Logo.scale-200.png" 300 300 300
    Write-PngAsset "Square44x44Logo.scale-200.png" 88 88 88
    Write-PngAsset "Square44x44Logo.targetsize-24_altform-unplated.png" 24 24 24
    Write-PngAsset "Square44x44Logo.targetsize-48_altform-lightunplated.png" 48 48 48
    Write-PngAsset "StoreLogo.png" 50 50 50
    Write-PngAsset "Wide310x150Logo.scale-200.png" 620 300 256
    Write-PngAsset "SplashScreen.scale-200.png" 1240 600 256
}
finally
{
    $sourceImage.Dispose()
    $frameStream.Dispose()
}

Get-ChildItem -LiteralPath $resolvedAssetsDirectory -File |
    Where-Object {
        $_.Name -eq "AppIcon.ico" -or
        $_.Extension -eq ".png"
    } |
    Sort-Object Name |
    Select-Object Name, Length, LastWriteTime
