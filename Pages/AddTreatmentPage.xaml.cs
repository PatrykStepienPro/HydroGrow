namespace HydroGrow.Pages;

public partial class AddTreatmentPage : ContentPage
{
    public AddTreatmentPage(AddTreatmentPageModel pageModel)
    {
        InitializeComponent();
        BindingContext = pageModel;
    }
}
