namespace HydroGrow.Pages;

public partial class PlantListPage : ContentPage
{
    private readonly PlantListPageModel _pageModel;
    private bool _isFirstAppearance = true;
    
    public PlantListPage(PlantListPageModel pageModel)
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
