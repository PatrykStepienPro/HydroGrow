using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Font = Microsoft.Maui.Font;

namespace HydroGrow;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("plant", typeof(Pages.PlantDetailPage));
        Routing.RegisterRoute("plant-edit", typeof(Pages.AddEditPlantPage));
        Routing.RegisterRoute("plant-measure", typeof(Pages.AddMeasurementPage));
        Routing.RegisterRoute("plant-treat", typeof(Pages.AddTreatmentPage));
        Routing.RegisterRoute("manage-locations", typeof(Pages.ManageLocationsPage));
    }

    public static async Task DisplaySnackbarAsync(string message)
    {
        var snackbarOptions = new SnackbarOptions
        {
            BackgroundColor = Color.FromArgb("#2ECC71"),
            TextColor = Colors.White,
            CornerRadius = new CornerRadius(8),
            Font = Font.SystemFontOfSize(16)
        };

        var snackbar = Snackbar.Make(message, visualOptions: snackbarOptions);
        await snackbar.Show(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
    }

    public static async Task DisplayToastAsync(string message)
    {
        if (OperatingSystem.IsWindows())
            return;

        var toast = Toast.Make(message, textSize: 16);
        await toast.Show(new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);
    }
}
