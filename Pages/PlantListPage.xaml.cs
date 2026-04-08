namespace HydroGrow.Pages;

public partial class PlantListPage : ContentPage
{
    public PlantListPage(PlantListPageModel pageModel)
    {
        InitializeComponent();
        BindingContext = pageModel;
    }
}
