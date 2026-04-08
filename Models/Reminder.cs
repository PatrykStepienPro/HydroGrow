namespace HydroGrow.Models;

public class Reminder
{
    public int Id { get; set; }
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();
    public int? PlantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReminderType { get; set; } = string.Empty;
    public int RecurrenceDays { get; set; } = 7;
    public string NextDueAt { get; set; } = DateTime.UtcNow.AddDays(7).ToString("O");
    public string? LastTriggeredAt { get; set; }
    public int IsEnabled { get; set; } = 1;
    public int NotificationId { get; set; }

    public DateTime NextDueAtUtc =>
        DateTime.TryParse(NextDueAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt
            : DateTime.MaxValue;

    public bool IsOverdue => NextDueAtUtc < DateTime.UtcNow;
    public bool IsEnabledBool => IsEnabled == 1;
}
