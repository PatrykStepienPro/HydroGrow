using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HydroGrow.PageModels;

public partial class RemindersPageModel : ObservableObject
{
    private readonly ReminderRepository _reminderRepository;
    private readonly PlantRepository _plantRepository;
    private readonly NotificationService _notificationService;
    private readonly IErrorHandler _errorHandler;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private List<ReminderViewModel> _reminders = [];

    public RemindersPageModel(
        ReminderRepository reminderRepository,
        PlantRepository plantRepository,
        NotificationService notificationService,
        IErrorHandler errorHandler)
    {
        _reminderRepository = reminderRepository;
        _plantRepository = plantRepository;
        _notificationService = notificationService;
        _errorHandler = errorHandler;
    }

    [RelayCommand]
    private Task Appearing() => LoadAsync();

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    partial void OnIsRefreshingChanged(bool value)
    {
        if (value)
            LoadAsync().FireAndForgetSafeAsync(_errorHandler);
    }

    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var reminders = await _reminderRepository.ListAsync();
            var plants = await _plantRepository.ListAsync();
            var plantDict = plants.ToDictionary(p => p.Id, p => p.Name);

            Reminders = reminders.Select(r => new ReminderViewModel
            {
                Reminder = r,
                PlantName = r.PlantId.HasValue && plantDict.TryGetValue(r.PlantId.Value, out var name) ? name : "Globalne"
            }).ToList();
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
    private async Task DeleteReminder(Reminder reminder)
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Usuń przypomnienie",
            $"Usunąć przypomnienie \"{reminder.Title}\"?",
            "Usuń", "Anuluj");

        if (!confirmed) return;

        _notificationService.Cancel(reminder.NotificationId);
        await _reminderRepository.DeleteItemAsync(reminder);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ToggleReminder(Reminder reminder)
    {
        reminder.IsEnabled = reminder.IsEnabled == 1 ? 0 : 1;
        await _reminderRepository.SaveItemAsync(reminder);

        if (reminder.IsEnabledBool)
            await _notificationService.ScheduleAsync(reminder);
        else
            _notificationService.Cancel(reminder.NotificationId);

        await LoadAsync();
    }

    [RelayCommand]
    private async Task NavigateToPlant(ReminderViewModel vm)
    {
        if (vm.Reminder.PlantId.HasValue)
            await Shell.Current.GoToAsync($"plant?id={vm.Reminder.PlantId.Value}");
    }
}

public class ReminderViewModel
{
    public Reminder Reminder { get; set; } = new();
    public string PlantName { get; set; } = string.Empty;
    public string RelativeTime => Reminder.NextDueAtUtc.ToRelativeFutureString();
    public bool IsOverdue => Reminder.IsOverdue;
}
