namespace HydroGrow.Pages;

public partial class PlantDetailPage : ContentPage
{
    public PlantDetailPage(PlantDetailPageModel pageModel)
    {
        InitializeComponent();
        BindingContext = pageModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PlantDetailPageModel vm)
            vm.RefreshCommand.Execute(null);
    }
}
