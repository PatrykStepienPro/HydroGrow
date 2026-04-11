namespace HydroGrow.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardPageModel _pageModel;
    private bool _isFirstAppearance = true;

    public DashboardPage(DashboardPageModel pageModel)
    {
        InitializeComponent();
        BindingContext = pageModel;
        _pageModel = pageModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isFirstAppearance)
            return;

        _isFirstAppearance = false;
        await _pageModel.InitializeAsync();
    }
}
