using System.Windows.Input;

namespace HydroGrow.Pages.Controls;

public partial class PhotoGalleryView : ContentView
{
    public static readonly BindableProperty PhotosProperty =
        BindableProperty.Create(
            nameof(Photos),
            typeof(List<PlantPhoto>),
            typeof(PhotoGalleryView),
            null,
            propertyChanged: (b, _, n) =>
            {
                var view = (PhotoGalleryView)b;
                BindableLayout.SetItemsSource(view.PhotosList, (IEnumerable<PlantPhoto>?)n);
            });

    public static readonly BindableProperty AddPhotoCommandProperty =
        BindableProperty.Create(nameof(AddPhotoCommand), typeof(ICommand), typeof(PhotoGalleryView));

    public static readonly BindableProperty DeletePhotoCommandProperty =
        BindableProperty.Create(nameof(DeletePhotoCommand), typeof(ICommand), typeof(PhotoGalleryView));

    public List<PlantPhoto>? Photos
    {
        get => (List<PlantPhoto>?)GetValue(PhotosProperty);
        set => SetValue(PhotosProperty, value);
    }

    public ICommand? AddPhotoCommand
    {
        get => (ICommand?)GetValue(AddPhotoCommandProperty);
        set => SetValue(AddPhotoCommandProperty, value);
    }

    public ICommand? DeletePhotoCommand
    {
        get => (ICommand?)GetValue(DeletePhotoCommandProperty);
        set => SetValue(DeletePhotoCommandProperty, value);
    }

    public PhotoGalleryView()
    {
        InitializeComponent();
    }
}
