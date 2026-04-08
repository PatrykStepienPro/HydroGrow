using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace HydroGrow.Data;

public class ReminderRepository
{
    private bool _hasBeenInitialized = false;
    private readonly ILogger _logger;

    public ReminderRepository(ILogger<ReminderRepository> logger)
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
                CREATE TABLE IF NOT EXISTS Reminder (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Guid TEXT NOT NULL UNIQUE,
                    PlantId INTEGER,
                    Title TEXT NOT NULL,
                    ReminderType TEXT NOT NULL DEFAULT '',
                    RecurrenceDays INTEGER NOT NULL DEFAULT 7,
                    NextDueAt TEXT NOT NULL,
                    LastTriggeredAt TEXT,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    NotificationId INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS idx_reminder_due ON Reminder(NextDueAt ASC);";
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating Reminder table");
            throw;
        }

        _hasBeenInitialized = true;
    }

    public async Task<List<Reminder>> ListAsync()
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Reminder WHERE IsEnabled = 1 ORDER BY NextDueAt ASC";

        var items = new List<Reminder>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapRow(reader));

        return items;
    }

    public async Task<List<Reminder>> ListAsync(int plantId)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Reminder WHERE PlantId = @plantId ORDER BY NextDueAt ASC";
        cmd.Parameters.AddWithValue("@plantId", plantId);

        var items = new List<Reminder>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapRow(reader));

        return items;
    }

    public async Task<List<Reminder>> GetOverdueAsync()
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var now = DateTime.UtcNow.ToString("O");
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Reminder WHERE IsEnabled = 1 AND NextDueAt <= @now ORDER BY NextDueAt ASC";
        cmd.Parameters.AddWithValue("@now", now);

        var items = new List<Reminder>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapRow(reader));

        return items;
    }

    public async Task<Reminder?> GetAsync(int id)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Reminder WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapRow(reader);

        return null;
    }

    public async Task<int> SaveItemAsync(Reminder item)
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
                INSERT INTO Reminder (Guid, PlantId, Title, ReminderType, RecurrenceDays, NextDueAt, LastTriggeredAt, IsEnabled, NotificationId)
                VALUES (@guid, @plantId, @title, @reminderType, @recurrenceDays, @nextDueAt, @lastTriggeredAt, @isEnabled, @notificationId);
                SELECT last_insert_rowid();";
        }
        else
        {
            cmd.CommandText = @"
                UPDATE Reminder SET PlantId=@plantId, Title=@title, ReminderType=@reminderType,
                    RecurrenceDays=@recurrenceDays, NextDueAt=@nextDueAt, LastTriggeredAt=@lastTriggeredAt,
                    IsEnabled=@isEnabled, NotificationId=@notificationId
                WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", item.Id);
        }

        cmd.Parameters.AddWithValue("@guid", item.Guid);
        cmd.Parameters.AddWithValue("@plantId", item.PlantId.HasValue ? item.PlantId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@title", item.Title);
        cmd.Parameters.AddWithValue("@reminderType", item.ReminderType ?? "");
        cmd.Parameters.AddWithValue("@recurrenceDays", item.RecurrenceDays);
        cmd.Parameters.AddWithValue("@nextDueAt", item.NextDueAt);
        cmd.Parameters.AddWithValue("@lastTriggeredAt", item.LastTriggeredAt != null ? item.LastTriggeredAt : DBNull.Value);
        cmd.Parameters.AddWithValue("@isEnabled", item.IsEnabled);
        cmd.Parameters.AddWithValue("@notificationId", item.NotificationId);

        var result = await cmd.ExecuteScalarAsync();
        if (item.Id == 0)
            item.Id = Convert.ToInt32(result);

        return item.Id;
    }

    public async Task<int> DeleteItemAsync(Reminder item)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Reminder WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", item.Id);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteByPlantAsync(int plantId)
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Reminder WHERE PlantId = @plantId";
        cmd.Parameters.AddWithValue("@plantId", plantId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DropTableAsync()
    {
        await Init();
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS Reminder";
        await cmd.ExecuteNonQueryAsync();
        _hasBeenInitialized = false;
    }

    private static Reminder MapRow(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        Guid = r.GetString(r.GetOrdinal("Guid")),
        PlantId = r.IsDBNull(r.GetOrdinal("PlantId")) ? null : r.GetInt32(r.GetOrdinal("PlantId")),
        Title = r.GetString(r.GetOrdinal("Title")),
        ReminderType = r.IsDBNull(r.GetOrdinal("ReminderType")) ? "" : r.GetString(r.GetOrdinal("ReminderType")),
        RecurrenceDays = r.GetInt32(r.GetOrdinal("RecurrenceDays")),
        NextDueAt = r.GetString(r.GetOrdinal("NextDueAt")),
        LastTriggeredAt = r.IsDBNull(r.GetOrdinal("LastTriggeredAt")) ? null : r.GetString(r.GetOrdinal("LastTriggeredAt")),
        IsEnabled = r.GetInt32(r.GetOrdinal("IsEnabled")),
        NotificationId = r.GetInt32(r.GetOrdinal("NotificationId"))
    };
}
