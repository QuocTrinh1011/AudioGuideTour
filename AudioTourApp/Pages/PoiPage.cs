using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class PoiPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public PoiPage(MainViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        Title = "POI";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        Content = BuildContent();
    }

    private async void OnPlaySelectedClicked(object? sender, EventArgs e)
    {
        await _viewModel.PlaySelectedAsync();
    }

    private async void OnOpenMapClicked(object? sender, EventArgs e)
    {
        await _viewModel.OpenSelectedMapAsync();
    }

    private async void OnOpenPoiDetailsClicked(object? sender, EventArgs e)
    {
        await _viewModel.OpenSelectedPoiDetailsAsync();
    }

    private async void OnOpenNarrationClicked(object? sender, EventArgs e)
    {
        await _viewModel.OpenSelectedNarrationAsync();
    }

    private async void OnDiagnoseAudioClicked(object? sender, EventArgs e)
    {
        await _viewModel.RunSelectedPoiAudioDiagnosticsAsync();
    }

    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        await _viewModel.ChangeLanguageAsync();
    }

    private void OnNearestPoiClicked(object? sender, EventArgs e)
    {
        _viewModel.SelectNearestPoi();
    }

    private void OnPreviousPoiClicked(object? sender, EventArgs e)
    {
        _viewModel.SelectPreviousPoi();
    }

    private void OnNextPoiClicked(object? sender, EventArgs e)
    {
        _viewModel.SelectNextPoi();
    }

    private void OnResetFiltersClicked(object? sender, EventArgs e)
    {
        _viewModel.ResetPoiFilters();
    }

    private View BuildContent()
    {
        var root = new VerticalStackLayout
        {
            Padding = new Thickness(18, 18, 18, 28),
            Spacing = 18
        };

        root.Add(new Border
        {
            StrokeThickness = 0,
            Padding = 18,
            StrokeShape = new RoundRectangle { CornerRadius = 26 },
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new(Color.FromArgb("#17324D"), 0f),
                    new(Color.FromArgb("#2C6278"), 0.65f),
                    new(Color.FromArgb("#5E9B8B"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "Thu vien POI", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = "Duyet tat ca diem thuyet minh, xem nhanh noi dung va mo ban thuyet minh day du.", TextColor = Color.FromArgb("#E4EEF7") }
                }
            }
        });

        var filterCard = CreateCard();
        var filterLayout = new VerticalStackLayout { Spacing = 12 };
        filterLayout.Add(new Label { Text = "Tim kiem va loc", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        filterLayout.Add(new Label { TextColor = Color.FromArgb("#667C92"), FontSize = 12 }
            .Bind(Label.TextProperty, nameof(MainViewModel.SelectedLanguageDisplayText)));
        filterLayout.Add(CreateLanguagePicker());
        filterLayout.Add(new Entry
        {
            Placeholder = "Tim theo ten, mo ta, dia chi...",
            BackgroundColor = Color.FromArgb("#F8FAFD"),
            TextColor = Color.FromArgb("#17324D")
        }.Bind(Entry.TextProperty, nameof(MainViewModel.PoiSearchText), BindingMode.TwoWay));
        var categoryPicker = new Picker
        {
            Title = "Loc theo danh muc",
            BackgroundColor = Color.FromArgb("#F8FAFD"),
            TextColor = Color.FromArgb("#17324D")
        };
        categoryPicker.SetBinding(Picker.ItemsSourceProperty, nameof(MainViewModel.CategoryFilterOptions));
        categoryPicker.SetBinding(Picker.SelectedItemProperty, nameof(MainViewModel.SelectedCategoryFilter), BindingMode.TwoWay);
        filterLayout.Add(categoryPicker);
        var filterActions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        filterActions.Add(CreateActionButton("POI gan nhat", OnNearestPoiClicked, "#EEF3F8", "#17324D"));
        var resetButton = CreateActionButton("Xoa bo loc", OnResetFiltersClicked, "#F5F8FB", "#17324D");
        Grid.SetColumn(resetButton, 1);
        filterActions.Add(resetButton);
        filterLayout.Add(filterActions);
        filterLayout.Add(new Label { TextColor = Color.FromArgb("#667C92"), FontSize = 12 }
            .Bind(Label.TextProperty, nameof(MainViewModel.VisiblePoisSummary)));
        filterCard.Content = filterLayout;
        root.Add(filterCard);

        var selectedCard = CreateCard();
        var selectedLayout = new VerticalStackLayout { Spacing = 12 };
        selectedLayout.Add(new Label { Text = "POI dang chon", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        selectedLayout.Add(new Image { HeightRequest = 220, Aspect = Aspect.AspectFill, BackgroundColor = Color.FromArgb("#E8EDF3") }
            .Bind(Image.SourceProperty, "SelectedPoi.ImageUrl", converter: AppImageSourceConverter.Instance));
        selectedLayout.Add(new Label { FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") }
            .Bind(Label.TextProperty, "SelectedPoi.Title"));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#667C92"), FontSize = 12 }
            .Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiPositionText)));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#5D7287") }.Bind(Label.TextProperty, "SelectedPoi.Summary"));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#667C92"), FontSize = 12 }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiMetaText)));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#17324D"), FontAttributes = FontAttributes.Bold }
            .Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiNarrationSourceText)));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#31485F"), MaxLines = 4, LineBreakMode = LineBreakMode.TailTruncation }
            .Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiNarrationText)));

        var selectedActions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        selectedActions.Add(CreateActionButton("Nghe ngay", OnPlaySelectedClicked, "#17324D", "White"));
        var narrationButton = CreateActionButton("Ban thuyet minh", OnOpenNarrationClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(narrationButton, 1);
        selectedActions.Add(narrationButton);
        selectedLayout.Add(selectedActions);

        selectedLayout.Add(CreateActionButton("Chan doan audio", OnDiagnoseAudioClicked, "#EEF5FB", "#17324D"));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#35526B") }.Bind(Label.TextProperty, nameof(MainViewModel.Status)));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#35526B"), FontAttributes = FontAttributes.Bold }
            .Bind(Label.TextProperty, nameof(MainViewModel.PlaybackStatusText)));
        selectedLayout.Add(new Label
        {
            TextColor = Color.FromArgb("#667C92"),
            FontSize = 12,
            LineBreakMode = LineBreakMode.WordWrap
        }.Bind(Label.TextProperty, nameof(MainViewModel.AudioDiagnosticsSummary)));

        var navigationActions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        navigationActions.Add(CreateActionButton("POI truoc", OnPreviousPoiClicked, "#F5F8FB", "#17324D"));
        var nearestButton = CreateActionButton("Gan nhat", OnNearestPoiClicked, "#EEF3F8", "#17324D");
        Grid.SetColumn(nearestButton, 1);
        navigationActions.Add(nearestButton);
        var nextButton = CreateActionButton("POI tiep", OnNextPoiClicked, "#F5F8FB", "#17324D");
        Grid.SetColumn(nextButton, 2);
        navigationActions.Add(nextButton);
        selectedLayout.Add(navigationActions);

        var secondActions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        secondActions.Add(CreateActionButton("Mo ban do", OnOpenMapClicked, "#EEF3F8", "#17324D"));
        var detailButton = CreateActionButton("Chi tiet", OnOpenPoiDetailsClicked, "#F3F7FB", "#17324D");
        Grid.SetColumn(detailButton, 1);
        secondActions.Add(detailButton);
        selectedLayout.Add(secondActions);
        selectedCard.Content = selectedLayout;
        root.Add(selectedCard);

        var listCard = CreateCard();
        var listLayout = new VerticalStackLayout { Spacing = 12 };
        listLayout.Add(new Label { Text = "Tat ca diem thuyet minh", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        var collection = new CollectionView { SelectionMode = SelectionMode.Single };
        collection.SetBinding(ItemsView.ItemsSourceProperty, nameof(MainViewModel.VisiblePois));
        collection.SetBinding(SelectableItemsView.SelectedItemProperty, nameof(MainViewModel.SelectedPoi), BindingMode.TwoWay);
        collection.ItemTemplate = new DataTemplate(() =>
        {
            var card = new Border
            {
                Stroke = Color.FromArgb("#E3EAF2"),
                BackgroundColor = Color.FromArgb("#FBFCFE"),
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = 12,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(92),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 12
            };

            grid.Add(new Image { HeightRequest = 88, WidthRequest = 92, Aspect = Aspect.AspectFill, BackgroundColor = Color.FromArgb("#E8EDF3") }
                .Bind(Image.SourceProperty, "ImageUrl", converter: AppImageSourceConverter.Instance));

            var details = new VerticalStackLayout { Spacing = 4 };
            details.Add(new Label { FontAttributes = FontAttributes.Bold, FontSize = 17, TextColor = Color.FromArgb("#17324D") }
                .Bind(Label.TextProperty, "Title"));
            details.Add(new Label { TextColor = Color.FromArgb("#5D7287"), MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation }
                .Bind(Label.TextProperty, "Summary"));
            details.Add(new Label { TextColor = Color.FromArgb("#17324D") }
                .Bind(Label.TextProperty, "DistanceMeters", stringFormat: "Khoang cach: {0:F0}m"));
            details.Add(new Label { TextColor = Color.FromArgb("#73869A") }
                .Bind(Label.TextProperty, "Language", stringFormat: "Ngon ngu: {0}"));
            details.Add(new Label { TextColor = Color.FromArgb("#8A9BAA"), FontSize = 12 }
                .Bind(Label.TextProperty, "Category", stringFormat: "Danh muc: {0}"));
            Grid.SetColumn(details, 1);
            grid.Add(details);
            card.Content = grid;
            return card;
        });
        listLayout.Add(collection);
        listCard.Content = listLayout;
        root.Add(listCard);

        return new ScrollView { Content = root };
    }

    private static Border CreateCard() => new()
    {
        Stroke = Color.FromArgb("#D9E3EE"),
        BackgroundColor = Colors.White,
        Padding = 16,
        StrokeShape = new RoundRectangle { CornerRadius = 24 }
    };

    private static Button CreateActionButton(string text, EventHandler handler, string backgroundColor, string textColor)
    {
        var button = new Button
        {
            Text = text,
            BackgroundColor = textColor.Equals("White", StringComparison.OrdinalIgnoreCase) ? Color.FromArgb(backgroundColor) : Color.FromArgb(backgroundColor),
            TextColor = textColor.Equals("White", StringComparison.OrdinalIgnoreCase) ? Colors.White : Color.FromArgb(textColor),
            CornerRadius = 18
        };
        button.Clicked += handler;
        return button;
    }

    private Picker CreateLanguagePicker()
    {
        var picker = new Picker
        {
            Title = "Chon ngon ngu noi dung",
            ItemDisplayBinding = new Binding("NativeName"),
            BackgroundColor = Color.FromArgb("#F8FAFD"),
            TextColor = Color.FromArgb("#17324D")
        };
        picker.SetBinding(Picker.ItemsSourceProperty, nameof(MainViewModel.Languages));
        picker.SetBinding(Picker.SelectedItemProperty, nameof(MainViewModel.SelectedLanguage), BindingMode.TwoWay);
        picker.SelectedIndexChanged += OnLanguageChanged;
        return picker;
    }
}
