using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace HydroGrow.Data;

public class PlantRepository
{
    private bool _hasBeenInitialized = false;
    private readonly ILogger _logger;

    public PlantRepository(ILogger<PlantRepository> logger)
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
                CREATE TABLE IF NOT EXISTS Plant (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Guid TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    Species TEXT NOT NULL DEFAULT '',
                    Location TEXT NOT NULL DEFAULT '',
                    MediumType TEXT NOT NULL DEFAULT '',
                    AcquiredDate TEXT NOT NULL DEFAULT '',
                    Notes TEXT NOT NULL DEFAULT '',
                    ThumbnailPhotoId INTEGER,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    IsArchived INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS idx_plant_guid ON Plant(Guid);";
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating Plant table");
            throw;
        }

        _hasBeenInitialized = true;
    }

    public async Task<List<Plant>> ListAsync(bool includeArchived = false)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = includeArchived
            ? "SELECT * FROM Plant ORDER BY Name ASC"
            : "SELECT * FROM Plant WHERE IsArchived = 0 ORDER BY Name ASC";

        var plants = new List<Plant>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            plants.Add(MapRow(reader));

        return plants;
    }

    public async Task<Plant?> GetAsync(int id)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Plant WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapRow(reader);

        return null;
    }

    public async Task<Plant?> GetByGuidAsync(string guid)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Plant WHERE Guid = @guid";
        cmd.Parameters.AddWithValue("@guid", guid);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapRow(reader);

        return null;
    }

    public async Task<int> SaveItemAsync(Plant item)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        item.UpdatedAt = DateTime.UtcNow.ToString("O");

        var cmd = connection.CreateCommand();
        if (item.Id == 0)
        {
            if (string.IsNullOrEmpty(item.Guid))
                item.Guid = Guid.NewGuid().ToString();
            item.CreatedAt = item.UpdatedAt;

            cmd.CommandText = @"
                INSERT INTO Plant (Guid, Name, Species, Location, MediumType, AcquiredDate, Notes,
                    ThumbnailPhotoId, CreatedAt, UpdatedAt, IsArchived)
                VALUES (@guid, @name, @species, @location, @mediumType, @acquiredDate, @notes,
                    @thumbnailPhotoId, @createdAt, @updatedAt, @isArchived);
                SELECT last_insert_rowid();";
        }
        else
        {
            cmd.CommandText = @"
                UPDATE Plant SET Name=@name, Species=@species, Location=@location, MediumType=@mediumType,
                    AcquiredDate=@acquiredDate, Notes=@notes, ThumbnailPhotoId=@thumbnailPhotoId,
                    UpdatedAt=@updatedAt, IsArchived=@isArchived
                WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", item.Id);
        }

        cmd.Parameters.AddWithValue("@guid", item.Guid);
        cmd.Parameters.AddWithValue("@name", item.Name);
        cmd.Parameters.AddWithValue("@species", item.Species ?? "");
        cmd.Parameters.AddWithValue("@location", item.Location ?? "");
        cmd.Parameters.AddWithValue("@mediumType", item.MediumType ?? "");
        cmd.Parameters.AddWithValue("@acquiredDate", item.AcquiredDate ?? "");
        cmd.Parameters.AddWithValue("@notes", item.Notes ?? "");
        cmd.Parameters.AddWithValue("@thumbnailPhotoId", item.ThumbnailPhotoId.HasValue ? item.ThumbnailPhotoId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@createdAt", item.CreatedAt);
        cmd.Parameters.AddWithValue("@updatedAt", item.UpdatedAt);
        cmd.Parameters.AddWithValue("@isArchived", item.IsArchived);

        var result = await cmd.ExecuteScalarAsync();
        if (item.Id == 0)
            item.Id = Convert.ToInt32(result);

        return item.Id;
    }

    public async Task<int> DeleteItemAsync(Plant item)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Plant WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", item.Id);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task DropTableAsync()
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS Plant";
        await cmd.ExecuteNonQueryAsync();
        _hasBeenInitialized = false;
    }

    private static Plant MapRow(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        Guid = r.GetString(r.GetOrdinal("Guid")),
        Name = r.GetString(r.GetOrdinal("Name")),
        Species = r.IsDBNull(r.GetOrdinal("Species")) ? "" : r.GetString(r.GetOrdinal("Species")),
        Location = r.IsDBNull(r.GetOrdinal("Location")) ? "" : r.GetString(r.GetOrdinal("Location")),
        MediumType = r.IsDBNull(r.GetOrdinal("MediumType")) ? "" : r.GetString(r.GetOrdinal("MediumType")),
        AcquiredDate = r.IsDBNull(r.GetOrdinal("AcquiredDate")) ? "" : r.GetString(r.GetOrdinal("AcquiredDate")),
        Notes = r.IsDBNull(r.GetOrdinal("Notes")) ? "" : r.GetString(r.GetOrdinal("Notes")),
        ThumbnailPhotoId = r.IsDBNull(r.GetOrdinal("ThumbnailPhotoId")) ? null : r.GetInt32(r.GetOrdinal("ThumbnailPhotoId")),
        CreatedAt = r.GetString(r.GetOrdinal("CreatedAt")),
        UpdatedAt = r.GetString(r.GetOrdinal("UpdatedAt")),
        IsArchived = r.GetInt32(r.GetOrdinal("IsArchived"))
    };
}
