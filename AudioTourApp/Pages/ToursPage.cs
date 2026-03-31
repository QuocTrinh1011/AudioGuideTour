using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class ToursPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public ToursPage(MainViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        Title = "Tours";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        Content = BuildContent();
    }

    private async void OnStartTourClicked(object sender, EventArgs e)
    {
        await _viewModel.StartSelectedTourAsync();
    }

    private async void OnNextTourStopClicked(object sender, EventArgs e)
    {
        await _viewModel.PlayNextTourStopAsync();
    }

    private async void OnPlaySelectedClicked(object sender, EventArgs e)
    {
        await _viewModel.PlaySelectedAsync();
    }

    private async void OnStopPlaybackClicked(object sender, EventArgs e)
    {
        await _viewModel.StopPlaybackAsync();
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
                    new(Color.FromArgb("#6E3F1E"), 0f),
                    new(Color.FromArgb("#D18A2B"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "Tour am thuc", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = "Chon tour, bat dau lo trinh va chuyen den diem dung ke tiep khi can.", TextColor = Color.FromArgb("#FFF7E1") }
                }
            }
        });

        var statusCard = CreateCard();
        var statusLayout = new VerticalStackLayout { Spacing = 14 };
        statusLayout.Add(new Label { Text = "Trang thai tour", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        statusLayout.Add(new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.ActiveTourStatus)));
        var statusActions = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        statusActions.Add(CreateActionButton("Bat dau tour", OnStartTourClicked, "#17324D", "White"));
        var nextStopButton = CreateActionButton("Diem ke tiep", OnNextTourStopClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(nextStopButton, 1);
        statusActions.Add(nextStopButton);
        statusLayout.Add(statusActions);
        statusCard.Content = statusLayout;
        root.Add(statusCard);

        var toursCard = CreateCard();
        var toursLayout = new VerticalStackLayout { Spacing = 12 };
        toursLayout.Add(new Label { Text = "Danh sach tour", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        var toursCollection = new CollectionView { SelectionMode = SelectionMode.Single };
        toursCollection.SetBinding(ItemsView.ItemsSourceProperty, nameof(MainViewModel.Tours));
        toursCollection.SetBinding(SelectableItemsView.SelectedItemProperty, nameof(MainViewModel.SelectedTour), BindingMode.TwoWay);
        toursCollection.ItemTemplate = new DataTemplate(() =>
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
                ColumnDefinitions = { new ColumnDefinition(104), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 12
            };
            grid.Add(new Image { HeightRequest = 92, WidthRequest = 104, Aspect = Aspect.AspectFill, BackgroundColor = Color.FromArgb("#E8EDF3") }
                .Bind(Image.SourceProperty, nameof(AudioTourApp.Models.TourItem.CoverImageUrl)));
            var details = new VerticalStackLayout { Spacing = 4 };
            details.Add(new Label { FontAttributes = FontAttributes.Bold, FontSize = 17, TextColor = Color.FromArgb("#17324D") }
                .Bind(Label.TextProperty, nameof(AudioTourApp.Models.TourItem.Name)));
            details.Add(new Label { TextColor = Color.FromArgb("#5D7287"), MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation }
                .Bind(Label.TextProperty, nameof(AudioTourApp.Models.TourItem.Description)));
            details.Add(new Label { TextColor = Color.FromArgb("#17324D") }
                .Bind(Label.TextProperty, nameof(AudioTourApp.Models.TourItem.Stops.Count), stringFormat: "{0} diem dung"));
            details.Add(new Label { TextColor = Color.FromArgb("#73869A") }
                .Bind(Label.TextProperty, nameof(AudioTourApp.Models.TourItem.EstimatedDurationMinutes), stringFormat: "Thoi luong: {0} phut"));
            Grid.SetColumn(details, 1);
            grid.Add(details);
            card.Content = grid;
            return card;
        });
        toursLayout.Add(toursCollection);
        toursCard.Content = toursLayout;
        root.Add(toursCard);

        var stopsCard = CreateCard();
        var stopsLayout = new VerticalStackLayout { Spacing = 12 };
        stopsLayout.Add(new Label { Text = "Cac diem dung trong tour", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        var stopsCollection = new CollectionView { EmptyView = "Chon 1 tour de xem cac diem dung." };
        stopsCollection.SetBinding(ItemsView.ItemsSourceProperty, nameof(MainViewModel.SelectedTourStops));
        stopsCollection.ItemTemplate = new DataTemplate(() =>
        {
            var card = new Border
            {
                Stroke = Color.FromArgb("#E3EAF2"),
                BackgroundColor = Color.FromArgb("#FBFCFE"),
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Padding = 12,
                Margin = new Thickness(0, 0, 0, 10)
            };
            var grid = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 12
            };
            grid.Add(new Border
            {
                BackgroundColor = Color.FromArgb("#17324D"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(10, 6),
                VerticalOptions = LayoutOptions.Start,
                Content = new Label { TextColor = Colors.White, FontAttributes = FontAttributes.Bold }
                    .Bind(Label.TextProperty, nameof(AudioTourApp.Models.TourStopItem.SortOrder))
            });
            var details = new VerticalStackLayout { Spacing = 3 };
            details.Add(new Label { FontAttributes = FontAttributes.Bold, FontSize = 16, TextColor = Color.FromArgb("#17324D") }
                .Bind(Label.TextProperty, "Poi.Title"));
            details.Add(new Label { TextColor = Color.FromArgb("#667C92"), MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation }
                .Bind(Label.TextProperty, "Poi.Summary"));
            details.Add(new Label { TextColor = Color.FromArgb("#8A9BAA"), FontSize = 12 }
                .Bind(Label.TextProperty, nameof(AudioTourApp.Models.TourStopItem.Note)));
            Grid.SetColumn(details, 1);
            grid.Add(details);
            card.Content = grid;
            return card;
        });
        stopsLayout.Add(stopsCollection);
        stopsCard.Content = stopsLayout;
        root.Add(stopsCard);

        var poiCard = CreateCard();
        var poiLayout = new VerticalStackLayout { Spacing = 12 };
        poiLayout.Add(new Label { Text = "POI dang duoc gan voi tour", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        poiLayout.Add(new Label { FontAttributes = FontAttributes.Bold, FontSize = 18, TextColor = Color.FromArgb("#17324D") }.Bind(Label.TextProperty, "SelectedPoi.Title"));
        poiLayout.Add(new Label { TextColor = Color.FromArgb("#5D7287") }.Bind(Label.TextProperty, "SelectedPoi.Summary"));
        var poiActions = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        poiActions.Add(CreateActionButton("Nghe diem nay", OnPlaySelectedClicked, "#17324D", "White"));
        var stopButton = CreateActionButton("Dung", OnStopPlaybackClicked, "#EEF3F8", "#17324D");
        Grid.SetColumn(stopButton, 1);
        poiActions.Add(stopButton);
        poiLayout.Add(poiActions);
        poiLayout.Add(CreateActionButton("Xem chi tiet POI", OnOpenPoiDetailsClicked, "#E4B43C", "#17324D"));
        poiCard.Content = poiLayout;
        root.Add(poiCard);

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
