using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class NarrationPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public NarrationPage(MainViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        Title = "Bản thuyết minh";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        Content = BuildContent();
    }

    private async void OnPlaySelectedClicked(object? sender, EventArgs e)
    {
        await _viewModel.PlaySelectedAsync();
    }

    private async void OnStopPlaybackClicked(object? sender, EventArgs e)
    {
        await _viewModel.StopPlaybackAsync();
    }

    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        await _viewModel.ChangeLanguageAsync();
    }

    private void OnPreviousPoiClicked(object? sender, EventArgs e)
    {
        _viewModel.SelectPreviousPoi();
    }

    private void OnNearestPoiClicked(object? sender, EventArgs e)
    {
        _viewModel.SelectNearestPoi();
    }

    private void OnNextPoiClicked(object? sender, EventArgs e)
    {
        _viewModel.SelectNextPoi();
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
                    new(Color.FromArgb("#245A76"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "Bản thuyết minh", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { TextColor = Color.FromArgb("#E4EEF7") }.Bind(Label.TextProperty, "SelectedPoi.Title")
                }
            }
        });

        var metaCard = CreateCard();
        metaCard.Content = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Ngôn ngữ đang xem", FontSize = 12, TextColor = Color.FromArgb("#8AA0B6") },
                CreateLanguagePicker(),
                new Label { FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") }.Bind(Label.TextProperty, "SelectedPoi.Title"),
                new Label { TextColor = Color.FromArgb("#8AA0B6"), FontSize = 12 }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiPositionText)),
                new Label { TextColor = Color.FromArgb("#5D7287") }.Bind(Label.TextProperty, "SelectedPoi.Summary"),
                new Label { TextColor = Color.FromArgb("#17324D"), FontAttributes = FontAttributes.Bold }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiNarrationSourceText)),
                new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiVoiceText)),
                new Label { TextColor = Color.FromArgb("#8AA0B6"), FontSize = 12 }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiMetaText))
            }
        };
        root.Add(metaCard);

        var scriptCard = CreateCard();
        scriptCard.Content = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Noi dung day du", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") },
                new Label
                {
                    TextColor = Color.FromArgb("#31485F"),
                    FontSize = 16,
                    LineBreakMode = LineBreakMode.WordWrap
                }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiNarrationText)),
                new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiAudioText))
            }
        };
        root.Add(scriptCard);

        var queueCard = CreateCard();
        queueCard.Content = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Trang thai phat", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") },
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
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        actions.Add(CreateActionButton("Nghe ngay", OnPlaySelectedClicked, "#17324D", "White"));
        var stopButton = CreateActionButton("Dung", OnStopPlaybackClicked, "#EEF3F8", "#17324D");
        Grid.SetColumn(stopButton, 1);
        actions.Add(stopButton);
        root.Add(actions);

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
        root.Add(navigationActions);

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
            Title = "Đổi ngôn ngữ bản thuyết minh",
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
