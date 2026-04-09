namespace HydroGrow.Data;

public static class Constants
{
    public const string DatabaseFilename = "AppSQLite.db3";

    private static string DatabasePath =>
        $"Data Source={Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename)};";

    public static async Task<Microsoft.Data.Sqlite.SqliteConnection> OpenConnectionAsync()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection(DatabasePath);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=3000;";
        await cmd.ExecuteNonQueryAsync();
        return connection;
    }

    public static string PhotosDirectory =>
        Path.Combine(FileSystem.AppDataDirectory, "photos");

    public static string BackupsDirectory =>
        Path.Combine(FileSystem.AppDataDirectory, "backups");

    public const int CurrentSchemaVersion = 1;
}