using AudioTourApp.Models;
using AudioTourApp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class QrDisplayPage : ContentPage
{
    private readonly ApiClient _apiClient;
    private readonly string _qrImageUrl;
    private readonly Image _qrImage;
    private readonly ActivityIndicator _loadingIndicator;
    private readonly Label _imageStatusLabel;
    private bool _hasLoadedImage;

    public QrDisplayPage(QrDirectoryItem item, string qrImageUrl, string qrPublicUrl, ApiClient apiClient)
    {
        _apiClient = apiClient;
        _qrImageUrl = qrImageUrl;

        Title = "Mở QR";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        BindingContext = item;

        _loadingIndicator = new ActivityIndicator
        {
            IsRunning = true,
            IsVisible = true,
            Color = Color.FromArgb("#17324D"),
            HorizontalOptions = LayoutOptions.Center
        };

        _qrImage = new Image
        {
            HeightRequest = 320,
            WidthRequest = 320,
            Aspect = Aspect.AspectFit,
            BackgroundColor = Colors.White,
            IsVisible = false
        };

        _imageStatusLabel = new Label
        {
            Text = "Đang tải mã QR...",
            TextColor = Color.FromArgb("#667C92"),
            HorizontalTextAlignment = TextAlignment.Center
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(18, 18, 18, 28),
                Spacing = 18,
                Children =
                {
                    new Border
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
                                    Text = "Mã QR trình chiếu",
                                    FontSize = 26,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Colors.White
                                },
                                new Label
                                {
                                    Text = "Giữ màn hình này để điện thoại khác quét và mở nội dung đầy đủ.",
                                    TextColor = Color.FromArgb("#E8F0F7")
                                }
                            }
                        }
                    },
                    CreateCard(new VerticalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            new Label
                            {
                                FontAttributes = FontAttributes.Bold,
                                FontSize = 22,
                                TextColor = Color.FromArgb("#17324D")
                            }.Bind(Label.TextProperty, nameof(QrDirectoryItem.PoiTitle)),
                            new Label
                            {
                                TextColor = Color.FromArgb("#667C92")
                            }.Bind(Label.TextProperty, nameof(QrDirectoryItem.Code), stringFormat: "Mã QR: {0}"),
                            new Border
                            {
                                Stroke = Color.FromArgb("#E3EAF2"),
                                BackgroundColor = Colors.White,
                                Padding = 18,
                                HorizontalOptions = LayoutOptions.Center,
                                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                                Content = new VerticalStackLayout
                                {
                                    Spacing = 10,
                                    HorizontalOptions = LayoutOptions.Center,
                                    Children =
                                    {
                                        _loadingIndicator,
                                        _qrImage,
                                        _imageStatusLabel
                                    }
                                }
                            },
                            new Label
                            {
                                Text = "Nếu điện thoại quét không mở được, hãy kiểm tra lại cùng Wi‑Fi và cổng admin LAN.",
                                TextColor = Color.FromArgb("#667C92"),
                                FontSize = 12
                            },
                            new Label
                            {
                                Text = "Nội dung đầy đủ sẽ mở trên điện thoại quét mã, không cần GPS.",
                                TextColor = Color.FromArgb("#17324D"),
                                FontSize = 12
                            }
                        }
                    })
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasLoadedImage)
        {
            return;
        }

        _hasLoadedImage = true;
        await LoadQrImageAsync();
    }

    private async Task LoadQrImageAsync()
    {
        try
        {
            var downloaded = await _apiClient.DownloadFileToCacheAsync(_qrImageUrl, "qr");
            _qrImage.Source = ImageSource.FromFile(downloaded.LocalPath);
            _qrImage.IsVisible = true;
            _imageStatusLabel.Text = "Đưa điện thoại khác lại gần để quét mã.";
        }
        catch (Exception ex)
        {
            _imageStatusLabel.Text = $"Không tải được mã QR: {ex.Message}";
        }
        finally
        {
            _loadingIndicator.IsRunning = false;
            _loadingIndicator.IsVisible = false;
        }
    }

    private static Border CreateCard(View content) => new()
    {
        Stroke = Color.FromArgb("#D9E3EE"),
        BackgroundColor = Colors.White,
        Padding = 16,
        StrokeShape = new RoundRectangle { CornerRadius = 24 },
        Content = content
    };
}
