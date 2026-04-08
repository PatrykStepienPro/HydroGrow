using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HydroGrow.PageModels;

[QueryProperty(nameof(PlantId), "plantId")]
public partial class AddTreatmentPageModel : ObservableObject, IQueryAttributable
{
    private readonly PlantRepository _plantRepository;
    private readonly TreatmentRepository _treatmentRepository;
    private readonly ReminderRepository _reminderRepository;
    private readonly NotificationService _notificationService;
    private readonly IErrorHandler _errorHandler;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private List<Plant> _plants = [];
    [ObservableProperty] private Plant? _selectedPlant;
    [ObservableProperty] private DateTime _recordedAt = DateTime.Now;
    [ObservableProperty] private TreatmentTypeItem? _selectedTreatmentType;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _productUsed = string.Empty;
    [ObservableProperty] private string _amountMl = string.Empty;
    [ObservableProperty] private bool _showProductFields;

    public List<TreatmentTypeItem> TreatmentTypes { get; } =
        TreatmentTypeExtensions.All()
            .Select(t => new TreatmentTypeItem(t))
            .ToList();

    public int PlantId { get; set; }

    public AddTreatmentPageModel(
        PlantRepository plantRepository,
        TreatmentRepository treatmentRepository,
        ReminderRepository reminderRepository,
        NotificationService notificationService,
        IErrorHandler errorHandler)
    {
        _plantRepository = plantRepository;
        _treatmentRepository = treatmentRepository;
        _reminderRepository = reminderRepository;
        _notificationService = notificationService;
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

    partial void OnSelectedTreatmentTypeChanged(TreatmentTypeItem? value)
    {
        ShowProductFields = value?.Type is TreatmentType.Fertilization or TreatmentType.NutrientChange;
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
    private void SelectTreatmentType(TreatmentTypeItem item)
    {
        SelectedTreatmentType = item;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (SelectedPlant is null)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Wybierz roślinę.", "OK");
            return;
        }
        if (SelectedTreatmentType is null)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Wybierz typ zabiegu.", "OK");
            return;
        }

        var treatment = new Treatment
        {
            PlantId = SelectedPlant.Id,
            RecordedAt = RecordedAt.ToUniversalTime().ToString("O"),
            TreatmentType = SelectedTreatmentType.Type.ToString(),
            Notes = Notes?.Trim() ?? string.Empty,
            ProductUsed = ProductUsed?.Trim() ?? string.Empty,
            AmountMl = ParseDouble(AmountMl)
        };

        IsBusy = true;
        try
        {
            await _treatmentRepository.SaveItemAsync(treatment);

            // Advance reminders for this treatment type
            var reminders = await _reminderRepository.ListAsync(SelectedPlant.Id);
            var matchingReminders = reminders.Where(r =>
                r.ReminderType == SelectedTreatmentType.Type.ToString() &&
                r.RecurrenceDays > 0).ToList();

            foreach (var reminder in matchingReminders)
            {
                reminder.LastTriggeredAt = DateTime.UtcNow.ToString("O");
                reminder.NextDueAt = DateTime.UtcNow.AddDays(reminder.RecurrenceDays).ToString("O");
                await _reminderRepository.SaveItemAsync(reminder);
                _notificationService.Cancel(reminder.NotificationId);
                await _notificationService.ScheduleAsync(reminder);
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

public class TreatmentTypeItem
{
    public TreatmentType Type { get; }
    public string DisplayName { get; }

    public TreatmentTypeItem(TreatmentType type)
    {
        Type = type;
        DisplayName = type.ToDisplayString();
    }
}
