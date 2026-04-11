namespace HydroGrow.Pages;

public partial class RemindersPage : ContentPage
{
    private readonly RemindersPageModel _pageModel;
    private bool _isFirstAppearance = true;

    public RemindersPage(RemindersPageModel pageModel)
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
