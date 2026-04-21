using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Location = HydroGrow.Models.Location;

namespace HydroGrow.Data;

public class LocationRepository
{
    private bool _hasBeenInitialized = false;
    private readonly ILogger _logger;

    public LocationRepository(ILogger<LocationRepository> logger) => _logger = logger;

    private async Task Init()
    {
        if (_hasBeenInitialized) return;
        await using var connection = await Constants.OpenConnectionAsync();
        try
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Location (
                    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE COLLATE NOCASE
                );";
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating Location table");
            throw;
        }
        _hasBeenInitialized = true;
    }

    public async Task<List<Location>> ListAsync()
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Location ORDER BY Name ASC";
        var list = new List<Location>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new Location { Id = reader.GetInt32(0), Name = reader.GetString(1) });
        return list;
    }

    public async Task<int> SaveItemAsync(Location item)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();
        var cmd = connection.CreateCommand();
        if (item.Id == 0)
        {
            cmd.CommandText = "INSERT INTO Location (Name) VALUES (@name); SELECT last_insert_rowid();";
        }
        else
        {
            cmd.CommandText = "UPDATE Location SET Name=@name WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", item.Id);
        }
        cmd.Parameters.AddWithValue("@name", item.Name.Trim());
        var result = await cmd.ExecuteScalarAsync();
        if (item.Id == 0) item.Id = Convert.ToInt32(result);
        return item.Id;
    }

    public async Task DeleteItemAsync(Location item)
    {
        await Init();
        await using var connection = await Constants.OpenConnectionAsync();
        var cmd = connection.CreateCommand();
        // Opcja B: zeruj LocationId we wszystkich roślinach przed usunięciem
        cmd.CommandText = @"
            UPDATE Plant SET LocationId = NULL WHERE LocationId = @id;
            DELETE FROM Location WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", item.Id);
        await cmd.ExecuteNonQueryAsync();
    }
}
