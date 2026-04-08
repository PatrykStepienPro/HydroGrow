using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HydroGrow.PageModels;

[QueryProperty(nameof(PlantId), "id")]
public partial class PlantDetailPageModel : ObservableObject, IQueryAttributable
{
    private readonly PlantRepository _plantRepository;
    private readonly MeasurementRepository _measurementRepository;
    private readonly TreatmentRepository _treatmentRepository;
    private readonly PhotoRepository _photoRepository;
    private readonly MeasurementRangeRepository _rangeRepository;
    private readonly PhotoService _photoService;
    private readonly IErrorHandler _errorHandler;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private Plant _plant = new();
    [ObservableProperty] private Measurement? _latestMeasurement;
    [ObservableProperty] private List<Measurement> _recentMeasurements = [];
    [ObservableProperty] private List<Treatment> _recentTreatments = [];
    [ObservableProperty] private List<PlantPhoto> _photos = [];
    [ObservableProperty] private MeasurementRange? _measurementRange;
    [ObservableProperty] private bool _hasAlerts;
    [ObservableProperty] private string? _thumbnailFullPath;
    [ObservableProperty] private bool _showDeleteButton;

    public int PlantId { get; set; }

    public AddEditPlantPageModel? EditPageModel { get; private set; }

    public PlantDetailPageModel(
        PlantRepository plantRepository,
        MeasurementRepository measurementRepository,
        TreatmentRepository treatmentRepository,
        PhotoRepository photoRepository,
        MeasurementRangeRepository rangeRepository,
        PhotoService photoService,
        IErrorHandler errorHandler)
    {
        _plantRepository = plantRepository;
        _measurementRepository = measurementRepository;
        _treatmentRepository = treatmentRepository;
        _photoRepository = photoRepository;
        _rangeRepository = rangeRepository;
        _photoService = photoService;
        _errorHandler = errorHandler;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) &&
            int.TryParse(idObj?.ToString(), out var id))
        {
            PlantId = id;
        }

        LoadAsync().FireAndForgetSafeAsync(_errorHandler);
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var plant = await _plantRepository.GetAsync(PlantId);
            if (plant is null) return;

            Plant = plant;
            ShowDeleteButton = true;

            var measurementsTask = _measurementRepository.ListAsync(PlantId, 20);
            var treatmentsTask = _treatmentRepository.ListAsync(PlantId, 10);
            var photosTask = _photoRepository.ListAsync(PlantId);
            var rangeTask = _rangeRepository.GetAsync(PlantId);

            await Task.WhenAll(measurementsTask, treatmentsTask, photosTask, rangeTask);

            RecentMeasurements = await measurementsTask;
            RecentTreatments = await treatmentsTask;
            Photos = await photosTask;
            MeasurementRange = await rangeTask;
            LatestMeasurement = RecentMeasurements.FirstOrDefault();

            // Resolve thumbnail
            if (plant.ThumbnailPhotoId.HasValue)
            {
                var thumb = await _photoRepository.GetAsync(plant.ThumbnailPhotoId.Value);
                if (thumb != null)
                    ThumbnailFullPath = _photoService.GetFullPath(thumb.FilePath);
            }
            else if (Photos.Any())
            {
                ThumbnailFullPath = _photoService.GetFullPath(Photos.First().FilePath);
            }

            // Check alerts
            if (LatestMeasurement != null && MeasurementRange != null)
                HasAlerts = CheckAlerts(LatestMeasurement, MeasurementRange);
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
    private async Task Edit()
    {
        await Shell.Current.GoToAsync($"plant-edit?id={PlantId}");
    }

    [RelayCommand]
    private async Task AddMeasurement()
    {
        await Shell.Current.GoToAsync($"plant-measure?plantId={PlantId}");
    }

    [RelayCommand]
    private async Task AddTreatment()
    {
        await Shell.Current.GoToAsync($"plant-treat?plantId={PlantId}");
    }

    [RelayCommand]
    private async Task AddPhoto()
    {
        var fileName = await _photoService.PickPhotoAsync();
        if (fileName is null) return;

        var photo = new PlantPhoto
        {
            PlantId = PlantId,
            FilePath = fileName,
            TakenAt = DateTime.UtcNow.ToString("O"),
            SortOrder = Photos.Count
        };
        await _photoRepository.SaveItemAsync(photo);

        // Set as thumbnail if plant has none
        if (!Plant.ThumbnailPhotoId.HasValue)
        {
            Plant.ThumbnailPhotoId = photo.Id;
            await _plantRepository.SaveItemAsync(Plant);
            ThumbnailFullPath = _photoService.GetFullPath(fileName);
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeletePhoto(PlantPhoto photo)
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Usuń zdjęcie", "Usunąć to zdjęcie?", "Usuń", "Anuluj");
        if (!confirmed) return;

        _photoService.DeletePhoto(photo.FilePath);
        await _photoRepository.DeleteItemAsync(photo);

        if (Plant.ThumbnailPhotoId == photo.Id)
        {
            Plant.ThumbnailPhotoId = null;
            await _plantRepository.SaveItemAsync(Plant);
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task Delete()
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Usuń roślinę",
            $"Czy na pewno chcesz usunąć \"{Plant.Name}\"? Wszystkie dane zostaną trwale usunięte.",
            "Usuń", "Anuluj");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            // Delete photos from disk first
            var photos = await _photoRepository.ListAsync(PlantId);
            foreach (var p in photos)
                _photoService.DeletePhoto(p.FilePath);

            // Delete all related data
            await Task.WhenAll(
                _measurementRepository.DeleteByPlantAsync(PlantId),
                _treatmentRepository.DeleteByPlantAsync(PlantId),
                _photoRepository.DeleteByPlantAsync(PlantId),
                _rangeRepository.DeleteAsync(PlantId));

            await _plantRepository.DeleteItemAsync(Plant);
            await Shell.Current.GoToAsync($"//plants");
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
    private async Task SaveNotes()
    {
        try
        {
            Plant.Notes = Plant.Notes ?? string.Empty;
            await _plantRepository.SaveItemAsync(Plant);
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
        }
    }

    private static bool CheckAlerts(Measurement m, MeasurementRange r) =>
        !r.IsPhInRange(m.Ph) ||
        !r.IsEcInRange(m.Ec) ||
        !r.IsTdsInRange(m.Tds) ||
        !r.IsWaterTempInRange(m.WaterTempC) ||
        !r.IsAmbientTempInRange(m.AmbientTempC) ||
        !r.IsHumidityInRange(m.HumidityPct);
}
