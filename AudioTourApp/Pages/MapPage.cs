using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class MapPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private WebView? _mapWebView;

    public MapPage(MainViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        Title = "Map";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        Content = BuildContent();
    }

    private async void OnToggleTrackingClicked(object sender, EventArgs e)
    {
        await _viewModel.ToggleTrackingAsync();
    }

    private async void OnPlaySelectedClicked(object sender, EventArgs e)
    {
        await _viewModel.PlaySelectedAsync();
    }

    private async void OnOpenMapClicked(object sender, EventArgs e)
    {
        await _viewModel.OpenSelectedMapAsync();
    }

    private async void OnOpenPoiDetailsClicked(object sender, EventArgs e)
    {
        await _viewModel.OpenSelectedPoiDetailsAsync();
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
                    new(Color.FromArgb("#2E6C7B"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "Ban do va Geofence", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = "Theo doi vi tri, highlight POI gan nhat va mo nhanh noi dung can nghe.", TextColor = Color.FromArgb("#E4EEF7") }
                }
            }
        });

        var mapCard = new Border
        {
            Stroke = Color.FromArgb("#D9E3EE"),
            BackgroundColor = Colors.White,
            Padding = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 24 }
        };

        var mapHeader = new Grid
        {
            Padding = new Thickness(16, 16, 16, 0),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        mapHeader.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = "Ban do hien tai", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") },
                new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.CurrentLocation))
            }
        });
        var trackingButton = CreateActionButton(string.Empty, OnToggleTrackingClicked, "#E4B43C", "#17324D");
        trackingButton.SetBinding(Button.TextProperty, nameof(MainViewModel.TrackingActionText));
        Grid.SetColumn(trackingButton, 1);
        mapHeader.Add(trackingButton);

        var webView = new WebView { HeightRequest = 380 };
        webView.Source = new HtmlWebViewSource();
        webView.SetBinding(WebView.SourceProperty, new Binding(nameof(MainViewModel.MapHtml), converter: new HtmlToSourceConverter()));
        webView.Navigating += OnMapNavigating;
        _mapWebView = webView;

        mapCard.Content = new VerticalStackLayout
        {
            Spacing = 0,
            Children = { mapHeader, webView }
        };
        root.Add(mapCard);

        var selectedCard = new Border
        {
            Stroke = Color.FromArgb("#D9E3EE"),
            BackgroundColor = Colors.White,
            Padding = 16,
            StrokeShape = new RoundRectangle { CornerRadius = 24 }
        };
        var selectedLayout = new VerticalStackLayout { Spacing = 12 };
        selectedLayout.Add(new Label { Text = "POI dang duoc chon tren ban do", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        selectedLayout.Add(new Image { HeightRequest = 200, Aspect = Aspect.AspectFill, BackgroundColor = Color.FromArgb("#E8EDF3") }
            .Bind(Image.SourceProperty, "SelectedPoi.ImageUrl"));
        selectedLayout.Add(new Label { FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") }
            .Bind(Label.TextProperty, "SelectedPoi.Title"));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiMetaText)));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#8AA0B6"), FontSize = 12 }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiCoordinateText)));
        selectedLayout.Add(new Label { Text = "Ban thuyet minh", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        selectedLayout.Add(new Label
        {
            TextColor = Color.FromArgb("#31485F"),
            MaxLines = 5,
            LineBreakMode = LineBreakMode.TailTruncation
        }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiNarrationText)));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiAudioText)));
        var selectedActions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        selectedActions.Add(CreateActionButton("Nghe ngay", OnPlaySelectedClicked, "#17324D", "White"));
        var selectedMapButton = CreateActionButton("Mo ban do", OnOpenMapClicked, "#EEF3F8", "#17324D");
        Grid.SetColumn(selectedMapButton, 1);
        selectedActions.Add(selectedMapButton);
        var selectedDetailButton = CreateActionButton("Chi tiet", OnOpenPoiDetailsClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(selectedDetailButton, 2);
        selectedActions.Add(selectedDetailButton);
        selectedLayout.Add(selectedActions);
        selectedCard.Content = selectedLayout;
        root.Add(selectedCard);

        var poisCard = new Border
        {
            Stroke = Color.FromArgb("#D9E3EE"),
            BackgroundColor = Colors.White,
            Padding = 16,
            StrokeShape = new RoundRectangle { CornerRadius = 24 }
        };
        var poisLayout = new VerticalStackLayout { Spacing = 12 };
        poisLayout.Add(new Label { Text = "Tat ca diem thuyet minh", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        poisLayout.Add(new Entry
        {
            Placeholder = "Tim theo ten, tom tat, dia chi...",
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
        poisLayout.Add(categoryPicker);
        poisLayout.Add(new Label { TextColor = Color.FromArgb("#667C92"), FontSize = 12 }
            .Bind(Label.TextProperty, nameof(MainViewModel.VisiblePoisSummary)));

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
                    new ColumnDefinition(100),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 12
            };
            grid.Add(new Image { HeightRequest = 90, WidthRequest = 100, Aspect = Aspect.AspectFill, BackgroundColor = Color.FromArgb("#E8EDF3") }
                .Bind(Image.SourceProperty, nameof(AudioTourApp.Models.PoiItem.ImageUrl)));
            var details = new VerticalStackLayout { Spacing = 4 };
            details.Add(new Label { FontAttributes = FontAttributes.Bold, FontSize = 17, TextColor = Color.FromArgb("#17324D") }
                .Bind(Label.TextProperty, nameof(AudioTourApp.Models.PoiItem.Title)));
            details.Add(new Label { TextColor = Color.FromArgb("#5D7287"), MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation }
                .Bind(Label.TextProperty, nameof(AudioTourApp.Models.PoiItem.Summary)));
            details.Add(new Label { TextColor = Color.FromArgb("#17324D") }
                .Bind(Label.TextProperty, nameof(AudioTourApp.Models.PoiItem.DistanceMeters), stringFormat: "Khoang cach: {0:F0}m"));
            details.Add(new Label { TextColor = Color.FromArgb("#73869A") }
                .Bind(Label.TextProperty, nameof(AudioTourApp.Models.PoiItem.TriggerMode), stringFormat: "Kich hoat: {0}"));
            details.Add(new Label { TextColor = Color.FromArgb("#8A9BAA"), FontSize = 12 }
                .Bind(Label.TextProperty, nameof(AudioTourApp.Models.PoiItem.Category), stringFormat: "Danh muc: {0}"));
            Grid.SetColumn(details, 1);
            grid.Add(details);
            card.Content = grid;
            return card;
        });
        poisLayout.Add(collection);

        var actions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        actions.Add(CreateActionButton("Nghe POI da chon", OnPlaySelectedClicked, "#17324D", "White"));
        var openMapButton = CreateActionButton("Mo ban do ngoai", OnOpenMapClicked, "#EEF3F8", "#17324D");
        Grid.SetColumn(openMapButton, 1);
        actions.Add(openMapButton);
        poisLayout.Add(actions);
        poisLayout.Add(CreateActionButton("Xem chi tiet POI", OnOpenPoiDetailsClicked, "#E4B43C", "#17324D"));
        poisCard.Content = poisLayout;
        root.Add(poisCard);

        return new ScrollView { Content = root };
    }

    private void OnMapNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Url) || !e.Url.StartsWith("audiotour://poi/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        e.Cancel = true;
        if (int.TryParse(e.Url["audiotour://poi/".Length..], out var poiId))
        {
            _viewModel.SelectPoiById(poiId);
        }
    }

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
}

internal sealed class HtmlToSourceConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => new HtmlWebViewSource { Html = value?.ToString() ?? string.Empty };

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => string.Empty;
}
