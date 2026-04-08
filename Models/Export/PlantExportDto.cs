namespace HydroGrow.Models.Export;

public class PlantExportDto
{
    public string Guid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string MediumType { get; set; } = string.Empty;
    public string AcquiredDate { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public int IsArchived { get; set; }

    public MeasurementRangeExportDto? MeasurementRange { get; set; }
    public List<MeasurementExportDto> Measurements { get; set; } = [];
    public List<TreatmentExportDto> Treatments { get; set; } = [];
    public List<ReminderExportDto> Reminders { get; set; } = [];
    public List<PhotoExportDto> Photos { get; set; } = [];
    public string? ThumbnailPhotoGuid { get; set; }
}

public class MeasurementRangeExportDto
{
    public double? PhMin { get; set; }
    public double? PhMax { get; set; }
    public double? EcMin { get; set; }
    public double? EcMax { get; set; }
    public double? TdsMin { get; set; }
    public double? TdsMax { get; set; }
    public double? WaterTempCMin { get; set; }
    public double? WaterTempCMax { get; set; }
    public double? AmbientTempCMin { get; set; }
    public double? AmbientTempCMax { get; set; }
    public double? HumidityPctMin { get; set; }
    public double? HumidityPctMax { get; set; }
}

public class MeasurementExportDto
{
    public string Guid { get; set; } = string.Empty;
    public string RecordedAt { get; set; } = string.Empty;
    public double? Ph { get; set; }
    public double? Ec { get; set; }
    public double? Tds { get; set; }
    public double? WaterTempC { get; set; }
    public double? AmbientTempC { get; set; }
    public double? HumidityPct { get; set; }
    public string? WaterLevel { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class TreatmentExportDto
{
    public string Guid { get; set; } = string.Empty;
    public string RecordedAt { get; set; } = string.Empty;
    public string TreatmentType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string ProductUsed { get; set; } = string.Empty;
    public double? AmountMl { get; set; }
}

public class ReminderExportDto
{
    public string Guid { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ReminderType { get; set; } = string.Empty;
    public int RecurrenceDays { get; set; }
    public string NextDueAt { get; set; } = string.Empty;
    public int IsEnabled { get; set; }
}

public class PhotoExportDto
{
    public string Guid { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string TakenAt { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class HydroGrowExportData
{
    public List<PlantExportDto> Plants { get; set; } = [];
}
