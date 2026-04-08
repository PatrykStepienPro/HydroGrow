using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace HydroGrow.Data;

public class PhotoRepository
{
    private bool _hasBeenInitialized = false;
    private readonly ILogger _logger;

    public PhotoRepository(ILogger<PhotoRepository> logger)
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
                CREATE TABLE IF NOT EXISTS PlantPhoto (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Guid TEXT NOT NULL UNIQUE,
                    PlantId INTEGER NOT NULL,
                    FilePath TEXT NOT NULL,
                    TakenAt TEXT NOT NULL,
                    Caption TEXT NOT NULL DEFAULT '',
                    SortOrder INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS idx_photo_plant ON PlantPhoto(PlantId, SortOrder ASC);";
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating PlantPhoto table");
            throw;
        }

        _hasBeenInitialized = true;
    }

    public async Task<List<PlantPhoto>> ListAsync(int plantId)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM PlantPhoto WHERE PlantId = @plantId ORDER BY SortOrder ASC, TakenAt DESC";
        cmd.Parameters.AddWithValue("@plantId", plantId);

        var items = new List<PlantPhoto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapRow(reader));

        return items;
    }

    public async Task<PlantPhoto?> GetAsync(int id)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM PlantPhoto WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapRow(reader);

        return null;
    }

    public async Task<int> SaveItemAsync(PlantPhoto item)
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
                INSERT INTO PlantPhoto (Guid, PlantId, FilePath, TakenAt, Caption, SortOrder)
                VALUES (@guid, @plantId, @filePath, @takenAt, @caption, @sortOrder);
                SELECT last_insert_rowid();";
        }
        else
        {
            cmd.CommandText = @"
                UPDATE PlantPhoto SET PlantId=@plantId, FilePath=@filePath, TakenAt=@takenAt,
                    Caption=@caption, SortOrder=@sortOrder
                WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", item.Id);
        }

        cmd.Parameters.AddWithValue("@guid", item.Guid);
        cmd.Parameters.AddWithValue("@plantId", item.PlantId);
        cmd.Parameters.AddWithValue("@filePath", item.FilePath);
        cmd.Parameters.AddWithValue("@takenAt", item.TakenAt);
        cmd.Parameters.AddWithValue("@caption", item.Caption ?? "");
        cmd.Parameters.AddWithValue("@sortOrder", item.SortOrder);

        var result = await cmd.ExecuteScalarAsync();
        if (item.Id == 0)
            item.Id = Convert.ToInt32(result);

        return item.Id;
    }

    public async Task<int> DeleteItemAsync(PlantPhoto item)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM PlantPhoto WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", item.Id);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteByPlantAsync(int plantId)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM PlantPhoto WHERE PlantId = @plantId";
        cmd.Parameters.AddWithValue("@plantId", plantId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DropTableAsync()
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS PlantPhoto";
        await cmd.ExecuteNonQueryAsync();
        _hasBeenInitialized = false;
    }

    private static PlantPhoto MapRow(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        Guid = r.GetString(r.GetOrdinal("Guid")),
        PlantId = r.GetInt32(r.GetOrdinal("PlantId")),
        FilePath = r.GetString(r.GetOrdinal("FilePath")),
        TakenAt = r.GetString(r.GetOrdinal("TakenAt")),
        Caption = r.IsDBNull(r.GetOrdinal("Caption")) ? "" : r.GetString(r.GetOrdinal("Caption")),
        SortOrder = r.GetInt32(r.GetOrdinal("SortOrder"))
    };
}
