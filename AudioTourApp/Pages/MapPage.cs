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

    private async void OnToggleTrackingClicked(object? sender, EventArgs e)
    {
        await _viewModel.ToggleTrackingAsync();
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
                new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.CurrentLocation)),
                new Label { FontSize = 12, TextColor = Color.FromArgb("#8AA0B6") }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedLanguageDisplayText))
            }
        });
        var headerActions = new VerticalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                CreateLanguagePicker(),
                CreateBoundActionButton(nameof(MainViewModel.TrackingActionText), OnToggleTrackingClicked, "#E4B43C", "#17324D")
            }
        };
        Grid.SetColumn(headerActions, 1);
        mapHeader.Add(headerActions);

        var statusStrip = new Grid
        {
            Padding = new Thickness(16, 12, 16, 12),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        statusStrip.Add(CreateInfoChip("Tracking", nameof(MainViewModel.TrackingStatusText), "#EEF5FB", "#17324D"));
        var nearestChip = CreateInfoChip("POI gan nhat", nameof(MainViewModel.NearestPoiSummaryText), "#FFF7E2", "#8B5E00");
        Grid.SetColumn(nearestChip, 1);
        statusStrip.Add(nearestChip);

        var mapLegend = new HorizontalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(16, 0, 16, 14)
        };
        mapLegend.Add(CreateLegendPill("Ban", "#0D6EFD"));
        mapLegend.Add(CreateLegendPill("Gan nhat", "#D9480F"));
        mapLegend.Add(CreateLegendPill("Dang chon", "#F0B429"));
        mapLegend.Add(new Label
        {
            TextColor = Color.FromArgb("#667C92"),
            FontSize = 12,
            VerticalTextAlignment = TextAlignment.Center
        }.Bind(Label.TextProperty, nameof(MainViewModel.VisiblePoisSummary)));

        var webView = new WebView { HeightRequest = 420 };
        webView.Source = new HtmlWebViewSource();
        webView.SetBinding(WebView.SourceProperty, new Binding(nameof(MainViewModel.MapHtml), converter: new HtmlToSourceConverter()));
        webView.Navigating += OnMapNavigating;
        _mapWebView = webView;

        mapCard.Content = new VerticalStackLayout
        {
            Spacing = 0,
            Children = { mapHeader, statusStrip, mapLegend, webView }
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
            .Bind(Image.SourceProperty, "SelectedPoi.ImageUrl", converter: AppImageSourceConverter.Instance));
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
        var bottomSelectedActions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        bottomSelectedActions.Add(CreateActionButton("Mo ban thuyet minh", OnOpenNarrationClicked, "#EEF5FB", "#17324D"));
        var nearestButton = CreateActionButton("POI gan nhat", OnNearestPoiClicked, "#F3F7FB", "#17324D");
        Grid.SetColumn(nearestButton, 1);
        bottomSelectedActions.Add(nearestButton);
        selectedLayout.Add(bottomSelectedActions);
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
                .Bind(Image.SourceProperty, nameof(AudioTourApp.Models.PoiItem.ImageUrl), converter: AppImageSourceConverter.Instance));
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

    private static Button CreateBoundActionButton(string propertyName, EventHandler handler, string backgroundColor, string textColor)
    {
        var button = new Button
        {
            BackgroundColor = textColor.Equals("White", StringComparison.OrdinalIgnoreCase) ? Color.FromArgb(backgroundColor) : Color.FromArgb(backgroundColor),
            TextColor = textColor.Equals("White", StringComparison.OrdinalIgnoreCase) ? Colors.White : Color.FromArgb(textColor),
            CornerRadius = 18
        };
        button.SetBinding(Button.TextProperty, propertyName);
        button.Clicked += handler;
        return button;
    }

    private Picker CreateLanguagePicker()
    {
        var picker = new Picker
        {
            Title = "Ngon ngu",
            ItemDisplayBinding = new Binding("NativeName"),
            WidthRequest = 170,
            BackgroundColor = Color.FromArgb("#F8FAFD"),
            TextColor = Color.FromArgb("#17324D")
        };
        picker.SetBinding(Picker.ItemsSourceProperty, nameof(MainViewModel.Languages));
        picker.SetBinding(Picker.SelectedItemProperty, nameof(MainViewModel.SelectedLanguage), BindingMode.TwoWay);
        picker.SelectedIndexChanged += OnLanguageChanged;
        return picker;
    }

    private static Border CreateInfoChip(string title, string bindingPath, string backgroundColor, string accentColor)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = title,
                    FontSize = 12,
                    TextColor = Color.FromArgb(accentColor)
                },
                new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#17324D"),
                    FontSize = 13,
                    LineBreakMode = LineBreakMode.TailTruncation
                }.Bind(Label.TextProperty, bindingPath)
            }
        };

        return new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb(backgroundColor),
            Padding = new Thickness(12, 10),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = stack
        };
    }

    private static Border CreateLegendPill(string text, string dotColor)
    {
        return new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#F5F8FB"),
            Padding = new Thickness(10, 6),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new BoxView
                    {
                        WidthRequest = 10,
                        HeightRequest = 10,
                        CornerRadius = 5,
                        BackgroundColor = Color.FromArgb(dotColor),
                        VerticalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = text,
                        FontSize = 12,
                        TextColor = Color.FromArgb("#17324D"),
                        VerticalTextAlignment = TextAlignment.Center
                    }
                }
            }
        };
    }
}

internal sealed class HtmlToSourceConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => new HtmlWebViewSource { Html = value?.ToString() ?? string.Empty };

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => string.Empty;
}
