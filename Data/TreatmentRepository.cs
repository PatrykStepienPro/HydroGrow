using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace HydroGrow.Data;

public class TreatmentRepository
{
    private bool _hasBeenInitialized = false;
    private readonly ILogger _logger;

    public TreatmentRepository(ILogger<TreatmentRepository> logger)
    {
        _logger = logger;
    }

    private async Task Init()
    {
        if (_hasBeenInitialized) return;

        await using var connection = await Constants.OpenConnectionAsync();

        try
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Treatment (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Guid TEXT NOT NULL UNIQUE,
                    PlantId INTEGER NOT NULL,
                    RecordedAt TEXT NOT NULL,
                    TreatmentType TEXT NOT NULL DEFAULT '',
                    Notes TEXT NOT NULL DEFAULT '',
                    ProductUsed TEXT NOT NULL DEFAULT '',
                    AmountMl REAL
                );
                CREATE INDEX IF NOT EXISTS idx_treatment_plant ON Treatment(PlantId, RecordedAt DESC);";
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating Treatment table");
            throw;
        }

        _hasBeenInitialized = true;
    }

    public async Task<List<Treatment>> ListAsync(int plantId, int limit = 30)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Treatment WHERE PlantId = @plantId ORDER BY RecordedAt DESC LIMIT @limit";
        cmd.Parameters.AddWithValue("@plantId", plantId);
        cmd.Parameters.AddWithValue("@limit", limit);

        var items = new List<Treatment>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapRow(reader));

        return items;
    }

    public async Task<Treatment?> GetLatestAsync(int plantId, string treatmentType)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Treatment WHERE PlantId = @plantId AND TreatmentType = @type ORDER BY RecordedAt DESC LIMIT 1";
        cmd.Parameters.AddWithValue("@plantId", plantId);
        cmd.Parameters.AddWithValue("@type", treatmentType);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapRow(reader);

        return null;
    }

    public async Task<Treatment?> GetAsync(int id)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Treatment WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapRow(reader);

        return null;
    }

    public async Task<int> SaveItemAsync(Treatment item)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        var cmd = connection.CreateCommand();
        if (item.Id == 0)
        {
            if (string.IsNullOrEmpty(item.Guid))
                item.Guid = Guid.NewGuid().ToString();

            cmd.CommandText = @"
                INSERT INTO Treatment (Guid, PlantId, RecordedAt, TreatmentType, Notes, ProductUsed, AmountMl)
                VALUES (@guid, @plantId, @recordedAt, @treatmentType, @notes, @productUsed, @amountMl);
                SELECT last_insert_rowid();";
        }
        else
        {
            cmd.CommandText = @"
                UPDATE Treatment SET PlantId=@plantId, RecordedAt=@recordedAt, TreatmentType=@treatmentType,
                    Notes=@notes, ProductUsed=@productUsed, AmountMl=@amountMl
                WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", item.Id);
        }

        cmd.Parameters.AddWithValue("@guid", item.Guid);
        cmd.Parameters.AddWithValue("@plantId", item.PlantId);
        cmd.Parameters.AddWithValue("@recordedAt", item.RecordedAt);
        cmd.Parameters.AddWithValue("@treatmentType", item.TreatmentType ?? "");
        cmd.Parameters.AddWithValue("@notes", item.Notes ?? "");
        cmd.Parameters.AddWithValue("@productUsed", item.ProductUsed ?? "");
        cmd.Parameters.AddWithValue("@amountMl", item.AmountMl.HasValue ? item.AmountMl.Value : DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        if (item.Id == 0)
            item.Id = Convert.ToInt32(result);

        return item.Id;
    }

    public async Task<int> DeleteItemAsync(Treatment item)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Treatment WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", item.Id);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteByPlantAsync(int plantId)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Treatment WHERE PlantId = @plantId";
        cmd.Parameters.AddWithValue("@plantId", plantId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DropTableAsync()
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS Treatment";
        await cmd.ExecuteNonQueryAsync();
        _hasBeenInitialized = false;
    }

    private static Treatment MapRow(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        Guid = r.GetString(r.GetOrdinal("Guid")),
        PlantId = r.GetInt32(r.GetOrdinal("PlantId")),
        RecordedAt = r.GetString(r.GetOrdinal("RecordedAt")),
        TreatmentType = r.IsDBNull(r.GetOrdinal("TreatmentType")) ? "" : r.GetString(r.GetOrdinal("TreatmentType")),
        Notes = r.IsDBNull(r.GetOrdinal("Notes")) ? "" : r.GetString(r.GetOrdinal("Notes")),
        ProductUsed = r.IsDBNull(r.GetOrdinal("ProductUsed")) ? "" : r.GetString(r.GetOrdinal("ProductUsed")),
        AmountMl = r.IsDBNull(r.GetOrdinal("AmountMl")) ? null : r.GetDouble(r.GetOrdinal("AmountMl"))
    };
}
