namespace HydroGrow.Models;

public class MeasurementRange
{
    public int Id { get; set; }
    public int PlantId { get; set; }

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

    public bool IsInRange(double? value, double? min, double? max)
    {
        if (!value.HasValue) return true;
        if (min.HasValue && value < min) return false;
        if (max.HasValue && value > max) return false;
        return true;
    }

    public bool IsPhInRange(double? value) => IsInRange(value, PhMin, PhMax);
    public bool IsEcInRange(double? value) => IsInRange(value, EcMin, EcMax);
    public bool IsTdsInRange(double? value) => IsInRange(value, TdsMin, TdsMax);
    public bool IsWaterTempInRange(double? value) => IsInRange(value, WaterTempCMin, WaterTempCMax);
    public bool IsAmbientTempInRange(double? value) => IsInRange(value, AmbientTempCMin, AmbientTempCMax);
    public bool IsHumidityInRange(double? value) => IsInRange(value, HumidityPctMin, HumidityPctMax);
}
