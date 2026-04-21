namespace HydroGrow.Pages;

public partial class ManageLocationsPage : ContentPage
{
    private readonly PageModels.ManageLocationsPageModel _pageModel;
    private bool _isFirstAppearance = true;

    public ManageLocationsPage(PageModels.ManageLocationsPageModel pageModel)
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
        await _pageModel.LoadAsync();
    }
}
