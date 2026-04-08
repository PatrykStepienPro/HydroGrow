using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HydroGrow.PageModels;

public partial class DashboardPageModel : ObservableObject
{
    private readonly PlantRepository _plantRepository;
    private readonly MeasurementRepository _measurementRepository;
    private readonly TreatmentRepository _treatmentRepository;
    private readonly MeasurementRangeRepository _rangeRepository;
    private readonly SeedDataService _seedDataService;
    private readonly IErrorHandler _errorHandler;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private List<Plant> _plantsNeedingAttention = [];

    [ObservableProperty]
    private List<ActivityItem> _recentActivity = [];

    [ObservableProperty]
    private int _totalPlants;

    [ObservableProperty]
    private int _alertCount;

    public DashboardPageModel(
        PlantRepository plantRepository,
        MeasurementRepository measurementRepository,
        TreatmentRepository treatmentRepository,
        MeasurementRangeRepository rangeRepository,
        SeedDataService seedDataService,
        IErrorHandler errorHandler)
    {
        _plantRepository = plantRepository;
        _measurementRepository = measurementRepository;
        _treatmentRepository = treatmentRepository;
        _rangeRepository = rangeRepository;
        _seedDataService = seedDataService;
        _errorHandler = errorHandler;
    }

    [RelayCommand]
    private Task Appearing() => LoadAsync(seedIfNeeded: true);

    [RelayCommand]
    private Task Refresh() => LoadAsync(seedIfNeeded: false);

    partial void OnIsRefreshingChanged(bool value)
    {
        if (value)
            LoadAsync(false).FireAndForgetSafeAsync(_errorHandler);
    }

    private async Task LoadAsync(bool seedIfNeeded)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            if (seedIfNeeded && !Preferences.Default.Get("is_seeded", false))
            {
                await _seedDataService.LoadSeedDataAsync();
                Preferences.Default.Set("is_seeded", true);
            }

            var plants = await _plantRepository.ListAsync();
            TotalPlants = plants.Count;

            var alertPlants = new List<Plant>();
            var activityItems = new List<ActivityItem>();

            foreach (var plant in plants)
            {
                var latest = await _measurementRepository.GetLatestAsync(plant.Id);
                var range = await _rangeRepository.GetAsync(plant.Id);

                if (latest != null && range != null && HasAlerts(latest, range))
                    alertPlants.Add(plant);

                if (latest != null)
                {
                    activityItems.Add(new ActivityItem
                    {
                        PlantId = plant.Id,
                        PlantName = plant.Name,
                        Description = "Pomiar",
                        RecordedAtUtc = latest.RecordedAtUtc,
                        IsAlert = range != null && HasAlerts(latest, range)
                    });
                }

                var latestTreatment = (await _treatmentRepository.ListAsync(plant.Id, 1)).FirstOrDefault();
                if (latestTreatment != null)
                {
                    activityItems.Add(new ActivityItem
                    {
                        PlantId = plant.Id,
                        PlantName = plant.Name,
                        Description = FormatTreatmentType(latestTreatment.TreatmentType),
                        RecordedAtUtc = latestTreatment.RecordedAtUtc,
                        IsAlert = false
                    });
                }
            }

            PlantsNeedingAttention = alertPlants;
            AlertCount = alertPlants.Count;
            RecentActivity = activityItems
                .OrderByDescending(a => a.RecordedAtUtc)
                .Take(15)
                .ToList();
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    private static bool HasAlerts(Measurement m, MeasurementRange r) =>
        !r.IsPhInRange(m.Ph) ||
        !r.IsEcInRange(m.Ec) ||
        !r.IsTdsInRange(m.Tds) ||
        !r.IsWaterTempInRange(m.WaterTempC) ||
        !r.IsAmbientTempInRange(m.AmbientTempC) ||
        !r.IsHumidityInRange(m.HumidityPct);

    private static string FormatTreatmentType(string type)
    {
        if (Enum.TryParse<TreatmentType>(type, out var t))
            return t.ToDisplayString();
        return type;
    }

    [RelayCommand]
    private async Task NavigateToPlant(Plant plant)
    {
        await Shell.Current.GoToAsync($"plant?id={plant.Id}");
    }
}

public class ActivityItem
{
    public int PlantId { get; set; }
    public string PlantName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime RecordedAtUtc { get; set; }
    public bool IsAlert { get; set; }

    public string RelativeTime => RecordedAtUtc.ToRelativeString();
}
