using AudioTourApp.Models;
using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class QrPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public QrPage(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        BindingContext = _viewModel = viewModel;
        Title = "QR";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        Content = BuildContent();
    }

    private async void OnOpenQrCardClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not QrDirectoryItem item)
        {
            return;
        }

        await Navigation.PushAsync(new QrDisplayPage(item, BuildQrImageUrl(item.Code), BuildQrPublicUrl(item.Code)));
    }

    private async void OnOpenQrPreviewClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not QrDirectoryItem item)
        {
            return;
        }

        await Navigation.PushAsync(new QrContentPreviewPage(item));
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
                    new Label
                    {
                        Text = "Mở QR để điện thoại khác quét",
                        FontSize = 26,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White
                    },
                    new Label
                    {
                        Text = "App này dùng để mở mã QR và xem nhanh nội dung. Khi cần mở đầy đủ, hãy dùng điện thoại khác quét mã đang hiển thị trên màn hình.",
                        TextColor = Color.FromArgb("#E8F0F7")
                    }
                }
            }
        });

        var directoryCard = CreateCard();
        var directoryLayout = new VerticalStackLayout { Spacing = 12 };
        directoryLayout.Add(new Label
        {
            Text = "Điểm mở bằng QR",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17324D")
        });
        directoryLayout.Add(new Label
        {
            TextColor = Color.FromArgb("#667C92"),
            FontSize = 12
        }.Bind(Label.TextProperty, nameof(MainViewModel.QrDirectorySummary)));

        var directoryCollection = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            EmptyView = "Chưa có điểm QR nào được tải về."
        };
        directoryCollection.SetBinding(ItemsView.ItemsSourceProperty, nameof(MainViewModel.QrDirectoryItems));
        directoryCollection.ItemTemplate = new DataTemplate(() =>
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

            grid.Add(new Image
            {
                HeightRequest = 88,
                WidthRequest = 92,
                Aspect = Aspect.AspectFill,
                BackgroundColor = Color.FromArgb("#E8EDF3")
            }.Bind(Image.SourceProperty, nameof(QrDirectoryItem.ImageUrl), converter: AppImageSourceConverter.Instance));

            var details = new VerticalStackLayout { Spacing = 4 };
            details.Add(new Label
            {
                FontAttributes = FontAttributes.Bold,
                FontSize = 17,
                TextColor = Color.FromArgb("#17324D")
            }.Bind(Label.TextProperty, nameof(QrDirectoryItem.PoiTitle)));
            details.Add(new Label
            {
                TextColor = Color.FromArgb("#5D7287"),
                MaxLines = 2,
                LineBreakMode = LineBreakMode.TailTruncation
            }.Bind(Label.TextProperty, nameof(QrDirectoryItem.PoiSummary)));
            details.Add(new Label
            {
                TextColor = Color.FromArgb("#73869A"),
                FontSize = 12
            }.Bind(Label.TextProperty, nameof(QrDirectoryItem.Code), stringFormat: "Mã QR: {0}"));

            var actions = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 10,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var qrButton = CreateActionButton("Mở QR", OnOpenQrCardClicked, "#17324D", "White");
            qrButton.SetBinding(Button.CommandParameterProperty, new Binding("."));
            actions.Add(qrButton);

            var previewButton = CreateActionButton("Mở nội dung", OnOpenQrPreviewClicked, "#E4B43C", "#17324D");
            previewButton.SetBinding(Button.CommandParameterProperty, new Binding("."));
            Grid.SetColumn(previewButton, 1);
            actions.Add(previewButton);

            details.Add(actions);
            Grid.SetColumn(details, 1);
            grid.Add(details);

            card.Content = grid;
            return card;
        });
        directoryLayout.Add(directoryCollection);
        directoryCard.Content = directoryLayout;
        root.Add(directoryCard);

        var noteCard = CreateCard();
        noteCard.Content = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = "Cách dùng",
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#17324D")
                },
                new Label
                {
                    Text = "1. Bấm Mở QR để app hiển thị mã vuông đen trắng toàn màn hình.",
                    TextColor = Color.FromArgb("#445D75")
                },
                new Label
                {
                    Text = "2. Dùng điện thoại khác quét mã này để mở trang nội dung đầy đủ.",
                    TextColor = Color.FromArgb("#445D75")
                },
                new Label
                {
                    Text = "3. Nếu chỉ muốn xem nhanh trong app, bấm Mở nội dung.",
                    TextColor = Color.FromArgb("#445D75")
                }
            }
        };
        root.Add(noteCard);

        var historyCard = CreateCard();
        var historyLayout = new VerticalStackLayout { Spacing = 12 };
        historyLayout.Add(new Label
        {
            Text = "QR đã mở gần đây",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17324D")
        });
        historyLayout.Add(new Label
        {
            TextColor = Color.FromArgb("#667C92"),
            FontSize = 12
        }.Bind(Label.TextProperty, nameof(MainViewModel.RecentQrSummary)));

        var historyCollection = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            EmptyView = "Chưa có QR nào được mở."
        };
        historyCollection.SetBinding(ItemsView.ItemsSourceProperty, nameof(MainViewModel.RecentQrLookups));
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

            grid.Add(new Image
            {
                HeightRequest = 88,
                WidthRequest = 92,
                Aspect = Aspect.AspectFill,
                BackgroundColor = Color.FromArgb("#E8EDF3")
            }.Bind(Image.SourceProperty, nameof(QrLookupHistoryItem.ImageUrl), converter: AppImageSourceConverter.Instance));

            var details = new VerticalStackLayout { Spacing = 4 };
            details.Add(new Label
            {
                FontAttributes = FontAttributes.Bold,
                FontSize = 17,
                TextColor = Color.FromArgb("#17324D")
            }.Bind(Label.TextProperty, nameof(QrLookupHistoryItem.PoiTitle)));
            details.Add(new Label
            {
                TextColor = Color.FromArgb("#5D7287"),
                MaxLines = 2,
                LineBreakMode = LineBreakMode.TailTruncation
            }.Bind(Label.TextProperty, nameof(QrLookupHistoryItem.PoiSummary)));
            details.Add(new Label
            {
                TextColor = Color.FromArgb("#17324D")
            }.Bind(Label.TextProperty, nameof(QrLookupHistoryItem.Code), stringFormat: "Mã: {0}"));
            details.Add(new Label
            {
                TextColor = Color.FromArgb("#73869A"),
                FontSize = 12
            }.Bind(Label.TextProperty, nameof(QrLookupHistoryItem.OpenedAt), stringFormat: "Mở lúc: {0:HH:mm dd/MM}"));

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

    private string BuildQrImageUrl(string code)
    {
        var baseUrl = BuildAdminBaseUrl();
        return $"{baseUrl}/QRCode/RenderPngByCode?code={Uri.EscapeDataString(code)}";
    }

    private string BuildQrPublicUrl(string code)
    {
        var baseUrl = BuildAdminBaseUrl();
        return $"{baseUrl}/QRCode/Open/{Uri.EscapeDataString(code)}";
    }

    private string BuildAdminBaseUrl()
    {
        if (!Uri.TryCreate(_viewModel.ApiBaseUrl, UriKind.Absolute, out var apiUri))
        {
            return "http://10.0.2.2:5038";
        }

        var host = apiUri.Host;
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            host = "10.0.2.2";
        }

        return $"http://{host}:5038";
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
            BackgroundColor = Color.FromArgb(backgroundColor),
            TextColor = textColor.Equals("White", StringComparison.OrdinalIgnoreCase) ? Colors.White : Color.FromArgb(textColor),
            CornerRadius = 18
        };
        button.Clicked += handler;
        return button;
    }
}
