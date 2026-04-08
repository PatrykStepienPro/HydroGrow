using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace HydroGrow.Data;

public class MeasurementRepository
{
    private bool _hasBeenInitialized = false;
    private readonly ILogger _logger;

    public MeasurementRepository(ILogger<MeasurementRepository> logger)
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
                CREATE TABLE IF NOT EXISTS Measurement (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Guid TEXT NOT NULL UNIQUE,
                    PlantId INTEGER NOT NULL,
                    RecordedAt TEXT NOT NULL,
                    Ph REAL,
                    Ec REAL,
                    Tds REAL,
                    WaterTempC REAL,
                    AmbientTempC REAL,
                    HumidityPct REAL,
                    WaterLevel TEXT,
                    Notes TEXT NOT NULL DEFAULT ''
                );
                CREATE INDEX IF NOT EXISTS idx_measurement_plant ON Measurement(PlantId, RecordedAt DESC);";
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating Measurement table");
            throw;
        }

        _hasBeenInitialized = true;
    }

    public async Task<List<Measurement>> ListAsync(int plantId, int limit = 50)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Measurement WHERE PlantId = @plantId ORDER BY RecordedAt DESC LIMIT @limit";
        cmd.Parameters.AddWithValue("@plantId", plantId);
        cmd.Parameters.AddWithValue("@limit", limit);

        var items = new List<Measurement>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapRow(reader));

        return items;
    }

    public async Task<Measurement?> GetLatestAsync(int plantId)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Measurement WHERE PlantId = @plantId ORDER BY RecordedAt DESC LIMIT 1";
        cmd.Parameters.AddWithValue("@plantId", plantId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapRow(reader);

        return null;
    }

    public async Task<Measurement?> GetAsync(int id)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Measurement WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapRow(reader);

        return null;
    }

    public async Task<int> SaveItemAsync(Measurement item)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        if (item.Id == 0)
        {
            if (string.IsNullOrEmpty(item.Guid))
                item.Guid = Guid.NewGuid().ToString();

            cmd.CommandText = @"
                INSERT INTO Measurement (Guid, PlantId, RecordedAt, Ph, Ec, Tds, WaterTempC, AmbientTempC, HumidityPct, WaterLevel, Notes)
                VALUES (@guid, @plantId, @recordedAt, @ph, @ec, @tds, @waterTempC, @ambientTempC, @humidityPct, @waterLevel, @notes);
                SELECT last_insert_rowid();";
        }
        else
        {
            cmd.CommandText = @"
                UPDATE Measurement SET PlantId=@plantId, RecordedAt=@recordedAt, Ph=@ph, Ec=@ec, Tds=@tds,
                    WaterTempC=@waterTempC, AmbientTempC=@ambientTempC, HumidityPct=@humidityPct,
                    WaterLevel=@waterLevel, Notes=@notes
                WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", item.Id);
        }

        cmd.Parameters.AddWithValue("@guid", item.Guid);
        cmd.Parameters.AddWithValue("@plantId", item.PlantId);
        cmd.Parameters.AddWithValue("@recordedAt", item.RecordedAt);
        cmd.Parameters.AddWithValue("@ph", item.Ph.HasValue ? item.Ph.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@ec", item.Ec.HasValue ? item.Ec.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@tds", item.Tds.HasValue ? item.Tds.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@waterTempC", item.WaterTempC.HasValue ? item.WaterTempC.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@ambientTempC", item.AmbientTempC.HasValue ? item.AmbientTempC.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@humidityPct", item.HumidityPct.HasValue ? item.HumidityPct.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@waterLevel", item.WaterLevel != null ? item.WaterLevel : DBNull.Value);
        cmd.Parameters.AddWithValue("@notes", item.Notes ?? "");

        var result = await cmd.ExecuteScalarAsync();
        if (item.Id == 0)
            item.Id = Convert.ToInt32(result);

        return item.Id;
    }

    public async Task<int> DeleteItemAsync(Measurement item)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Measurement WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", item.Id);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteByPlantAsync(int plantId)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Measurement WHERE PlantId = @plantId";
        cmd.Parameters.AddWithValue("@plantId", plantId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DropTableAsync()
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS Measurement";
        await cmd.ExecuteNonQueryAsync();
        _hasBeenInitialized = false;
    }

    private static Measurement MapRow(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        Guid = r.GetString(r.GetOrdinal("Guid")),
        PlantId = r.GetInt32(r.GetOrdinal("PlantId")),
        RecordedAt = r.GetString(r.GetOrdinal("RecordedAt")),
        Ph = r.IsDBNull(r.GetOrdinal("Ph")) ? null : r.GetDouble(r.GetOrdinal("Ph")),
        Ec = r.IsDBNull(r.GetOrdinal("Ec")) ? null : r.GetDouble(r.GetOrdinal("Ec")),
        Tds = r.IsDBNull(r.GetOrdinal("Tds")) ? null : r.GetDouble(r.GetOrdinal("Tds")),
        WaterTempC = r.IsDBNull(r.GetOrdinal("WaterTempC")) ? null : r.GetDouble(r.GetOrdinal("WaterTempC")),
        AmbientTempC = r.IsDBNull(r.GetOrdinal("AmbientTempC")) ? null : r.GetDouble(r.GetOrdinal("AmbientTempC")),
        HumidityPct = r.IsDBNull(r.GetOrdinal("HumidityPct")) ? null : r.GetDouble(r.GetOrdinal("HumidityPct")),
        WaterLevel = r.IsDBNull(r.GetOrdinal("WaterLevel")) ? null : r.GetString(r.GetOrdinal("WaterLevel")),
        Notes = r.IsDBNull(r.GetOrdinal("Notes")) ? "" : r.GetString(r.GetOrdinal("Notes"))
    };
}
