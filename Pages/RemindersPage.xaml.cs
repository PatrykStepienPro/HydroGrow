namespace HydroGrow.Pages;

public partial class RemindersPage : ContentPage
{
    public RemindersPage(RemindersPageModel pageModel)
    {
        InitializeComponent();
        BindingContext = pageModel;
    }
}
