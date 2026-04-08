using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace HydroGrow.Data;

public class MeasurementRangeRepository
{
    private bool _hasBeenInitialized = false;
    private readonly ILogger _logger;

    public MeasurementRangeRepository(ILogger<MeasurementRangeRepository> logger)
    {
        _logger = logger;
    }

    private async Task Init()
    {
        if (_hasBeenInitialized) return;

        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        try
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS MeasurementRange (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlantId INTEGER NOT NULL UNIQUE,
                    PhMin REAL, PhMax REAL,
                    EcMin REAL, EcMax REAL,
                    TdsMin REAL, TdsMax REAL,
                    WaterTempCMin REAL, WaterTempCMax REAL,
                    AmbientTempCMin REAL, AmbientTempCMax REAL,
                    HumidityPctMin REAL, HumidityPctMax REAL
                );";
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating MeasurementRange table");
            throw;
        }

        _hasBeenInitialized = true;
    }

    public async Task<MeasurementRange?> GetAsync(int plantId)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM MeasurementRange WHERE PlantId = @plantId";
        cmd.Parameters.AddWithValue("@plantId", plantId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapRow(reader);

        return null;
    }

    public async Task<int> SaveItemAsync(MeasurementRange item)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO MeasurementRange (PlantId, PhMin, PhMax, EcMin, EcMax, TdsMin, TdsMax,
                WaterTempCMin, WaterTempCMax, AmbientTempCMin, AmbientTempCMax, HumidityPctMin, HumidityPctMax)
            VALUES (@plantId, @phMin, @phMax, @ecMin, @ecMax, @tdsMin, @tdsMax,
                @waterTempCMin, @waterTempCMax, @ambientTempCMin, @ambientTempCMax, @humidityPctMin, @humidityPctMax)
            ON CONFLICT(PlantId) DO UPDATE SET
                PhMin=excluded.PhMin, PhMax=excluded.PhMax,
                EcMin=excluded.EcMin, EcMax=excluded.EcMax,
                TdsMin=excluded.TdsMin, TdsMax=excluded.TdsMax,
                WaterTempCMin=excluded.WaterTempCMin, WaterTempCMax=excluded.WaterTempCMax,
                AmbientTempCMin=excluded.AmbientTempCMin, AmbientTempCMax=excluded.AmbientTempCMax,
                HumidityPctMin=excluded.HumidityPctMin, HumidityPctMax=excluded.HumidityPctMax;
            SELECT Id FROM MeasurementRange WHERE PlantId = @plantId;";

        cmd.Parameters.AddWithValue("@plantId", item.PlantId);
        cmd.Parameters.AddWithValue("@phMin", item.PhMin.HasValue ? item.PhMin.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@phMax", item.PhMax.HasValue ? item.PhMax.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@ecMin", item.EcMin.HasValue ? item.EcMin.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@ecMax", item.EcMax.HasValue ? item.EcMax.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@tdsMin", item.TdsMin.HasValue ? item.TdsMin.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@tdsMax", item.TdsMax.HasValue ? item.TdsMax.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@waterTempCMin", item.WaterTempCMin.HasValue ? item.WaterTempCMin.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@waterTempCMax", item.WaterTempCMax.HasValue ? item.WaterTempCMax.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@ambientTempCMin", item.AmbientTempCMin.HasValue ? item.AmbientTempCMin.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@ambientTempCMax", item.AmbientTempCMax.HasValue ? item.AmbientTempCMax.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@humidityPctMin", item.HumidityPctMin.HasValue ? item.HumidityPctMin.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@humidityPctMax", item.HumidityPctMax.HasValue ? item.HumidityPctMax.Value : DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        item.Id = Convert.ToInt32(result);
        return item.Id;
    }

    public async Task DeleteAsync(int plantId)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM MeasurementRange WHERE PlantId = @plantId";
        cmd.Parameters.AddWithValue("@plantId", plantId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DropTableAsync()
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS MeasurementRange";
        await cmd.ExecuteNonQueryAsync();
        _hasBeenInitialized = false;
    }

    private static MeasurementRange MapRow(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        PlantId = r.GetInt32(r.GetOrdinal("PlantId")),
        PhMin = r.IsDBNull(r.GetOrdinal("PhMin")) ? null : r.GetDouble(r.GetOrdinal("PhMin")),
        PhMax = r.IsDBNull(r.GetOrdinal("PhMax")) ? null : r.GetDouble(r.GetOrdinal("PhMax")),
        EcMin = r.IsDBNull(r.GetOrdinal("EcMin")) ? null : r.GetDouble(r.GetOrdinal("EcMin")),
        EcMax = r.IsDBNull(r.GetOrdinal("EcMax")) ? null : r.GetDouble(r.GetOrdinal("EcMax")),
        TdsMin = r.IsDBNull(r.GetOrdinal("TdsMin")) ? null : r.GetDouble(r.GetOrdinal("TdsMin")),
        TdsMax = r.IsDBNull(r.GetOrdinal("TdsMax")) ? null : r.GetDouble(r.GetOrdinal("TdsMax")),
        WaterTempCMin = r.IsDBNull(r.GetOrdinal("WaterTempCMin")) ? null : r.GetDouble(r.GetOrdinal("WaterTempCMin")),
        WaterTempCMax = r.IsDBNull(r.GetOrdinal("WaterTempCMax")) ? null : r.GetDouble(r.GetOrdinal("WaterTempCMax")),
        AmbientTempCMin = r.IsDBNull(r.GetOrdinal("AmbientTempCMin")) ? null : r.GetDouble(r.GetOrdinal("AmbientTempCMin")),
        AmbientTempCMax = r.IsDBNull(r.GetOrdinal("AmbientTempCMax")) ? null : r.GetDouble(r.GetOrdinal("AmbientTempCMax")),
        HumidityPctMin = r.IsDBNull(r.GetOrdinal("HumidityPctMin")) ? null : r.GetDouble(r.GetOrdinal("HumidityPctMin")),
        HumidityPctMax = r.IsDBNull(r.GetOrdinal("HumidityPctMax")) ? null : r.GetDouble(r.GetOrdinal("HumidityPctMax"))
    };
}
