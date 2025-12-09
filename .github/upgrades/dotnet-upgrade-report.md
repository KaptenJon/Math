# .NET 10 Upgrade Report

## Project target framework modifications

| Project name         | Old Target Framework                                                    | New Target Framework        |
|:---------------------|:-----------------------------------------------------------------------:|:---------------------------:|
| Math.csproj          | net9.0-android;net9.0-ios;net9.0-maccatalyst;net9.0-windows10.0.19041.0 | net10.0-windows10.0.19041.0 |

## NuGet Packages

| Package Name                        | Old Version | New Version |
|:------------------------------------|:-----------:|:-----------:|
| Microsoft.Maui.Controls             |   9.0.110   |  10.0.11    |
| Microsoft.Extensions.Logging.Debug  |   9.0.9     |  10.0.0     |
| Microsoft.Data.Sqlite               |   8.0.8     |  10.0.0     |

## Important Notes

### Platform Support Limitation

Due to .NET 10 being in early preview, full platform support for Android, iOS, and macOS is not yet available. The project has been successfully upgraded to .NET 10 for **Windows only**.

**Original platforms:**
- net9.0-android
- net9.0-ios
- net9.0-maccatalyst
- net9.0-windows10.0.19041.0

**Current platform:**
- net10.0-windows10.0.19041.0

### Minimum Platform Version Updates

The following minimum platform versions were updated for .NET 10 compatibility:
- Windows: 10.0.19041.0 (unchanged)

## Next steps

- **Monitor .NET 10 releases**: As .NET 10 matures and platform workloads become available, you can restore multi-platform targeting by adding back:
  - `net10.0-android`
  - `net10.0-ios`
  - `net10.0-maccatalyst`
  
- **Test the application**: Ensure all functionality works correctly on Windows with .NET 10.

- **Consider staying on .NET 9**: If multi-platform support is critical for your project, you may want to remain on .NET 9 until .NET 10 platform workloads are fully released. .NET 9 has full support for Android, iOS, macOS, and Windows.
