# Snabbscript för att skapa keystore och bygga signerad AAB

Write-Host "`n=== SKAPA UPLOAD-NYCKEL OCH BYGG AAB ===`n" -ForegroundColor Cyan

$KeystoreFile = "mathquest-upload.keystore"
$KeyAlias = "upload"

# Kontrollera om keystore redan finns
if (Test-Path $KeystoreFile) {
    Write-Host "? Keystore finns redan: $KeystoreFile" -ForegroundColor Green
} else {
    Write-Host "Skapar ny upload-keystore..." -ForegroundColor Yellow
    Write-Host "Du kommer att bli frågad om:" -ForegroundColor White
    Write-Host "  1. Keystore lösenord (välj något enkelt, t.ex. 'upload123')" -ForegroundColor White
    Write-Host "  2. Ditt för- och efternamn" -ForegroundColor White
    Write-Host "  3. Tryck Enter för resten av frågorna (kan lämnas tomma)" -ForegroundColor White
    Write-Host "  4. Key lösenord - tryck bara Enter för samma lösenord`n" -ForegroundColor White
    
    # Skapa keystore
    & keytool -genkey -v -keystore $KeystoreFile -alias $KeyAlias -keyalg RSA -keysize 2048 -validity 10000
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`nMisslyckades att skapa keystore!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`n? Keystore skapad!" -ForegroundColor Green
}

# Fråga efter lösenord
Write-Host "`nAnge lösenordet du just skapade (eller har):" -ForegroundColor Yellow
$password = Read-Host -AsSecureString "Keystore lösenord"
$passwordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))

# Sätt miljövariabler
$env:ANDROID_KEYSTORE_PASSWORD = $passwordPlain
$env:ANDROID_KEY_PASSWORD = $passwordPlain

Write-Host "`n? Lösenord satt!" -ForegroundColor Green
Write-Host "`nBygger signerad AAB...`n" -ForegroundColor Cyan

# Uppdatera csproj temporärt för att använda vår keystore
$csprojContent = Get-Content "Math.csproj" -Raw
$newContent = $csprojContent -replace '<AndroidSigningKeyStore>mathquest.keystore</AndroidSigningKeyStore>', "<AndroidSigningKeyStore>$KeystoreFile</AndroidSigningKeyStore>"
$newContent = $newContent -replace '<AndroidSigningKeyAlias>mathquest</AndroidSigningKeyAlias>', "<AndroidSigningKeyAlias>$KeyAlias</AndroidSigningKeyAlias>"
Set-Content "Math.csproj" -Value $newContent

# Bygg
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=aab -p:AndroidKeyStore=true

# Återställ csproj
Set-Content "Math.csproj" -Value $csprojContent

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n? BUILD LYCKADES!`n" -ForegroundColor Green
    
    # Hitta AAB-filen
    $publishPath = "bin\Release\net10.0-android\publish"
    $aabFiles = Get-ChildItem -Path $publishPath -Filter "*.aab"
    
    if ($aabFiles) {
        Write-Host "SIGNERAD AAB-fil skapad:" -ForegroundColor Green
        foreach ($file in $aabFiles) {
            Write-Host "  ?? Fil: $($file.Name)" -ForegroundColor White
            Write-Host "  ?? Storlek: $([math]::Round($file.Length / 1MB, 2)) MB" -ForegroundColor Cyan
            Write-Host "  ?? Sökväg: $($file.FullName)" -ForegroundColor Gray
        }
        
        # Hitta symbolfilerna
        Write-Host "`nDebug symbols (för Google Play Console):" -ForegroundColor Yellow
        
        # Leta efter native debug symbols (.zip eller symbols.zip)
        $symbolFiles = Get-ChildItem -Path $publishPath -Filter "*symbols.zip" -ErrorAction SilentlyContinue
        if (-not $symbolFiles) {
            $symbolFiles = Get-ChildItem -Path $publishPath -Filter "*.symbols.zip" -ErrorAction SilentlyContinue
        }
        
        # Leta även i bin\Release\net10.0-android\ för symbols
        if (-not $symbolFiles) {
            $symbolFiles = Get-ChildItem -Path "bin\Release\net10.0-android" -Filter "*.symbols.zip" -Recurse -ErrorAction SilentlyContinue
        }
        
        if ($symbolFiles) {
            foreach ($symbolFile in $symbolFiles) {
                Write-Host "  ?? Symbol fil: $($symbolFile.Name)" -ForegroundColor White
                Write-Host "     Storlek: $([math]::Round($symbolFile.Length / 1MB, 2)) MB" -ForegroundColor Cyan
                Write-Host "     Sökväg: $($symbolFile.FullName)" -ForegroundColor Gray
            }
        } else {
            Write-Host "  ??  Ingen symbols.zip hittades - kontrollera build output" -ForegroundColor Yellow
            Write-Host "     Sökte i: $publishPath" -ForegroundColor Gray
        }
        
        # Leta även efter PDB-filer (managed symbols)
        $pdbFiles = Get-ChildItem -Path $publishPath -Filter "*.pdb" -ErrorAction SilentlyContinue
        if ($pdbFiles) {
            Write-Host "`n  ?? Managed debug symbols (.pdb):" -ForegroundColor Cyan
            foreach ($pdb in $pdbFiles | Select-Object -First 3) {
                Write-Host "     - $($pdb.Name)" -ForegroundColor Gray
            }
            if ($pdbFiles.Count -gt 3) {
                Write-Host "     ... och $($pdbFiles.Count - 3) filer till" -ForegroundColor Gray
            }
        }
        
        Write-Host "`n? REDO FÖR GOOGLE PLAY!" -ForegroundColor Green
        Write-Host "`nNästa steg:" -ForegroundColor Yellow
        Write-Host "1. Gå till https://play.google.com/console" -ForegroundColor White
        Write-Host "2. Skapa ny release" -ForegroundColor White
        Write-Host "3. Välj 'Use Google Play App Signing'" -ForegroundColor White
        Write-Host "4. Ladda upp AAB-filen ovan" -ForegroundColor White
        Write-Host "5. ?? Ladda upp symbols.zip (om tillgänglig) under 'Native debug symbols'" -ForegroundColor Cyan
        Write-Host "   Detta ger bättre crash reports med stack traces!" -ForegroundColor Gray
    }
} else {
    Write-Host "`n? Build misslyckades!" -ForegroundColor Red
    Write-Host "Kontrollera Output-fönstret i Visual Studio för detaljer" -ForegroundColor Yellow
}

Write-Host ""
