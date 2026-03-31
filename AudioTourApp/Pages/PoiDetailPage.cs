using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class PoiDetailPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public PoiDetailPage(MainViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        Title = "Chi tiet POI";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        Content = BuildContent();
    }

    private async void OnPlaySelectedClicked(object sender, EventArgs e)
    {
        await _viewModel.PlaySelectedAsync();
    }

    private async void OnStopPlaybackClicked(object sender, EventArgs e)
    {
        await _viewModel.StopPlaybackAsync();
    }

    private async void OnOpenMapClicked(object sender, EventArgs e)
    {
        await _viewModel.OpenSelectedMapAsync();
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
                    new(Color.FromArgb("#2C6278"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }.Bind(Label.TextProperty, "SelectedPoi.Title"),
                    new Label { TextColor = Color.FromArgb("#E4EEF7") }.Bind(Label.TextProperty, "SelectedPoi.Summary")
                }
            }
        });

        root.Add(new Image
        {
            HeightRequest = 240,
            Aspect = Aspect.AspectFill,
            BackgroundColor = Color.FromArgb("#E8EDF3")
        }.Bind(Image.SourceProperty, "SelectedPoi.ImageUrl"));

        var infoCard = CreateCard();
        infoCard.Content = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Thong tin diem thuyet minh", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") },
                new Label { TextColor = Color.FromArgb("#445D75") }.Bind(Label.TextProperty, "SelectedPoi.Description"),
                new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, "SelectedPoi.Address", stringFormat: "Dia chi: {0}"),
                new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, "SelectedPoi.Language", stringFormat: "Ngon ngu: {0}"),
                new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, "SelectedPoi.Category", stringFormat: "Danh muc: {0}"),
                new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, "SelectedPoi.TriggerMode", stringFormat: "Kich hoat: {0}"),
                new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, "SelectedPoi.Radius", stringFormat: "Ban kinh geofence: {0}m"),
                new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, "SelectedPoi.ApproachRadiusMeters", stringFormat: "Ban kinh nearby: {0}m"),
                new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, "SelectedPoi.Priority", stringFormat: "Uu tien: {0}")
            }
        };
        root.Add(infoCard);

        var narrationCard = CreateCard();
        narrationCard.Content = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Ban thuyet minh", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") },
                new Border
                {
                    BackgroundColor = Color.FromArgb("#EEF5FB"),
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 14 },
                    Padding = new Thickness(10, 8),
                    Content = new Label { TextColor = Color.FromArgb("#17324D"), FontAttributes = FontAttributes.Bold }
                        .Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiNarrationSourceText))
                },
                new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiVoiceText)),
                new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiMetaText)),
                new Label { TextColor = Color.FromArgb("#8AA0B6"), FontSize = 12 }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiCoordinateText)),
                new Label
                {
                    TextColor = Color.FromArgb("#31485F"),
                    FontSize = 15,
                    LineBreakMode = LineBreakMode.WordWrap
                }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiNarrationText)),
                new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiAudioText))
            }
        };
        root.Add(narrationCard);

        var queueCard = CreateCard();
        queueCard.Content = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Hang cho audio", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") },
                new Label { TextColor = Color.FromArgb("#35526B") }.Bind(Label.TextProperty, nameof(MainViewModel.PlaybackStatusText)),
                new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.QueueSummaryText))
            }
        };
        root.Add(queueCard);

        var actions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        actions.Add(CreateActionButton("Nghe ngay", OnPlaySelectedClicked, "#17324D", "White"));
        var stopButton = CreateActionButton("Dung", OnStopPlaybackClicked, "#EEF3F8", "#17324D");
        Grid.SetColumn(stopButton, 1);
        actions.Add(stopButton);
        var mapButton = CreateActionButton("Mo ban do", OnOpenMapClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(mapButton, 2);
        actions.Add(mapButton);
        root.Add(actions);

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
}
