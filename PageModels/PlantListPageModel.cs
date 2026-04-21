using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HydroGrow.PageModels;

[QueryProperty(nameof(Refresh), "refresh")]
public partial class PlantListPageModel : ObservableObject
{
    private readonly PlantRepository _plantRepository;
    private readonly IErrorHandler _errorHandler;

    private List<Plant> _allPlants = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private List<Plant> _plants = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedMediumFilter = string.Empty;

    public List<string> MediumFilters { get; } =
        new[] { "Wszystkie" }
        .Concat(Enum.GetValues<MediumType>().Select(m => m.ToDisplayString()))
        .ToList();

    public PlantListPageModel(PlantRepository plantRepository, IErrorHandler errorHandler)
    {
        _plantRepository = plantRepository;
        _errorHandler = errorHandler;
        _selectedMediumFilter = MediumFilters[0];
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public string? Refresh
    {
        set
        {
            if (value == "true")
                InitializeAsync().FireAndForgetSafeAsync(_errorHandler);
        }
    }

    public Task InitializeAsync() => LoadAsync();
    public IAsyncRelayCommand RefreshCommand { get; }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedMediumFilterChanged(string value) => ApplyFilter();

    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        IsRefreshing = true;
        
        try
        {
            _allPlants = await _plantRepository.ListAsync();
            ApplyFilter();
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

    private void ApplyFilter()
    {
        var filtered = _allPlants.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var lower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Name.ToLowerInvariant().Contains(lower) ||
                p.Species.ToLowerInvariant().Contains(lower) ||
                p.LocationName.ToLowerInvariant().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(SelectedMediumFilter) && SelectedMediumFilter != "Wszystkie")
        {
            filtered = filtered.Where(p =>
                p.GetMediumDisplayName() == SelectedMediumFilter);
        }

        Plants = filtered.ToList();
    }

    [RelayCommand]
    private async Task NavigateToPlant(Plant plant)
    {
        await Shell.Current.GoToAsync($"plant?id={plant.Id}");
    }

    [RelayCommand]
    private void SetMediumFilter(string filter)
    {
        SelectedMediumFilter = filter;
    }

    [RelayCommand]
    private async Task AddPlant()
    {
        await Shell.Current.GoToAsync("plant-edit");
    }
}
