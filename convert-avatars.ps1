# Convert SVG avatars to PNG using .NET
# This script requires Windows and uses WPF to render SVGs

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase, System.Xaml

$svgFolder = "Resources\Images"
$size = 128

Write-Host "Converting SVG avatars to PNG..." -ForegroundColor Cyan

Get-ChildItem -Path $svgFolder -Filter "avatar_*.svg" | ForEach-Object {
    $svgPath = $_.FullName
    $pngPath = $svgPath -replace '\.svg$', '.png'
    $fileName = $_.Name
    
    try {
        Write-Host "Converting $fileName..." -NoNewline
        
        # Read SVG content
        $svgContent = Get-Content $svgPath -Raw
        
        # Parse viewBox to get dimensions
        if ($svgContent -match 'viewBox="([^"]+)"') {
            $viewBox = $matches[1] -split '\s+'
            $width = [int]$viewBox[2]
            $height = [int]$viewBox[3]
        } else {
            $width = $size
            $height = $size
        }
        
        # Create DrawingImage from SVG
        [xml]$svgXml = $svgContent
        
        # For now, use ImageMagick or external tool
        # This is a placeholder - you'll need ImageMagick or Inkscape installed
        
        Write-Host " ?? Requires ImageMagick or Inkscape" -ForegroundColor Yellow
        
    } catch {
        Write-Host " ? Failed: $_" -ForegroundColor Red
    }
}

Write-Host "`nNote: This script requires ImageMagick or Inkscape to be installed." -ForegroundColor Yellow
Write-Host "Install ImageMagick: winget install ImageMagick.ImageMagick" -ForegroundColor Cyan
Write-Host "Or install Inkscape: winget install Inkscape.Inkscape" -ForegroundColor Cyan
