# Debug Symbols för Google Play Console

## Vad är debug symbols?

Debug symbols innehåller information som hjälper Google Play Console att visa läsbara stack traces när appen kraschar. Utan symbols ser du bara minnesadresser - med symbols ser du exakta filnamn, metodnamn och radnummer.

## Vad genereras?

När du bygger en Release build genereras nu automatiskt:

1. **Native debug symbols** (`.symbols.zip`)
   - Innehåller debug info för native Android-kod (.so-filer)
   - Finns i: `bin\Release\net10.0-android\publish\` eller `bin\Release\net10.0-android\`
   - Storlek: Vanligtvis 1-5 MB

2. **Managed symbols** (`.pdb` filer)
   - Innehåller debug info för C#-kod
   - Finns i: `bin\Release\net10.0-android\publish\`
   - Dessa används automatiskt av .NET runtime

## Hur laddar jag upp symbols till Google Play?

### Steg 1: Bygg appen
```powershell
.\quick-build.ps1
```

Efter build visas var symbolfilerna finns.

### Steg 2: Gå till Google Play Console
1. Öppna https://play.google.com/console
2. Välj din app (Math Quest)
3. Gå till **Release** ? **Production** (eller Internal testing)
4. Klicka på **Create new release**

### Steg 3: Ladda upp AAB och symbols
1. Under "App bundles", ladda upp din `.aab`-fil
2. Scrolla ner till **"Native debug symbols"** sektionen
3. Klicka på **"Upload debug symbols"**
4. Välj din `*.symbols.zip` fil
5. Klicka **"Save"** och fortsätt med release

## Vad händer om jag inte laddar upp symbols?

Appen fungerar fortfarande normalt, men:
- Crash reports visar bara minnesadresser
- Du ser inte vilken metod eller rad som orsakade kraschen
- Svårare att debugga problem som användare rapporterar

## Exempel på crash report

### Utan symbols:
```
#00 pc 0000000000123456 /data/app/com.mathquest.app/lib/arm64/libmonosgen-2.0.so
#01 pc 0000000000234567 /data/app/com.mathquest.app/lib/arm64/libmonodroid.so
```

### Med symbols:
```
at Math.Services.GameService.GenerateQuestion (System.String behavior) [0x00123] in GameService.cs:156
at Math.Pages.QuizPage.ShowCurrent () [0x00045] in QuizPage.cs:234
at Math.Pages.QuizPage.OnSubmit (System.Object sender, System.EventArgs e) [0x00089] in QuizPage.cs:312
```

## Automatisk konfiguration

Projektet är nu konfigurerat att automatiskt generera symbols för Release builds genom följande inställningar i `Math.csproj`:

```xml
<AndroidCreateNativeDebugArchive>true</AndroidCreateNativeDebugArchive>
<DebugType>portable</DebugType>
<DebugSymbols>true</DebugSymbols>
```

## Troubleshooting

### Symbols.zip genereras inte?

1. Kontrollera att du bygger i **Release** mode
2. Kontrollera build output för felmeddelanden
3. Kör `dotnet clean` och försök igen

### Kan inte hitta symbols.zip?

Scriptet `quick-build.ps1` söker på flera platser:
- `bin\Release\net10.0-android\publish\*.symbols.zip`
- `bin\Release\net10.0-android\**\*.symbols.zip`

Kör scriptet så får du exakt sökväg.

### Google Play säger "Invalid symbols file"?

- Kontrollera att zip-filen inte är korrupt
- Bygg om appen och generera nya symbols
- Symbols måste matcha exakt den AAB du laddar upp

## Tips

?? **Ladda alltid upp symbols för varje release** - även för internal testing tracks. Detta hjälper dig hitta buggar tidigt.

?? **Spara gamla symbols** - Om användare rapporterar crashes från äldre versioner behöver du symbols från den versionen.

?? **Symbols påverkar inte app-storleken** - De finns bara i Google Play Console, inte i appen som användare laddar ner.

## Mer information

- [Google Play Console - Native Debug Symbols](https://developer.android.com/studio/build/shrink-code#native-crash-support)
- [.NET MAUI Android Debugging](https://learn.microsoft.com/dotnet/maui/android/deployment/debugging)
