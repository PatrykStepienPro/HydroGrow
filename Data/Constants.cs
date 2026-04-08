namespace HydroGrow.Data;

public static class Constants
{
    public const string DatabaseFilename = "AppSQLite.db3";

    public static string DatabasePath =>
        $"Data Source={Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename)};Pragma journal_mode=WAL;Pragma busy_timeout=3000;";

    public static string PhotosDirectory =>
        Path.Combine(FileSystem.AppDataDirectory, "photos");

    public static string BackupsDirectory =>
        Path.Combine(FileSystem.AppDataDirectory, "backups");

    public const int CurrentSchemaVersion = 1;
}