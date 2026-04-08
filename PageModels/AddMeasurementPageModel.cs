using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HydroGrow.PageModels;

[QueryProperty(nameof(PlantId), "plantId")]
public partial class AddMeasurementPageModel : ObservableObject, IQueryAttributable
{
    private readonly PlantRepository _plantRepository;
    private readonly MeasurementRepository _measurementRepository;
    private readonly MeasurementRangeRepository _rangeRepository;
    private readonly IErrorHandler _errorHandler;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private List<Plant> _plants = [];
    [ObservableProperty] private Plant? _selectedPlant;
    [ObservableProperty] private DateTime _recordedAt = DateTime.Now;

    // Measurement fields as strings for entry binding
    [ObservableProperty] private string _ph = string.Empty;
    [ObservableProperty] private string _ec = string.Empty;
    [ObservableProperty] private string _tds = string.Empty;
    [ObservableProperty] private string _waterTempC = string.Empty;
    [ObservableProperty] private string _ambientTempC = string.Empty;
    [ObservableProperty] private string _humidityPct = string.Empty;
    [ObservableProperty] private string _selectedWaterLevel = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;

    public List<string> WaterLevels { get; } = ["", "Niski", "Średni", "Wysoki", "Pełny"];

    public int PlantId { get; set; }

    public AddMeasurementPageModel(
        PlantRepository plantRepository,
        MeasurementRepository measurementRepository,
        MeasurementRangeRepository rangeRepository,
        IErrorHandler errorHandler)
    {
        _plantRepository = plantRepository;
        _measurementRepository = measurementRepository;
        _rangeRepository = rangeRepository;
        _errorHandler = errorHandler;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("plantId", out var idObj) &&
            int.TryParse(idObj?.ToString(), out var id))
        {
            PlantId = id;
        }
        LoadAsync().FireAndForgetSafeAsync(_errorHandler);
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Plants = await _plantRepository.ListAsync();
            if (PlantId > 0)
                SelectedPlant = Plants.FirstOrDefault(p => p.Id == PlantId);
            else if (Plants.Count == 1)
                SelectedPlant = Plants[0];
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (SelectedPlant is null)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Wybierz roślinę.", "OK");
            return;
        }

        var measurement = new Measurement
        {
            PlantId = SelectedPlant.Id,
            RecordedAt = RecordedAt.ToUniversalTime().ToString("O"),
            Ph = ParseDouble(Ph),
            Ec = ParseDouble(Ec),
            Tds = ParseDouble(Tds),
            WaterTempC = ParseDouble(WaterTempC),
            AmbientTempC = ParseDouble(AmbientTempC),
            HumidityPct = ParseDouble(HumidityPct),
            WaterLevel = string.IsNullOrWhiteSpace(SelectedWaterLevel) ? null : SelectedWaterLevel,
            Notes = Notes?.Trim() ?? string.Empty
        };

        if (!measurement.HasAnyValue)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Podaj przynajmniej jeden parametr.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            await _measurementRepository.SaveItemAsync(measurement);

            // Check alerts
            var range = await _rangeRepository.GetAsync(SelectedPlant.Id);
            if (range != null)
            {
                var alerts = new List<string>();
                if (!range.IsPhInRange(measurement.Ph)) alerts.Add($"pH: {measurement.Ph:F1}");
                if (!range.IsEcInRange(measurement.Ec)) alerts.Add($"EC: {measurement.Ec:F2}");
                if (!range.IsTdsInRange(measurement.Tds)) alerts.Add($"TDS: {measurement.Tds:F0} ppm");
                if (!range.IsWaterTempInRange(measurement.WaterTempC)) alerts.Add($"Temp. wody: {measurement.WaterTempC:F1}°C");
                if (!range.IsHumidityInRange(measurement.HumidityPct)) alerts.Add($"Wilgotność: {measurement.HumidityPct:F0}%");

                if (alerts.Any())
                    await Shell.Current.DisplayAlert("⚠ Alert",
                        $"Parametry poza zakresem:\n{string.Join("\n", alerts)}", "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Cancel() => await Shell.Current.GoToAsync("..");

    private static double? ParseDouble(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return double.TryParse(s.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}
