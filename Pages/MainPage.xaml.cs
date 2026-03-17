using HydroGrow.Models;
using HydroGrow.PageModels;

namespace HydroGrow.Pages;

public partial class MainPage : ContentPage
{
	public MainPage(MainPageModel model)
	{
		InitializeComponent();
		BindingContext = model;
	}
}