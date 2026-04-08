namespace HydroGrow.Models;

public class Treatment
{
    public int Id { get; set; }
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();
    public int PlantId { get; set; }
    public string RecordedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string TreatmentType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string ProductUsed { get; set; } = string.Empty;
    public double? AmountMl { get; set; }

    public DateTime RecordedAtUtc =>
        DateTime.TryParse(RecordedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt
            : DateTime.MinValue;
}
