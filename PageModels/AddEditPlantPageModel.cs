using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HydroGrow.PageModels;

[QueryProperty(nameof(PlantId), "id")]
public partial class AddEditPlantPageModel : ObservableObject, IQueryAttributable
{
    private readonly PlantRepository _plantRepository;
    private readonly PhotoRepository _photoRepository;
    private readonly PhotoService _photoService;
    private readonly IErrorHandler _errorHandler;

    private Plant _plant = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isNew = true;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _species = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private string _selectedMediumType = MediumType.LECA.ToDisplayString();

    [ObservableProperty]
    private DateTime _acquiredDate = DateTime.Today;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string? _thumbnailFullPath;

    public List<string> MediumTypes { get; } =
        Enum.GetValues<MediumType>().Select(m => m.ToDisplayString()).ToList();

    public int PlantId { get; set; }

    public string PageTitle => IsNew ? "Nowa roślina" : "Edytuj roślinę";

    public AddEditPlantPageModel(
        PlantRepository plantRepository,
        PhotoRepository photoRepository,
        PhotoService photoService,
        IErrorHandler errorHandler)
    {
        _plantRepository = plantRepository;
        _photoRepository = photoRepository;
        _photoService = photoService;
        _errorHandler = errorHandler;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) &&
            int.TryParse(idObj?.ToString(), out var id) && id > 0)
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
            if (PlantId > 0)
            {
                _plant = await _plantRepository.GetAsync(PlantId) ?? new Plant();
                IsNew = false;
            }
            else
            {
                _plant = new Plant();
                IsNew = true;
            }

            Name = _plant.Name;
            Species = _plant.Species;
            Location = _plant.Location;
            Notes = _plant.Notes;

            if (Enum.TryParse<MediumType>(_plant.MediumType, out var mt))
                SelectedMediumType = mt.ToDisplayString();

            if (DateTime.TryParse(_plant.AcquiredDate, out var date))
                AcquiredDate = date;

            if (_plant.ThumbnailPhotoId.HasValue)
            {
                var photo = await _photoRepository.GetAsync(_plant.ThumbnailPhotoId.Value);
                if (photo != null)
                    ThumbnailFullPath = _photoService.GetFullPath(photo.FilePath);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PickThumbnail()
    {
        var fileName = await _photoService.PickPhotoAsync();
        if (fileName is null) return;

        var photo = new PlantPhoto
        {
            PlantId = _plant.Id,
            FilePath = fileName,
            TakenAt = DateTime.UtcNow.ToString("O"),
            SortOrder = 0
        };

        await _photoRepository.SaveItemAsync(photo);

        _plant.ThumbnailPhotoId = photo.Id;

        if (_plant.Id > 0)
            await _plantRepository.SaveItemAsync(_plant);

        ThumbnailFullPath = _photoService.GetFullPath(fileName);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Nazwa rośliny jest wymagana.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            _plant.Name = Name.Trim();
            _plant.Species = Species?.Trim() ?? string.Empty;
            _plant.Location = Location?.Trim() ?? string.Empty;
            _plant.Notes = Notes?.Trim() ?? string.Empty;
            _plant.AcquiredDate = AcquiredDate.ToString("yyyy-MM-dd");
            _plant.MediumType = MediumTypeExtensions.FromDisplayString(SelectedMediumType).ToString();

            await _plantRepository.SaveItemAsync(_plant);

            // For new plants: update photo's PlantId now that the plant has a real Id
            if (_plant.ThumbnailPhotoId.HasValue)
            {
                var thumbnail = await _photoRepository.GetAsync(_plant.ThumbnailPhotoId.Value);
                if (thumbnail != null && thumbnail.PlantId != _plant.Id)
                {
                    thumbnail.PlantId = _plant.Id;
                    await _photoRepository.SaveItemAsync(thumbnail);
                }
            }

            await Shell.Current.GoToAsync($"..?refresh=true");
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
    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }
}
