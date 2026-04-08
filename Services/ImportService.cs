using System.IO.Compression;
using System.Text.Json;

namespace HydroGrow.Services;

public enum ImportMode
{
    MergeNew,
    ReplaceAll
}

public class ImportResult
{
    public int PlantsImported { get; set; }
    public int PlantsSkipped { get; set; }
    public int PhotosCopied { get; set; }
    public string? BackupPath { get; set; }
}

public class ImportService
{
    private readonly PlantRepository _plantRepository;
    private readonly MeasurementRepository _measurementRepository;
    private readonly TreatmentRepository _treatmentRepository;
    private readonly ReminderRepository _reminderRepository;
    private readonly PhotoRepository _photoRepository;
    private readonly MeasurementRangeRepository _rangeRepository;
    private readonly ExportService _exportService;

    public ImportService(
        PlantRepository plantRepository,
        MeasurementRepository measurementRepository,
        TreatmentRepository treatmentRepository,
        ReminderRepository reminderRepository,
        PhotoRepository photoRepository,
        MeasurementRangeRepository rangeRepository,
        ExportService exportService)
    {
        _plantRepository = plantRepository;
        _measurementRepository = measurementRepository;
        _treatmentRepository = treatmentRepository;
        _reminderRepository = reminderRepository;
        _photoRepository = photoRepository;
        _rangeRepository = rangeRepository;
        _exportService = exportService;
    }

    public async Task<ImportResult> ImportAsync(string zipPath, ImportMode mode, IProgress<double>? progress = null)
    {
        await using var zipStream = File.OpenRead(zipPath);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

        // Read manifest
        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new InvalidOperationException("Invalid export file: missing manifest.json");

        ExportManifest manifest;
        await using (var ms = manifestEntry.Open())
        {
            manifest = await JsonSerializer.DeserializeAsync(ms, JsonContext.Default.ExportManifest)
                ?? throw new InvalidOperationException("Failed to parse manifest.json");
        }

        if (manifest.SchemaVersion > Constants.CurrentSchemaVersion)
            throw new InvalidOperationException(
                $"Export schema version {manifest.SchemaVersion} is newer than supported ({Constants.CurrentSchemaVersion}). Please update the app.");

        // Read data
        var dataEntry = archive.GetEntry("data.json")
            ?? throw new InvalidOperationException("Invalid export file: missing data.json");

        HydroGrowExportData exportData;
        await using (var ms = dataEntry.Open())
        {
            exportData = await JsonSerializer.DeserializeAsync(ms, JsonContext.Default.HydroGrowExportData)
                ?? throw new InvalidOperationException("Failed to parse data.json");
        }

        progress?.Report(0.1);

        string? backupPath = null;
        if (mode == ImportMode.ReplaceAll)
        {
            // Auto-backup before replacing
            backupPath = await _exportService.ExportAsync(new Progress<double>(p =>
                progress?.Report(0.1 + p * 0.3)));

            // Drop all tables (repos re-create on next access)
            await _plantRepository.DropTableAsync();
            await _measurementRepository.DropTableAsync();
            await _treatmentRepository.DropTableAsync();
            await _reminderRepository.DropTableAsync();
            await _photoRepository.DropTableAsync();
            await _rangeRepository.DropTableAsync();

            // Delete all existing photos from disk
            var photosDir = Constants.PhotosDirectory;
            if (Directory.Exists(photosDir))
            {
                foreach (var file in Directory.GetFiles(photosDir))
                    File.Delete(file);
            }
        }

        progress?.Report(0.4);

        var result = new ImportResult { BackupPath = backupPath };
        int totalPlants = exportData.Plants.Count;

        for (int i = 0; i < totalPlants; i++)
        {
            var dto = exportData.Plants[i];
            progress?.Report(0.4 + (double)i / Math.Max(1, totalPlants) * 0.4);

            if (mode == ImportMode.MergeNew)
            {
                var existing = await _plantRepository.GetByGuidAsync(dto.Guid);
                if (existing != null)
                {
                    result.PlantsSkipped++;
                    continue;
                }
            }

            // Insert plant (Id=0 forces insert)
            var plant = new Plant
            {
                Guid = dto.Guid,
                Name = dto.Name,
                Species = dto.Species,
                Location = dto.Location,
                MediumType = dto.MediumType,
                AcquiredDate = dto.AcquiredDate,
                Notes = dto.Notes,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                IsArchived = dto.IsArchived
            };
            await _plantRepository.SaveItemAsync(plant);

            // Insert measurements
            foreach (var m in dto.Measurements)
            {
                await _measurementRepository.SaveItemAsync(new Measurement
                {
                    Guid = m.Guid,
                    PlantId = plant.Id,
                    RecordedAt = m.RecordedAt,
                    Ph = m.Ph, Ec = m.Ec, Tds = m.Tds,
                    WaterTempC = m.WaterTempC, AmbientTempC = m.AmbientTempC,
                    HumidityPct = m.HumidityPct, WaterLevel = m.WaterLevel,
                    Notes = m.Notes
                });
            }

            // Insert treatments
            foreach (var t in dto.Treatments)
            {
                await _treatmentRepository.SaveItemAsync(new Treatment
                {
                    Guid = t.Guid,
                    PlantId = plant.Id,
                    RecordedAt = t.RecordedAt,
                    TreatmentType = t.TreatmentType,
                    Notes = t.Notes,
                    ProductUsed = t.ProductUsed,
                    AmountMl = t.AmountMl
                });
            }

            // Insert reminders
            foreach (var r in dto.Reminders)
            {
                await _reminderRepository.SaveItemAsync(new Reminder
                {
                    Guid = r.Guid,
                    PlantId = plant.Id,
                    Title = r.Title,
                    ReminderType = r.ReminderType,
                    RecurrenceDays = r.RecurrenceDays,
                    NextDueAt = r.NextDueAt,
                    IsEnabled = r.IsEnabled
                });
            }

            // Insert photos and copy files
            int? thumbnailPhotoId = null;
            foreach (var p in dto.Photos)
            {
                var destFileName = p.FileName;
                var destPath = Path.Combine(Constants.PhotosDirectory, destFileName);

                if (!Directory.Exists(Constants.PhotosDirectory))
                    Directory.CreateDirectory(Constants.PhotosDirectory);

                var archivePhotoEntry = archive.GetEntry($"photos/{p.FileName}");
                if (archivePhotoEntry != null && !File.Exists(destPath))
                {
                    await using var entryStream = archivePhotoEntry.Open();
                    await using var fileStream = File.OpenWrite(destPath);
                    await entryStream.CopyToAsync(fileStream);
                    result.PhotosCopied++;
                }

                var photo = new PlantPhoto
                {
                    Guid = p.Guid,
                    PlantId = plant.Id,
                    FilePath = destFileName,
                    TakenAt = p.TakenAt,
                    Caption = p.Caption,
                    SortOrder = p.SortOrder
                };
                await _photoRepository.SaveItemAsync(photo);

                if (p.Guid == dto.ThumbnailPhotoGuid)
                    thumbnailPhotoId = photo.Id;
            }

            // Update thumbnail reference
            if (thumbnailPhotoId.HasValue)
            {
                plant.ThumbnailPhotoId = thumbnailPhotoId.Value;
                await _plantRepository.SaveItemAsync(plant);
            }

            // Insert measurement range
            if (dto.MeasurementRange != null)
            {
                var r = dto.MeasurementRange;
                await _rangeRepository.SaveItemAsync(new MeasurementRange
                {
                    PlantId = plant.Id,
                    PhMin = r.PhMin, PhMax = r.PhMax,
                    EcMin = r.EcMin, EcMax = r.EcMax,
                    TdsMin = r.TdsMin, TdsMax = r.TdsMax,
                    WaterTempCMin = r.WaterTempCMin, WaterTempCMax = r.WaterTempCMax,
                    AmbientTempCMin = r.AmbientTempCMin, AmbientTempCMax = r.AmbientTempCMax,
                    HumidityPctMin = r.HumidityPctMin, HumidityPctMax = r.HumidityPctMax
                });
            }

            result.PlantsImported++;
        }

        progress?.Report(1.0);
        return result;
    }
}
