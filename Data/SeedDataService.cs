using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace HydroGrow.Data;

public class SeedData
{
    public List<PlantSeedDto> Plants { get; set; } = [];
}

public class PlantSeedDto
{
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string MediumType { get; set; } = string.Empty;
    public string AcquiredDate { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public MeasurementRangeSeedDto? Range { get; set; }
}

public class MeasurementRangeSeedDto
{
    public double? PhMin { get; set; }
    public double? PhMax { get; set; }
    public double? EcMin { get; set; }
    public double? EcMax { get; set; }
    public double? TdsMin { get; set; }
    public double? TdsMax { get; set; }
    public double? WaterTempCMin { get; set; }
    public double? WaterTempCMax { get; set; }
    public double? HumidityPctMin { get; set; }
    public double? HumidityPctMax { get; set; }
}

public class SeedDataService
{
    private readonly PlantRepository _plantRepository;
    private readonly MeasurementRepository _measurementRepository;
    private readonly TreatmentRepository _treatmentRepository;
    private readonly ReminderRepository _reminderRepository;
    private readonly PhotoRepository _photoRepository;
    private readonly MeasurementRangeRepository _rangeRepository;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        PlantRepository plantRepository,
        MeasurementRepository measurementRepository,
        TreatmentRepository treatmentRepository,
        ReminderRepository reminderRepository,
        PhotoRepository photoRepository,
        MeasurementRangeRepository rangeRepository,
        ILogger<SeedDataService> logger)
    {
        _plantRepository = plantRepository;
        _measurementRepository = measurementRepository;
        _treatmentRepository = treatmentRepository;
        _reminderRepository = reminderRepository;
        _photoRepository = photoRepository;
        _rangeRepository = rangeRepository;
        _logger = logger;
    }

    public async Task LoadSeedDataAsync()
    {
        await ClearTablesAsync();

        await using Stream templateStream = await FileSystem.OpenAppPackageFileAsync("SeedData.json");

        SeedData? payload = null;
        try
        {
            payload = await JsonSerializer.DeserializeAsync(templateStream, JsonContext.Default.SeedData);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing seed data");
        }

        if (payload is null) return;

        try
        {
            foreach (var dto in payload.Plants)
            {
                var plant = new Plant
                {
                    Name = dto.Name,
                    Species = dto.Species,
                    // LocationId seeded as null — no location dictionary in seed data
                    MediumType = dto.MediumType,
                    AcquiredDate = dto.AcquiredDate,
                    Notes = dto.Notes
                };

                await _plantRepository.SaveItemAsync(plant);

                if (dto.Range is not null)
                {
                    var range = new MeasurementRange
                    {
                        PlantId = plant.Id,
                        PhMin = dto.Range.PhMin,
                        PhMax = dto.Range.PhMax,
                        EcMin = dto.Range.EcMin,
                        EcMax = dto.Range.EcMax,
                        TdsMin = dto.Range.TdsMin,
                        TdsMax = dto.Range.TdsMax,
                        WaterTempCMin = dto.Range.WaterTempCMin,
                        WaterTempCMax = dto.Range.WaterTempCMax,
                        HumidityPctMin = dto.Range.HumidityPctMin,
                        HumidityPctMax = dto.Range.HumidityPctMax
                    };
                    await _rangeRepository.SaveItemAsync(range);
                }

                // Add a sample measurement for each seed plant
                var measurement = new Measurement
                {
                    PlantId = plant.Id,
                    RecordedAt = DateTime.UtcNow.AddDays(-2).ToString("O"),
                    Ph = 5.8 + (plant.Id % 3) * 0.2,
                    Ec = 1.2 + (plant.Id % 2) * 0.3,
                    WaterTempC = 22.0,
                    HumidityPct = 60
                };
                await _measurementRepository.SaveItemAsync(measurement);

                // Add a sample treatment
                var treatment = new Treatment
                {
                    PlantId = plant.Id,
                    RecordedAt = DateTime.UtcNow.AddDays(-5).ToString("O"),
                    TreatmentType = TreatmentType.NutrientChange.ToString(),
                    Notes = "Regularna wymiana pożywki"
                };
                await _treatmentRepository.SaveItemAsync(treatment);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving seed data");
            throw;
        }
    }

    private async Task ClearTablesAsync()
    {
        try
        {
            await Task.WhenAll(
                _plantRepository.DropTableAsync(),
                _measurementRepository.DropTableAsync(),
                _treatmentRepository.DropTableAsync(),
                _reminderRepository.DropTableAsync(),
                _photoRepository.DropTableAsync(),
                _rangeRepository.DropTableAsync());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error clearing tables during seed");
        }
    }
}
