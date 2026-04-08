using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace HydroGrow.Services;

public class NotificationService
{
    public Task<bool> RequestPermissionAsync()
    {
        return LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    public async Task ScheduleAsync(Reminder reminder)
    {
        if (!reminder.IsEnabledBool) return;

        var notifyTime = reminder.NextDueAtUtc.ToLocalTime();
        if (notifyTime <= DateTime.Now) return;

        var request = new NotificationRequest
        {
            NotificationId = reminder.NotificationId,
            Title = reminder.Title,
            Description = "Czas na zadbanie o roślinę!",
            BadgeNumber = 1,
            Android = new AndroidOptions
            {
                ChannelId = "hydrogrow_reminders",
                Priority = AndroidPriority.Default
            },
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = notifyTime,
                RepeatType = NotificationRepeat.No
            }
        };

        await LocalNotificationCenter.Current.Show(request);
    }

    public void Cancel(int notificationId)
    {
        LocalNotificationCenter.Current.Cancel(notificationId);
    }
}
