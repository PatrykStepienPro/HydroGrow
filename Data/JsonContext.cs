using System.Text.Json.Serialization;
using HydroGrow.Models;
using HydroGrow.Models.Export;

[JsonSerializable(typeof(Plant))]
[JsonSerializable(typeof(Measurement))]
[JsonSerializable(typeof(Treatment))]
[JsonSerializable(typeof(Reminder))]
[JsonSerializable(typeof(PlantPhoto))]
[JsonSerializable(typeof(MeasurementRange))]
[JsonSerializable(typeof(ExportManifest))]
[JsonSerializable(typeof(PlantExportDto))]
[JsonSerializable(typeof(MeasurementExportDto))]
[JsonSerializable(typeof(TreatmentExportDto))]
[JsonSerializable(typeof(ReminderExportDto))]
[JsonSerializable(typeof(PhotoExportDto))]
[JsonSerializable(typeof(MeasurementRangeExportDto))]
[JsonSerializable(typeof(HydroGrowExportData))]
[JsonSerializable(typeof(List<Plant>))]
[JsonSerializable(typeof(List<PlantExportDto>))]
[JsonSerializable(typeof(SeedData))]
[JsonSerializable(typeof(List<PlantSpeciesEntry>))]
public partial class JsonContext : JsonSerializerContext
{
}
