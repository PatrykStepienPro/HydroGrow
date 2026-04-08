namespace HydroGrow.Pages.Controls;

public partial class MeasurementChartView : ContentView
{
    public static readonly BindableProperty MeasurementsProperty =
        BindableProperty.Create(
            nameof(Measurements),
            typeof(List<Measurement>),
            typeof(MeasurementChartView),
            null,
            propertyChanged: (b, _, _) => ((MeasurementChartView)b).RefreshChart());

    private List<ChartPoint> _chartPoints = [];
    private bool _isEmpty = true;
    private string _selectedParam = "Ph";

    public List<Measurement>? Measurements
    {
        get => (List<Measurement>?)GetValue(MeasurementsProperty);
        set => SetValue(MeasurementsProperty, value);
    }

    public List<ChartPoint> ChartPoints
    {
        get => _chartPoints;
        private set { _chartPoints = value; OnPropertyChanged(); }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set { _isEmpty = value; OnPropertyChanged(); }
    }

    public MeasurementChartView()
    {
        InitializeComponent();
    }

    private void RefreshChart()
    {
        var measurements = Measurements;
        if (measurements is null || measurements.Count == 0)
        {
            ChartPoints = [];
            IsEmpty = true;
            return;
        }

        ChartPoints = measurements
            .Select(m => (Date: m.RecordedAtUtc, Value: GetParamValue(m)))
            .Where(p => p.Value.HasValue)
            .OrderBy(p => p.Date)
            .Select(p => new ChartPoint(p.Date, p.Value!.Value))
            .ToList();

        IsEmpty = ChartPoints.Count == 0;
    }

    private double? GetParamValue(Measurement m) => _selectedParam switch
    {
        "Ec" => m.Ec,
        "Tds" => m.Tds,
        _ => m.Ph
    };

    private void OnPhTapped(object? sender, TappedEventArgs e) => SetParam("Ph");
    private void OnEcTapped(object? sender, TappedEventArgs e) => SetParam("Ec");
    private void OnTdsTapped(object? sender, TappedEventArgs e) => SetParam("Tds");

    private void SetParam(string param)
    {
        _selectedParam = param;
        UpdateChipStyles(param);
        RefreshChart();
    }

    private void UpdateChipStyles(string activeParam)
    {
        var activeColor = (Color)Application.Current!.Resources["HydroCyan"];
        var inactiveColor = Application.Current.RequestedTheme == AppTheme.Dark
            ? (Color)Application.Current.Resources["DarkSecondaryBackground"]
            : (Color)Application.Current.Resources["LightSecondaryBackground"];

        PhChip.BackgroundColor = activeParam == "Ph" ? activeColor : inactiveColor;
        EcChip.BackgroundColor = activeParam == "Ec" ? activeColor : inactiveColor;
        TdsChip.BackgroundColor = activeParam == "Tds" ? activeColor : inactiveColor;

        // Update label colors
        var labelColor = (Color)Application.Current!.Resources["DarkOnLightBackground"];
        if (PhChip.Content is Label phLabel)
            phLabel.TextColor = activeParam == "Ph" ? Colors.White : labelColor;
        if (EcChip.Content is Label ecLabel)
            ecLabel.TextColor = activeParam == "Ec" ? Colors.White : labelColor;
        if (TdsChip.Content is Label tdsLabel)
            tdsLabel.TextColor = activeParam == "Tds" ? Colors.White : labelColor;
    }
}

public record ChartPoint(DateTime Date, double Value);
