namespace HydroGrow.Models;

public class Measurement
{
    public int Id { get; set; }
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();
    public int PlantId { get; set; }
    public string RecordedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public double? Ph { get; set; }
    public double? Ec { get; set; }
    public double? Tds { get; set; }
    public double? WaterTempC { get; set; }
    public double? AmbientTempC { get; set; }
    public double? HumidityPct { get; set; }
    public string? WaterLevel { get; set; }
    public string Notes { get; set; } = string.Empty;

    public DateTime RecordedAtUtc =>
        DateTime.TryParse(RecordedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt
            : DateTime.MinValue;

    public bool HasAnyValue =>
        Ph.HasValue || Ec.HasValue || Tds.HasValue ||
        WaterTempC.HasValue || AmbientTempC.HasValue ||
        HumidityPct.HasValue || WaterLevel != null;
}
