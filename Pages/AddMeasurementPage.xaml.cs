namespace HydroGrow.Pages;

public partial class AddMeasurementPage : ContentPage
{
    public AddMeasurementPage(AddMeasurementPageModel pageModel)
    {
        InitializeComponent();
        BindingContext = pageModel;
    }
}
