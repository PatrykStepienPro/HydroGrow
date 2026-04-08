namespace HydroGrow.Pages;

public partial class AddEditPlantPage : ContentPage
{
    public AddEditPlantPage(AddEditPlantPageModel pageModel)
    {
        InitializeComponent();
        BindingContext = pageModel;
    }
}
