using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Serilog;
using Plugin.LocalNotification;
using Syncfusion.Maui.Toolkit.Hosting;

namespace HydroGrow;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        try
        {
        return CreateMauiAppInternal();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CRASH] CreateMauiApp failed: {ex}");
            throw;
        }
    }

    private static MauiApp CreateMauiAppInternal()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureSyncfusionToolkit()
            .UseLocalNotification()
            .ConfigureMauiHandlers(handlers =>
            {
#if WINDOWS
                Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping(
                    "KeyboardAccessibleCollectionView", (handler, view) =>
                    {
                        handler.PlatformView.SingleSelectionFollowsFocus = false;
                    });
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
            });

#if DEBUG
        builder.Logging.AddDebug();
        builder.Services.AddLogging(configure => configure.AddDebug());

        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.File(
                Path.Combine(FileSystem.AppDataDirectory, "logs", "hydrogrow-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        builder.Logging.AddSerilog(serilogLogger, dispose: true);
#endif

        // Repositories (Singleton — shared DB access, per-call connections)
        builder.Services.AddSingleton<PlantRepository>();
        builder.Services.AddSingleton<MeasurementRepository>();
        builder.Services.AddSingleton<TreatmentRepository>();
        builder.Services.AddSingleton<ReminderRepository>();
        builder.Services.AddSingleton<PhotoRepository>();
        builder.Services.AddSingleton<MeasurementRangeRepository>();
        builder.Services.AddSingleton<LocationRepository>();

        // Services
        builder.Services.AddSingleton<SeedDataService>();
        builder.Services.AddSingleton<IErrorHandler, ModalErrorHandler>();
        builder.Services.AddSingleton<PhotoService>();
        builder.Services.AddSingleton<ExportService>();
        builder.Services.AddSingleton<ImportService>();
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<PlantSpeciesService>();

        // Tab page models (Singleton — persists state across tab switches)
        builder.Services.AddSingleton<DashboardPageModel>();
        builder.Services.AddSingleton<PlantListPageModel>();
        builder.Services.AddSingleton<RemindersPageModel>();
        builder.Services.AddSingleton<SettingsPageModel>();

        // Tab pages (Singleton)
        builder.Services.AddSingleton<Pages.DashboardPage>();
        builder.Services.AddSingleton<Pages.PlantListPage>();
        builder.Services.AddSingleton<Pages.RemindersPage>();
        builder.Services.AddSingleton<Pages.SettingsPage>();

        // Detail pages (Transient — fresh state per navigation, query params)
        builder.Services.AddTransientWithShellRoute<Pages.PlantDetailPage, PlantDetailPageModel>("plant");
        builder.Services.AddTransientWithShellRoute<Pages.AddEditPlantPage, AddEditPlantPageModel>("plant-edit");
        builder.Services.AddTransientWithShellRoute<Pages.AddMeasurementPage, AddMeasurementPageModel>("plant-measure");
        builder.Services.AddTransientWithShellRoute<Pages.AddTreatmentPage, AddTreatmentPageModel>("plant-treat");
        builder.Services.AddTransientWithShellRoute<Pages.ManageLocationsPage, ManageLocationsPageModel>("manage-locations");

        return builder.Build();
    }
}

