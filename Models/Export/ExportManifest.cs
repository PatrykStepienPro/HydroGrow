namespace HydroGrow.Models.Export;

public class ExportManifest
{
    public int SchemaVersion { get; set; } = 1;
    public string AppVersion { get; set; } = "1.0";
    public string ExportedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public int PlantCount { get; set; }
    public int MeasurementCount { get; set; }
    public int TreatmentCount { get; set; }
    public int PhotoCount { get; set; }
    public int ReminderCount { get; set; }
}
