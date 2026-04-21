using System.IO.Compression;
using System.Text.Json;

namespace HydroGrow.Services;

public class ExportService
{
    private readonly PlantRepository _plantRepository;
    private readonly MeasurementRepository _measurementRepository;
    private readonly TreatmentRepository _treatmentRepository;
    private readonly ReminderRepository _reminderRepository;
    private readonly PhotoRepository _photoRepository;
    private readonly MeasurementRangeRepository _rangeRepository;
    private readonly PhotoService _photoService;

    public ExportService(
        PlantRepository plantRepository,
        MeasurementRepository measurementRepository,
        TreatmentRepository treatmentRepository,
        ReminderRepository reminderRepository,
        PhotoRepository photoRepository,
        MeasurementRangeRepository rangeRepository,
        PhotoService photoService)
    {
        _plantRepository = plantRepository;
        _measurementRepository = measurementRepository;
        _treatmentRepository = treatmentRepository;
        _reminderRepository = reminderRepository;
        _photoRepository = photoRepository;
        _rangeRepository = rangeRepository;
        _photoService = photoService;
    }

    public async Task<string> ExportAsync(IProgress<double>? progress = null)
    {
        var backupsDir = Constants.BackupsDirectory;
        if (!Directory.Exists(backupsDir))
            Directory.CreateDirectory(backupsDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var zipPath = Path.Combine(backupsDir, $"hydrogrow_export_{timestamp}.zip");

        var plants = await _plantRepository.ListAsync(includeArchived: true);
        var exportDtos = new List<PlantExportDto>();

        int measurementCount = 0, treatmentCount = 0, reminderCount = 0, photoCount = 0;

        for (int i = 0; i < plants.Count; i++)
        {
            var plant = plants[i];
            progress?.Report((double)i / plants.Count * 0.6);

            var measurements = await _measurementRepository.ListAsync(plant.Id, limit: 1000);
            var treatments = await _treatmentRepository.ListAsync(plant.Id, limit: 1000);
            var reminders = await _reminderRepository.ListAsync(plant.Id);
            var photos = await _photoRepository.ListAsync(plant.Id);
            var range = await _rangeRepository.GetAsync(plant.Id);

            string? thumbGuid = null;
            if (plant.ThumbnailPhotoId.HasValue)
            {
                var thumb = photos.FirstOrDefault(p => p.Id == plant.ThumbnailPhotoId.Value);
                thumbGuid = thumb?.Guid;
            }

            exportDtos.Add(new PlantExportDto
            {
                Guid = plant.Guid,
                Name = plant.Name,
                Species = plant.Species,
                Location = plant.LocationName,
                MediumType = plant.MediumType,
                AcquiredDate = plant.AcquiredDate,
                Notes = plant.Notes,
                CreatedAt = plant.CreatedAt,
                UpdatedAt = plant.UpdatedAt,
                IsArchived = plant.IsArchived,
                ThumbnailPhotoGuid = thumbGuid,
                MeasurementRange = range != null ? MapRange(range) : null,
                Measurements = measurements.Select(m => new MeasurementExportDto
                {
                    Guid = m.Guid,
                    RecordedAt = m.RecordedAt,
                    Ph = m.Ph, Ec = m.Ec, Tds = m.Tds,
                    WaterTempC = m.WaterTempC, AmbientTempC = m.AmbientTempC,
                    HumidityPct = m.HumidityPct, WaterLevel = m.WaterLevel,
                    Notes = m.Notes
                }).ToList(),
                Treatments = treatments.Select(t => new TreatmentExportDto
                {
                    Guid = t.Guid,
                    RecordedAt = t.RecordedAt,
                    TreatmentType = t.TreatmentType,
                    Notes = t.Notes,
                    ProductUsed = t.ProductUsed,
                    AmountMl = t.AmountMl
                }).ToList(),
                Reminders = reminders.Select(r => new ReminderExportDto
                {
                    Guid = r.Guid,
                    Title = r.Title,
                    ReminderType = r.ReminderType,
                    RecurrenceDays = r.RecurrenceDays,
                    NextDueAt = r.NextDueAt,
                    IsEnabled = r.IsEnabled
                }).ToList(),
                Photos = photos.Select(p => new PhotoExportDto
                {
                    Guid = p.Guid,
                    FileName = p.FilePath,
                    TakenAt = p.TakenAt,
                    Caption = p.Caption,
                    SortOrder = p.SortOrder
                }).ToList()
            });

            measurementCount += measurements.Count;
            treatmentCount += treatments.Count;
            reminderCount += reminders.Count;
            photoCount += photos.Count;
        }

        var exportData = new HydroGrowExportData { Plants = exportDtos };
        var manifest = new ExportManifest
        {
            PlantCount = plants.Count,
            MeasurementCount = measurementCount,
            TreatmentCount = treatmentCount,
            ReminderCount = reminderCount,
            PhotoCount = photoCount
        };

        progress?.Report(0.7);

        await using var zipStream = File.OpenWrite(zipPath);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false);

        // Write manifest.json
        var manifestEntry = archive.CreateEntry("manifest.json");
        await using (var ms = manifestEntry.Open())
        {
            await JsonSerializer.SerializeAsync(ms, manifest, JsonContext.Default.ExportManifest);
        }

        // Write data.json
        var dataEntry = archive.CreateEntry("data.json");
        await using (var ms = dataEntry.Open())
        {
            await JsonSerializer.SerializeAsync(ms, exportData, JsonContext.Default.HydroGrowExportData);
        }

        // Write photos
        int photosDone = 0;
        foreach (var dto in exportDtos)
        {
            foreach (var photo in dto.Photos)
            {
                var fullPath = _photoService.GetFullPath(photo.FileName);
                if (File.Exists(fullPath))
                {
                    var photoEntry = archive.CreateEntry($"photos/{photo.FileName}");
                    await using var photoEntryStream = photoEntry.Open();
                    await using var fileStream = File.OpenRead(fullPath);
                    await fileStream.CopyToAsync(photoEntryStream);
                }
                photosDone++;
                progress?.Report(0.7 + (double)photosDone / Math.Max(1, photoCount) * 0.3);
            }
        }

        progress?.Report(1.0);
        return zipPath;
    }

    private static MeasurementRangeExportDto MapRange(MeasurementRange r) => new()
    {
        PhMin = r.PhMin, PhMax = r.PhMax,
        EcMin = r.EcMin, EcMax = r.EcMax,
        TdsMin = r.TdsMin, TdsMax = r.TdsMax,
        WaterTempCMin = r.WaterTempCMin, WaterTempCMax = r.WaterTempCMax,
        AmbientTempCMin = r.AmbientTempCMin, AmbientTempCMax = r.AmbientTempCMax,
        HumidityPctMin = r.HumidityPctMin, HumidityPctMax = r.HumidityPctMax
    };
}
