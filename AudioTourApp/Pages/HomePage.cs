using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class HomePage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private bool _didAutoBootstrap;

    public HomePage(MainViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        Title = "Trang chủ";
        BackgroundColor = Color.FromArgb("#F3F6FA");

        ToolbarItems.Add(new ToolbarItem("POI", null, OnOpenPoiLibraryClicked));
        ToolbarItems.Add(new ToolbarItem("Cài đặt", null, OnOpenSettingsClicked));

        Content = BuildContent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_didAutoBootstrap)
        {
            return;
        }

        _didAutoBootstrap = true;
        _ = _viewModel.BootstrapAsync();
        _ = _viewModel.RefreshPermissionStatusAsync();
    }

    private async void OnBootstrapClicked(object? sender, EventArgs e)
    {
        await _viewModel.BootstrapAsync();
    }

    private async void OnToggleTrackingClicked(object? sender, EventArgs e)
    {
        await _viewModel.ToggleTrackingAsync();
    }

    private async void OnPlaySelectedClicked(object? sender, EventArgs e)
    {
        await _viewModel.PlaySelectedAsync();
    }

    private async void OnStopPlaybackClicked(object? sender, EventArgs e)
    {
        await _viewModel.StopPlaybackAsync();
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

    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        await _viewModel.ChangeLanguageAsync();
    }

    private async void OnOpenPoiLibraryClicked()
    {
        await Navigation.PushAsync(new PoiPage(_viewModel));
    }

    private async void OnOpenSettingsClicked()
    {
        await Navigation.PushAsync(new SettingsPage(_viewModel));
    }

    private async void OnOpenQrTabClicked(object? sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//qr");
        }
    }

    private async void OnOpenMapTabClicked(object? sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//map");
        }
    }

    private View BuildContent()
    {
        var root = new VerticalStackLayout
        {
            Padding = new Thickness(18, 18, 18, 28),
            Spacing = 18
        };

        var hero = new Border
        {
            StrokeThickness = 0,
            Padding = 18,
            StrokeShape = new RoundRectangle { CornerRadius = 28 },
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new(Color.FromArgb("#16324B"), 0.0f),
                    new(Color.FromArgb("#245A76"), 0.55f),
                    new(Color.FromArgb("#E4B43C"), 1.0f)
                },
                new Point(0, 0),
                new Point(1, 1))
        };

        var heroGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };

        heroGrid.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label
                {
                    Text = "Audio Tour Vĩnh Khánh",
                    FontSize = 28,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White
                },
                new Label
                {
                    Text = "Nghe thuyết minh đa ngôn ngữ, quét QR tại điểm dừng và khám phá phố ẩm thực ngay trên điện thoại.",
                    TextColor = Color.FromArgb("#E9F2FB"),
                    LineBreakMode = LineBreakMode.WordWrap
                }
            }
        });

        var trackingBadge = new Border
        {
            BackgroundColor = Color.FromArgb("#33FFFFFF"),
            Padding = new Thickness(12, 8),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            VerticalOptions = LayoutOptions.Start,
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label { Text = "Định vị", FontSize = 12, TextColor = Color.FromArgb("#D6E4F1") },
                    new Label { FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
                        .Bind(Label.TextProperty, nameof(MainViewModel.TrackingStatusText))
                }
            }
        };
        Grid.SetColumn(trackingBadge, 1);
        heroGrid.Add(trackingBadge);

        var statusCard = new Border
        {
            BackgroundColor = Color.FromArgb("#F9FBFD"),
            Padding = 14,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "Trạng thái hiện tại", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") },
                    new Label { Text = "Ngôn ngữ nghe", FontSize = 12, TextColor = Color.FromArgb("#8AA0B6") },
                    CreateLanguagePicker(),
                    new Label { TextColor = Color.FromArgb("#35526B") }.Bind(Label.TextProperty, nameof(MainViewModel.Status)),
                    new Label { FontSize = 13, TextColor = Color.FromArgb("#5D7287") }.Bind(Label.TextProperty, nameof(MainViewModel.CurrentLocation)),
                    new Label { FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") }.Bind(Label.TextProperty, nameof(MainViewModel.PlaybackStatusText))
                }
            }
        };

        hero.Content = new VerticalStackLayout
        {
            Spacing = 14,
            Children = { heroGrid, statusCard }
        };
        root.Add(hero);

        var quickGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };

        var qrCard = CreateCard();
        var qrLayout = new VerticalStackLayout { Spacing = 10 };
        qrLayout.Add(new Label { Text = "Quét mã QR", FontAttributes = FontAttributes.Bold, FontSize = 18, TextColor = Color.FromArgb("#17324D") });
        qrLayout.Add(new Label { Text = "Mở nội dung ngay tại điểm dừng xe buýt hoặc điểm thuyết minh gần bạn.", TextColor = Color.FromArgb("#667C92"), FontSize = 13 });
        qrLayout.Add(CreateActionButton("Mở màn quét QR", OnOpenQrTabClicked, "#17324D", "White"));
        qrLayout.Add(new Label { Text = "Nếu cần, bạn vẫn có thể nhập mã thủ công ở màn QR.", TextColor = Color.FromArgb("#8AA0B6"), FontSize = 12 });
        qrCard.Content = qrLayout;
        quickGrid.Add(qrCard);

        var actionCard = CreateCard();
        var actionLayout = new VerticalStackLayout { Spacing = 10 };
        actionLayout.Add(new Label { Text = "Khám phá quanh bạn", FontAttributes = FontAttributes.Bold, FontSize = 18, TextColor = Color.FromArgb("#17324D") });
        actionLayout.Add(new Label { Text = "Xem bản đồ, làm mới dữ liệu và bật định vị để app tự phát đúng lúc.", TextColor = Color.FromArgb("#667C92"), FontSize = 13 });
        actionLayout.Add(CreateActionButton("Xem bản đồ", OnOpenMapTabClicked, "#E4B43C", "#17324D"));
        actionLayout.Add(CreateBoundActionButton(nameof(MainViewModel.TrackingActionText), OnToggleTrackingClicked, "#EEF3F8", "#17324D"));
        actionLayout.Add(CreateActionButton("Làm mới nội dung", OnBootstrapClicked, "#F5F8FB", "#17324D"));
        actionCard.Content = actionLayout;
        Grid.SetColumn(actionCard, 1);
        quickGrid.Add(actionCard);
        root.Add(quickGrid);

        var selectedPoiCard = CreateCard();
        var selectedPoiLayout = new VerticalStackLayout { Spacing = 14 };

        var selectedHeader = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        selectedHeader.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = "POI nổi bật gần bạn", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") },
                new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiSubtitle))
            }
        });
        var langBadge = new Border
        {
            BackgroundColor = Color.FromArgb("#17324D"),
            Padding = new Thickness(12, 8),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            VerticalOptions = LayoutOptions.Start,
            Content = new Label { TextColor = Colors.White }
                .Bind(Label.TextProperty, "SelectedPoi.Language")
        };
        Grid.SetColumn(langBadge, 1);
        selectedHeader.Add(langBadge);
        selectedPoiLayout.Add(selectedHeader);

        var selectedBody = new Border
        {
            Stroke = Color.FromArgb("#E3EAF2"),
            BackgroundColor = Color.FromArgb("#FBFCFE"),
            Padding = 12,
            StrokeShape = new RoundRectangle { CornerRadius = 20 }
        };
        var bodyLayout = new VerticalStackLayout { Spacing = 10 };
        bodyLayout.Add(new Image { HeightRequest = 220, Aspect = Aspect.AspectFill, BackgroundColor = Color.FromArgb("#E8EDF3") }
            .Bind(Image.SourceProperty, "SelectedPoi.ImageUrl", converter: AppImageSourceConverter.Instance));
        bodyLayout.Add(new Label { FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") }
            .Bind(Label.TextProperty, "SelectedPoi.Title"));
        bodyLayout.Add(new Label { TextColor = Color.FromArgb("#566C82") }.Bind(Label.TextProperty, "SelectedPoi.Summary"));
        bodyLayout.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#EEF5FB"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(10, 8),
            Content = new Label { TextColor = Color.FromArgb("#17324D"), FontSize = 12, FontAttributes = FontAttributes.Bold }
                .Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiNarrationSourceText))
        });
        bodyLayout.Add(new Label { TextColor = Color.FromArgb("#31485F"), MaxLines = 4, LineBreakMode = LineBreakMode.TailTruncation }
            .Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiNarrationText)));
        bodyLayout.Add(new Label { TextColor = Color.FromArgb("#5F7488"), FontSize = 12 }
            .Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiVoiceText)));

        var poiActions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        poiActions.Add(CreateActionButton("Nghe ngay", OnPlaySelectedClicked, "#17324D", "White"));
        var stopButton = CreateActionButton("Dừng", OnStopPlaybackClicked, "#EEF3F8", "#17324D");
        Grid.SetColumn(stopButton, 1);
        poiActions.Add(stopButton);
        var mapButton = CreateActionButton("Mở bản đồ", OnOpenMapClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(mapButton, 2);
        poiActions.Add(mapButton);
        bodyLayout.Add(poiActions);

        var extraActions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        extraActions.Add(CreateActionButton("Xem chi tiết", OnOpenPoiDetailsClicked, "#F2F6FA", "#17324D"));
        var narrationButton = CreateActionButton("Bản thuyết minh", OnOpenNarrationClicked, "#EEF5FB", "#17324D");
        Grid.SetColumn(narrationButton, 1);
        extraActions.Add(narrationButton);
        bodyLayout.Add(extraActions);
        selectedBody.Content = bodyLayout;
        selectedPoiLayout.Add(selectedBody);
        selectedPoiCard.Content = selectedPoiLayout;
        root.Add(selectedPoiCard);

        var nearbyCard = CreateCard();
        var nearbyLayout = new VerticalStackLayout { Spacing = 12 };
        nearbyLayout.Add(new Label { Text = "Các điểm gần bạn", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });

        var nearbyCollection = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 255,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal) { ItemSpacing = 12 }
        };
        nearbyCollection.SetBinding(ItemsView.ItemsSourceProperty, nameof(MainViewModel.NearbyPois));
        nearbyCollection.SetBinding(SelectableItemsView.SelectedItemProperty, nameof(MainViewModel.SelectedPoi), BindingMode.TwoWay);
        nearbyCollection.ItemTemplate = new DataTemplate(() =>
        {
            var card = new Border
            {
                Stroke = Color.FromArgb("#E3EAF2"),
                BackgroundColor = Color.FromArgb("#FBFCFE"),
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = 12,
                WidthRequest = 250
            };

            var layout = new VerticalStackLayout { Spacing = 10 };
            layout.Add(new Image { HeightRequest = 118, Aspect = Aspect.AspectFill, BackgroundColor = Color.FromArgb("#E8EDF3") }
                .Bind(Image.SourceProperty, nameof(AudioTourApp.Models.PoiItem.ImageUrl), converter: AppImageSourceConverter.Instance));
            layout.Add(new Label { FontAttributes = FontAttributes.Bold, FontSize = 17, TextColor = Color.FromArgb("#17324D") }
                .Bind(Label.TextProperty, nameof(AudioTourApp.Models.PoiItem.Title)));
            layout.Add(new Label { TextColor = Color.FromArgb("#5D7287"), MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation }
                .Bind(Label.TextProperty, nameof(AudioTourApp.Models.PoiItem.Summary)));
            var distanceBadge = new Border
            {
                BackgroundColor = Color.FromArgb("#EEF5FB"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(8, 5),
                HorizontalOptions = LayoutOptions.Start,
                Content = new Label { TextColor = Color.FromArgb("#17324D"), FontAttributes = FontAttributes.Bold }
                    .Bind(Label.TextProperty, nameof(AudioTourApp.Models.PoiItem.DistanceDisplay))
            };
            layout.Add(distanceBadge);
            card.Content = layout;
            return card;
        });
        nearbyLayout.Add(nearbyCollection);
        nearbyCard.Content = nearbyLayout;
        root.Add(nearbyCard);

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
            BackgroundColor = ParseColor(backgroundColor),
            TextColor = ParseColor(textColor),
            CornerRadius = 18
        };
        button.Clicked += handler;
        return button;
    }

    private static Button CreateBoundActionButton(string propertyName, EventHandler handler, string backgroundColor, string textColor)
    {
        var button = new Button
        {
            BackgroundColor = ParseColor(backgroundColor),
            TextColor = ParseColor(textColor),
            CornerRadius = 18
        };
        button.SetBinding(Button.TextProperty, propertyName);
        button.Clicked += handler;
        return button;
    }

    private static Color ParseColor(string value)
        => value.Equals("White", StringComparison.OrdinalIgnoreCase)
            ? Colors.White
            : Color.FromArgb(value);

    private Picker CreateLanguagePicker()
    {
        var picker = new Picker
        {
            Title = "Chọn ngôn ngữ nghe",
            ItemDisplayBinding = new Binding("NativeName"),
            BackgroundColor = Color.FromArgb("#F2F6FA"),
            TextColor = Color.FromArgb("#17324D")
        };
        picker.SetBinding(Picker.ItemsSourceProperty, nameof(MainViewModel.Languages));
        picker.SetBinding(Picker.SelectedItemProperty, nameof(MainViewModel.SelectedLanguage), BindingMode.TwoWay);
        picker.SelectedIndexChanged += OnLanguageChanged;
        return picker;
    }
}

internal static class ViewBindingExtensions
{
    public static T Bind<T>(this T target, BindableProperty property, string path, BindingMode mode = BindingMode.Default, string? stringFormat = null, IValueConverter? converter = null)
        where T : BindableObject
    {
        target.SetBinding(property, new Binding(path, mode, converter, stringFormat: stringFormat));
        return target;
    }
}
