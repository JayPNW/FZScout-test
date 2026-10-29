# Draws the tray and app icons into ResourceHunterBonusScout/Assets: the glyph in white on a colored circle.
# Glyphs are black-on-transparent PNGs, like a Noun Project download. Credit them in README.md and the settings window.
# The error glyph is already a circle with a cutout, so it's colored instead, with white showing through the cutout.
# Icons whose glyph is missing are skipped.
param(
    [string]$Glyph = (Join-Path $PSScriptRoot '../ResourceHunterBonusScout/Assets/Source/search.png'),
    [string]$ErrorGlyph = (Join-Path $PSScriptRoot '../ResourceHunterBonusScout/Assets/Source/error.png'),
    [string]$OutDir = (Join-Path $PSScriptRoot '../ResourceHunterBonusScout/Assets')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Keeps the glyph's alpha and makes every pixel the given color.
function New-Tint([System.Drawing.Color]$color) {
    $tint = New-Object System.Drawing.Imaging.ImageAttributes
    $matrix = New-Object System.Drawing.Imaging.ColorMatrix
    $matrix.Matrix00 = 0; $matrix.Matrix11 = 0; $matrix.Matrix22 = 0
    $matrix.Matrix40 = $color.R / 255; $matrix.Matrix41 = $color.G / 255; $matrix.Matrix42 = $color.B / 255
    $tint.SetColorMatrix($matrix)
    return $tint
}

function New-IconImage([int]$size, [System.Drawing.Color]$color, [System.Drawing.Image]$glyphImage, [bool]$filled) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.Clear([System.Drawing.Color]::Transparent)
    $pad = [Math]::Max(0.5, $size / 32)
    if ($filled) {
        # White behind the cutout, hidden under the glyph's edge.
        $inset = $size / 5
        $g.FillEllipse([System.Drawing.Brushes]::White, $inset, $inset, $size - 2 * $inset, $size - 2 * $inset)
        $box = $size - 2 * $pad
        $tint = New-Tint $color
    } else {
        $g.FillEllipse((New-Object System.Drawing.SolidBrush $color), $pad, $pad, $size - 2 * $pad, $size - 2 * $pad)
        $box = $size * 0.68
        $tint = New-Tint ([System.Drawing.Color]::White)
    }

    # Fit the glyph in the middle of the circle, keeping its shape.
    $scale = $box / [Math]::Max($glyphImage.Width, $glyphImage.Height)
    $w = $glyphImage.Width * $scale; $h = $glyphImage.Height * $scale
    $dest = New-Object System.Drawing.Rectangle ([int](($size - $w) / 2)), ([int](($size - $h) / 2)), ([int]$w), ([int]$h)
    $g.DrawImage($glyphImage, $dest, 0, 0, $glyphImage.Width, $glyphImage.Height, [System.Drawing.GraphicsUnit]::Pixel, $tint)

    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return , $ms.ToArray()
}

function Save-Icon([string]$path, [System.Drawing.Color]$color, [string]$glyphPath, [bool]$filled = $false) {
    if (-not (Test-Path $glyphPath)) { Write-Warning "Skipping $path because $glyphPath is missing."; return }
    $glyphImage = [System.Drawing.Image]::FromFile((Resolve-Path $glyphPath))
    $sizes = 16, 20, 24, 32, 40, 48, 64, 256
    $images = $sizes | ForEach-Object { , (New-IconImage $_ $color $glyphImage $filled) }
    $glyphImage.Dispose()
    $fs = [System.IO.File]::Create($path)
    $w = New-Object System.IO.BinaryWriter $fs
    $w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $s = $sizes[$i]; $len = $images[$i].Length
        $w.Write([byte]($s % 256)); $w.Write([byte]($s % 256)); $w.Write([byte]0); $w.Write([byte]0)
        $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]$len); $w.Write([uint32]$offset)
        $offset += $len
    }
    foreach ($img in $images) { $w.Write($img) }
    $w.Dispose()
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
Save-Icon (Join-Path $OutDir 'Idle.ico') ([System.Drawing.Color]::FromArgb(255, 110, 118, 129)) $Glyph
Save-Icon (Join-Path $OutDir 'Active.ico') ([System.Drawing.Color]::FromArgb(255, 16, 140, 120)) $Glyph
Save-Icon (Join-Path $OutDir 'Error.ico') ([System.Drawing.Color]::FromArgb(255, 200, 40, 40)) $ErrorGlyph $true
