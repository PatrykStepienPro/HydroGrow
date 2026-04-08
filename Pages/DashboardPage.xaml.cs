namespace HydroGrow.Pages;

public partial class DashboardPage : ContentPage
{
    public DashboardPage(DashboardPageModel pageModel)
    {
        InitializeComponent();
        BindingContext = pageModel;
    }
}
