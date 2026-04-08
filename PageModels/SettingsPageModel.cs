using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HydroGrow.PageModels;

public partial class SettingsPageModel : ObservableObject
{
    private readonly SeedDataService _seedDataService;
    private readonly ExportService _exportService;
    private readonly ImportService _importService;
    private readonly IErrorHandler _errorHandler;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public string AppVersion => AppInfo.VersionString;

    public SettingsPageModel(
        SeedDataService seedDataService,
        ExportService exportService,
        ImportService importService,
        IErrorHandler errorHandler)
    {
        _seedDataService = seedDataService;
        _exportService = exportService;
        _importService = importService;
        _errorHandler = errorHandler;
    }

    [RelayCommand]
    private async Task Export()
    {
        IsBusy = true;
        Progress = 0;
        StatusMessage = "Eksportowanie danych...";

        try
        {
            var zipPath = await _exportService.ExportAsync(new Progress<double>(p =>
            {
                Progress = p;
                StatusMessage = p < 0.7
                    ? "Zbieranie danych..."
                    : p < 1.0
                        ? "Pakowanie plików..."
                        : "Gotowe!";
            }));

            StatusMessage = "Eksport zakończony.";

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Eksport HydroGrow",
                File = new ShareFile(zipPath)
            });
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
            StatusMessage = string.Empty;
        }
        finally
        {
            IsBusy = false;
            Progress = 0;
        }
    }

    [RelayCommand]
    private async Task Import()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Wybierz plik eksportu HydroGrow (.zip)"
            });

            if (result is null) return;

            var modeChoice = await Shell.Current.DisplayActionSheetAsync(
                "Tryb importu",
                "Anuluj",
                null,
                "Scal (dodaj nowe rośliny)",
                "Zastąp wszystko (auto-backup)");

            if (modeChoice is null or "Anuluj") return;

            var mode = modeChoice.StartsWith("Zastąp")
                ? ImportMode.ReplaceAll
                : ImportMode.MergeNew;

            if (mode == ImportMode.ReplaceAll)
            {
                bool confirmed = await Shell.Current.DisplayAlertAsync(
                    "Zastąp wszystko",
                    "Wszystkie obecne dane zostaną zastąpione danymi z importu. Przed zastąpieniem zostanie automatycznie utworzona kopia zapasowa. Kontynuować?",
                    "Zastąp", "Anuluj");

                if (!confirmed) return;
            }

            IsBusy = true;
            Progress = 0;
            StatusMessage = "Importowanie danych...";

            var zipPath = result.FullPath;
            var importResult = await _importService.ImportAsync(zipPath, mode, new Progress<double>(p =>
            {
                Progress = p;
                StatusMessage = p < 0.4
                    ? "Tworzenie kopii zapasowej..."
                    : p < 0.8
                        ? "Importowanie roślin..."
                        : "Kopiowanie zdjęć...";
            }));

            var summary = mode == ImportMode.MergeNew
                ? $"Zaimportowano {importResult.PlantsImported} roślin, pominięto {importResult.PlantsSkipped} istniejących."
                : $"Zaimportowano {importResult.PlantsImported} roślin. Kopia zapasowa: {Path.GetFileName(importResult.BackupPath)}";

            StatusMessage = "Import zakończony.";
            await Shell.Current.DisplayAlertAsync("Import zakończony", summary, "OK");
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
            StatusMessage = string.Empty;
        }
        finally
        {
            IsBusy = false;
            Progress = 0;
        }
    }

    [RelayCommand]
    private async Task ResetData()
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Resetuj dane",
            "Czy na pewno chcesz usunąć wszystkie dane i załadować dane testowe? Tej operacji nie można cofnąć.",
            "Resetuj", "Anuluj");

        if (!confirmed) return;

        IsBusy = true;
        StatusMessage = "Resetowanie danych...";

        try
        {
            Preferences.Default.Remove("is_seeded");
            await _seedDataService.LoadSeedDataAsync();
            Preferences.Default.Set("is_seeded", true);
            StatusMessage = "Dane zostały zresetowane.";
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
            StatusMessage = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        Application.Current!.UserAppTheme =
            Application.Current.UserAppTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
    }
}
