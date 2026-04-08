using AudioTourApp.Models;
using AudioTourApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class QrPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public QrPage(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        BindingContext = _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        Title = "QR";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        Content = BuildContent();
    }

    private async void OnLookupQrClicked(object? sender, EventArgs e)
    {
        await _viewModel.LookupQrAsync();
    }

    private async void OnPasteQrClicked(object? sender, EventArgs e)
    {
        await _viewModel.PasteQrFromClipboardAsync();
    }

    private async void OnRequestCameraPermissionClicked(object? sender, EventArgs e)
    {
        await _viewModel.RequestCameraPermissionAsync();
    }

    private async void OnOpenScannerClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanUseCamera)
        {
            await _viewModel.RequestCameraPermissionAsync();
            await _viewModel.RefreshPermissionStatusAsync();
        }

        if (!_viewModel.CanUseCamera)
        {
            await DisplayAlert("Camera", "App chua co quyen camera de quet QR.", "OK");
            return;
        }

        var scannerPage = _serviceProvider.GetRequiredService<QrScannerPage>();
        await Navigation.PushModalAsync(new NavigationPage(scannerPage));
    }

    private async void OnRefreshPermissionsClicked(object? sender, EventArgs e)
    {
        await _viewModel.RefreshPermissionStatusAsync();
    }

    private async void OnQuickQrClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string code)
        {
            await _viewModel.LookupQrAsync(code);
        }
    }

    private void OnRecentQrSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not CollectionView collection || e.CurrentSelection.FirstOrDefault() is not QrLookupHistoryItem item)
        {
            return;
        }

        _viewModel.OpenRecentQr(item);
        collection.SelectedItem = null;
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
                    new(Color.FromArgb("#2D5E77"), 0.7f),
                    new(Color.FromArgb("#E4B43C"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "QR kich hoat nhanh", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = "Nhap, dan, hoac dung ma nhanh de mo ngay noi dung xe buyt ma khong can GPS.", TextColor = Color.FromArgb("#E8F0F7") }
                }
            }
        });

        var qrCard = CreateCard();
        var qrLayout = new VerticalStackLayout { Spacing = 12 };
        qrLayout.Add(new Label { Text = "Mo ma QR", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        qrLayout.Add(new Entry
        {
            Placeholder = "BUS-KH-001",
            BackgroundColor = Color.FromArgb("#F8FAFD"),
            TextColor = Color.FromArgb("#17324D")
        }.Bind(Entry.TextProperty, nameof(MainViewModel.QrCodeInput), BindingMode.TwoWay));

        var quickCodes = new HorizontalStackLayout { Spacing = 8 };
        quickCodes.Add(CreateActionButton("Khanh Hoi", OnQuickQrClicked, "#EEF3F8", "#17324D", "BUS-KH-001"));
        quickCodes.Add(CreateActionButton("Vinh Hoi", OnQuickQrClicked, "#EEF3F8", "#17324D", "BUS-VH-002"));
        quickCodes.Add(CreateActionButton("Xuan Chieu", OnQuickQrClicked, "#EEF3F8", "#17324D", "BUS-XC-003"));
        qrLayout.Add(quickCodes);

        var qrActions = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        qrActions.Add(CreateActionButton("Mo QR", OnLookupQrClicked, "#17324D", "White"));
        var pasteButton = CreateActionButton("Dan tu clipboard", OnPasteQrClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(pasteButton, 1);
        qrActions.Add(pasteButton);
        qrLayout.Add(qrActions);
        qrLayout.Add(new Label { TextColor = Color.FromArgb("#667C92"), FontSize = 12 }.Bind(Label.TextProperty, nameof(MainViewModel.Status)));
        qrCard.Content = qrLayout;
        root.Add(qrCard);

        var cameraCard = CreateCard();
        var cameraLayout = new VerticalStackLayout { Spacing = 12 };
        cameraLayout.Add(new Label { Text = "Quyen camera", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        cameraLayout.Add(new Label { TextColor = Color.FromArgb("#445D75") }.Bind(Label.TextProperty, nameof(MainViewModel.CameraPermissionText)));
        cameraLayout.Add(new Label
        {
            Text = "Ban co the quet camera that, hoac dung luong nhap/dan ma QR neu dang test tren emulator.",
            TextColor = Color.FromArgb("#667C92"),
            FontSize = 12
        });

        var cameraActions = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        cameraActions.Add(CreateActionButton("Quet bang camera", OnOpenScannerClicked, "#17324D", "White"));
        var requestButton = CreateActionButton("Xin quyen", OnRequestCameraPermissionClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(requestButton, 1);
        cameraActions.Add(requestButton);
        var refreshButton = CreateActionButton("Kiem tra lai", OnRefreshPermissionsClicked, "#EEF3F8", "#17324D");
        Grid.SetColumn(refreshButton, 2);
        cameraActions.Add(refreshButton);
        cameraLayout.Add(cameraActions);
        cameraCard.Content = cameraLayout;
        root.Add(cameraCard);

        var historyCard = CreateCard();
        var historyLayout = new VerticalStackLayout { Spacing = 12 };
        historyLayout.Add(new Label { Text = "QR da mo gan day", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        historyLayout.Add(new Label { TextColor = Color.FromArgb("#667C92"), FontSize = 12 }.Bind(Label.TextProperty, nameof(MainViewModel.RecentQrSummary)));
        var historyCollection = new CollectionView { SelectionMode = SelectionMode.Single, EmptyView = "Chua co QR nao duoc mo." };
        historyCollection.SetBinding(ItemsView.ItemsSourceProperty, nameof(MainViewModel.RecentQrLookups));
        historyCollection.SelectionChanged += OnRecentQrSelected;
        historyCollection.ItemTemplate = new DataTemplate(() =>
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
                .Bind(Image.SourceProperty, nameof(QrLookupHistoryItem.ImageUrl), converter: AppImageSourceConverter.Instance));
            var details = new VerticalStackLayout { Spacing = 4 };
            details.Add(new Label { FontAttributes = FontAttributes.Bold, FontSize = 17, TextColor = Color.FromArgb("#17324D") }
                .Bind(Label.TextProperty, nameof(QrLookupHistoryItem.PoiTitle)));
            details.Add(new Label { TextColor = Color.FromArgb("#5D7287"), MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation }
                .Bind(Label.TextProperty, nameof(QrLookupHistoryItem.PoiSummary)));
            details.Add(new Label { TextColor = Color.FromArgb("#17324D") }
                .Bind(Label.TextProperty, nameof(QrLookupHistoryItem.Code), stringFormat: "Ma: {0}"));
            details.Add(new Label { TextColor = Color.FromArgb("#73869A"), FontSize = 12 }
                .Bind(Label.TextProperty, nameof(QrLookupHistoryItem.OpenedAt), stringFormat: "Mo luc: {0:HH:mm dd/MM}"));
            Grid.SetColumn(details, 1);
            grid.Add(details);
            card.Content = grid;
            return card;
        });
        historyLayout.Add(historyCollection);
        historyCard.Content = historyLayout;
        root.Add(historyCard);

        return new ScrollView { Content = root };
    }

    private static Border CreateCard() => new()
    {
        Stroke = Color.FromArgb("#D9E3EE"),
        BackgroundColor = Colors.White,
        Padding = 16,
        StrokeShape = new RoundRectangle { CornerRadius = 24 }
    };

    private static Button CreateActionButton(string text, EventHandler handler, string backgroundColor, string textColor, object? commandParameter = null)
    {
        var button = new Button
        {
            Text = text,
            BackgroundColor = textColor.Equals("White", StringComparison.OrdinalIgnoreCase) ? Color.FromArgb(backgroundColor) : Color.FromArgb(backgroundColor),
            TextColor = textColor.Equals("White", StringComparison.OrdinalIgnoreCase) ? Colors.White : Color.FromArgb(textColor),
            CornerRadius = 18,
            CommandParameter = commandParameter
        };
        button.Clicked += handler;
        return button;
    }
}
