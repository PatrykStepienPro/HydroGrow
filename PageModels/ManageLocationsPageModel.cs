using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location = HydroGrow.Models.Location;
using Microsoft.Data.Sqlite;

namespace HydroGrow.PageModels;

public partial class ManageLocationsPageModel : ObservableObject
{
    private readonly LocationRepository _repo;
    private readonly IErrorHandler _errorHandler;

    public ObservableCollection<Location> Locations { get; } = [];

    [ObservableProperty]
    private string _newLocationName = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    public ManageLocationsPageModel(LocationRepository repo, IErrorHandler errorHandler)
    {
        _repo = repo;
        _errorHandler = errorHandler;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public IAsyncRelayCommand RefreshCommand { get; }

    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        IsRefreshing = true;
        try
        {
            var list = await _repo.ListAsync();
            Locations.Clear();
            foreach (var l in list) Locations.Add(l);
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

    [RelayCommand]
    private async Task AddLocation()
    {
        var name = NewLocationName.Trim();
        if (string.IsNullOrEmpty(name)) return;

        if (Locations.Any(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Taka lokalizacja już istnieje.", "OK");
            return;
        }

        var location = new Location { Name = name };
        try
        {
            await _repo.SaveItemAsync(location);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Taka lokalizacja już istnieje.", "OK");
            return;
        }
        Locations.Add(location);
        NewLocationName = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteLocation(Location location)
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Usuń lokalizację",
            $"Usunąć \"{location.Name}\"? Rośliny przypisane do tej lokalizacji stracą lokalizację.",
            "Usuń", "Anuluj");
        if (!confirmed) return;

        await _repo.DeleteItemAsync(location);
        Locations.Remove(location);
    }
}
