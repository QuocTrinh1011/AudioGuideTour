using AudioTourApp.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class QrContentPreviewPage : ContentPage
{
    public QrContentPreviewPage(QrDirectoryItem item)
    {
        Title = "Mở nội dung";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        BindingContext = item;

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
                                new(Color.FromArgb("#2D5E77"), 1f)
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
                                    Text = "Nội dung xem nhanh",
                                    FontSize = 26,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Colors.White
                                },
                                new Label
                                {
                                    Text = "Trong app chỉ xem nhanh một phần nội dung. Muốn mở đầy đủ, hãy quay lại và bấm Mở QR để điện thoại khác quét.",
                                    TextColor = Color.FromArgb("#E8F0F7")
                                }
                            }
                        }
                    },
                    new Image
                    {
                        HeightRequest = 230,
                        Aspect = Aspect.AspectFill,
                        BackgroundColor = Color.FromArgb("#E8EDF3")
                    }.Bind(Image.SourceProperty, nameof(QrDirectoryItem.ImageUrl), converter: AppImageSourceConverter.Instance),
                    CreateCard(new VerticalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new Label
                            {
                                FontSize = 24,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Color.FromArgb("#17324D")
                            }.Bind(Label.TextProperty, nameof(QrDirectoryItem.PoiTitle)),
                            new Label
                            {
                                TextColor = Color.FromArgb("#667C92"),
                                FontSize = 12
                            }.Bind(Label.TextProperty, nameof(QrDirectoryItem.Code), stringFormat: "Mã QR: {0}"),
                            new Label
                            {
                                TextColor = Color.FromArgb("#445D75"),
                                FontAttributes = FontAttributes.Bold,
                                Text = "Tóm tắt"
                            },
                            new Label
                            {
                                TextColor = Color.FromArgb("#31485F"),
                                LineBreakMode = LineBreakMode.WordWrap
                            }.Bind(Label.TextProperty, nameof(QrDirectoryItem.PoiSummary)),
                            new Label
                            {
                                Text = BuildPreviewDescription(item),
                                TextColor = Color.FromArgb("#5F7488"),
                                LineBreakMode = LineBreakMode.WordWrap
                            }
                        }
                    })
                }
            }
        };
    }

    private static string BuildPreviewDescription(QrDirectoryItem item)
    {
        var description = item.Poi?.Description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description))
        {
            return "Điểm này có nội dung đầy đủ khi mở bằng trang QR công khai hoặc trong app theo luồng nghe chính.";
        }

        return description.Length <= 260
            ? description
            : $"{description[..260].Trim()}...";
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
