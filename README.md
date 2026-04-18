# HydroGrow

Cross-platform mobile app for managing hydroponic plants, built with **.NET MAUI**.

Track your plants, log measurements, schedule treatments and reminders, and export your data — all in one place.

## Features

- **Plant management** — add, edit, and view plants with photos and medium type (hydro/soil/etc.)
- **Measurements** — log pH, EC, TDS, temperature, and more with configurable alert ranges
- **Treatments** — record fertilizing, pruning, repotting, and other care actions
- **Reminders** — local push notifications for scheduled plant care
- **Photo gallery** — take or pick photos, view and delete per plant
- **Measurement charts** — line chart history for pH, EC, and TDS
- **Export / Import** — full data backup as ZIP (including photos); merge or replace on import

## Platforms

| Platform | Target |
|---|---|
| Android | `net10.0-android` |
| iOS | `net10.0-ios` |
| macOS | `net10.0-maccatalyst` |
| Windows | `net10.0-windows10.0.19041.0` |

## Tech Stack

| Package | Purpose |
|---|---|
| .NET MAUI 10 | Cross-platform UI framework |
| CommunityToolkit.Mvvm 8.3 | MVVM source generators (`[ObservableProperty]`, `[RelayCommand]`) |
| CommunityToolkit.Maui 12.3 | MAUI extensions |
| Microsoft.Data.Sqlite.Core 8.0 | Local SQLite database |
| Syncfusion.Maui.Toolkit 1.0 | Charts, segmented controls |
| Plugin.LocalNotification 12.0 | Local push notifications |

## Architecture

**MVVM + Repository pattern**

```
Pages/          — XAML views + code-behind
PageModels/     — ObservableObject view models
Data/           — Repositories (SQLite access)
Models/         — Entity models + enums + export DTOs
Services/       — PhotoService, NotificationService, ExportService, ImportService
Resources/      — Styles, colors, seed data
```

- Data persisted in `AppDataDirectory/AppSQLite.db3`
- Photos stored as `AppDataDirectory/photos/{guid}.jpg`
- Exports saved to `AppDataDirectory/backups/hydrogrow_export_{timestamp}.zip`
- Seed data (3 example plants) loaded once on first run from `Resources/Raw/SeedData.json`

## Build & Run

```bash
# Build
dotnet build HydroGrow.csproj

# Run on Android
dotnet run -f net10.0-android

# Run on Windows
dotnet run -f net10.0-windows10.0.19041.0
```

> There are no automated tests in this project.
