# Verifiera att AAB är i Release-läge
# Detta script kontrollerar att rätt AAB-fil är byggd

Write-Host "`n=== VERIFIERING AV AAB-FIL ===`n" -ForegroundColor Cyan

# Leta efter AAB-filer
$releaseFiles = Get-ChildItem -Path "bin\Release\net10.0-android\publish" -Filter "*.aab" -ErrorAction SilentlyContinue
$debugFiles = Get-ChildItem -Path "bin\Debug\net10.0-android\publish" -Filter "*.aab" -ErrorAction SilentlyContinue

if ($releaseFiles) {
    Write-Host "? RELEASE AAB-filer hittade:" -ForegroundColor Green
    foreach ($file in $releaseFiles) {
        Write-Host "  Fil: $($file.Name)" -ForegroundColor White
        Write-Host "  Storlek: $([math]::Round($file.Length / 1MB, 2)) MB" -ForegroundColor Cyan
        Write-Host "  Skapad: $($file.LastWriteTime)" -ForegroundColor Cyan
        Write-Host "  Sökväg: $($file.FullName)" -ForegroundColor Gray
        Write-Host ""
    }
    
    Write-Host "? Denna fil är REDO för Google Play Store!" -ForegroundColor Green
    Write-Host ""
    Write-Host "NÄSTA STEG:" -ForegroundColor Yellow
    Write-Host "1. Gå till https://play.google.com/console" -ForegroundColor White
    Write-Host "2. Välj din app (eller skapa ny)" -ForegroundColor White
    Write-Host "3. Gå till Release ? Production ? Create new release" -ForegroundColor White
    Write-Host "4. Välj 'Use Google Play App Signing'" -ForegroundColor White
    Write-Host "5. Ladda upp filen ovan" -ForegroundColor White
    Write-Host "6. Fyll i release notes och skicka in" -ForegroundColor White
    
} else {
    Write-Host "? Inga Release AAB-filer hittade!" -ForegroundColor Red
    Write-Host "Kör: .\build-android.ps1" -ForegroundColor Yellow
}

if ($debugFiles) {
    Write-Host "`n? VARNING: Debug AAB-filer hittade (använd INTE dessa för Play Store):" -ForegroundColor Yellow
    foreach ($file in $debugFiles) {
        Write-Host "  $($file.FullName)" -ForegroundColor Gray
    }
}

# Kontrollera att det inte finns några Debug-filer som är nyare
if ($releaseFiles -and $debugFiles) {
    $newestRelease = ($releaseFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1).LastWriteTime
    $newestDebug = ($debugFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1).LastWriteTime
    
    if ($newestDebug -gt $newestRelease) {
        Write-Host "`n? KRITISK VARNING: Debug-filen är nyare än Release-filen!" -ForegroundColor Red
        Write-Host "Se till att ladda upp RELEASE-filen, inte Debug!" -ForegroundColor Red
    }
}

Write-Host "`n=== VERIFIERING KLAR ===`n" -ForegroundColor Cyan
