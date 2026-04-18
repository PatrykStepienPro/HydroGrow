using HydroGrow.Models;
using HydroGrow.Services;

namespace HydroGrow.Pages.Controls;

public partial class SpeciesAutocompleteView : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(SpeciesAutocompleteView),
            string.Empty,
            BindingMode.TwoWay,
            propertyChanged: (b, _, n) =>
            {
                var view = (SpeciesAutocompleteView)b;
                // Only search when user is actively typing (entry is focused)
                if (view.SpeciesEntry?.IsFocused == true)
                    view.UpdateSuggestionsAsync((string?)n ?? string.Empty).FireAndForgetSafeAsync();
            });

    public static readonly BindableProperty SuggestionsVisibleProperty =
        BindableProperty.Create(
            nameof(SuggestionsVisible),
            typeof(bool),
            typeof(SpeciesAutocompleteView),
            false);

    public static readonly BindableProperty SuggestionsProperty =
        BindableProperty.Create(
            nameof(Suggestions),
            typeof(IReadOnlyList<PlantSpeciesEntry>),
            typeof(SpeciesAutocompleteView),
            Array.Empty<PlantSpeciesEntry>());

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool SuggestionsVisible
    {
        get => (bool)GetValue(SuggestionsVisibleProperty);
        private set => SetValue(SuggestionsVisibleProperty, value);
    }

    public IReadOnlyList<PlantSpeciesEntry> Suggestions
    {
        get => (IReadOnlyList<PlantSpeciesEntry>)GetValue(SuggestionsProperty);
        private set => SetValue(SuggestionsProperty, value);
    }

    private PlantSpeciesService? _speciesServiceCache;
    private PlantSpeciesService SpeciesService =>
        _speciesServiceCache ??= Application.Current!.Handler!.MauiContext!
            .Services.GetRequiredService<PlantSpeciesService>();

    public SpeciesAutocompleteView()
    {
        InitializeComponent();

        SpeciesEntry.Unfocused += (_, _) =>
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), HideSuggestions);
    }

    private void OnSuggestionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not PlantSpeciesEntry entry)
            return;

        HideSuggestions();
        SpeciesEntry.Unfocus(); // Unfocus BEFORE setting Text so propertyChanged won't search
        SuggestionList.SelectedItem = null;
        Text = entry.DisplayName;
    }

    private async Task UpdateSuggestionsAsync(string query)
    {
        var results = await SpeciesService.SearchAsync(query);
        // Ensure UI updates happen on main thread (async continuation may be on thread pool)
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Suggestions = results;
            SuggestionsVisible = results.Count > 0;
        });
    }

    private void HideSuggestions()
    {
        SuggestionsVisible = false;
        Suggestions = [];
    }
}
