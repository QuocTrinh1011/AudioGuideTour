using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class SettingsPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public SettingsPage(MainViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        Title = "Settings";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        Content = BuildContent();
    }

    private async void OnBootstrapClicked(object sender, EventArgs e)
    {
        await _viewModel.BootstrapAsync();
    }

    private async void OnSyncVisitorClicked(object sender, EventArgs e)
    {
        await _viewModel.SyncVisitorSettingsAsync();
    }

    private async void OnLanguageChanged(object sender, EventArgs e)
    {
        await _viewModel.ChangeLanguageAsync();
    }

    private void OnResetApiClicked(object sender, EventArgs e)
    {
        _viewModel.ResetApiBaseUrl();
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
                    new(Color.FromArgb("#253748"), 0f),
                    new(Color.FromArgb("#6C7F90"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "Cai dat va dong bo", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = "Quan ly ket noi API, ngon ngu va visitor duoc admin theo doi.", TextColor = Color.FromArgb("#E6EDF3") }
                }
            }
        });

        var systemCard = CreateCard();
        var systemLayout = new VerticalStackLayout { Spacing = 14 };
        systemLayout.Add(new Label { Text = "Ket noi he thong", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        systemLayout.Add(new Entry { Placeholder = "http://10.0.2.2:5297", BackgroundColor = Color.FromArgb("#F9FBFD"), TextColor = Color.FromArgb("#17324D") }
            .Bind(Entry.TextProperty, nameof(MainViewModel.ApiBaseUrl), BindingMode.TwoWay));
        var picker = new Picker
        {
            Title = "Chon ngon ngu",
            ItemDisplayBinding = new Binding(nameof(AudioTourApp.Models.LanguageItem.NativeName)),
            BackgroundColor = Color.FromArgb("#F9FBFD"),
            TextColor = Color.FromArgb("#17324D")
        };
        picker.SetBinding(Picker.ItemsSourceProperty, nameof(MainViewModel.Languages));
        picker.SetBinding(Picker.SelectedItemProperty, nameof(MainViewModel.SelectedLanguage), BindingMode.TwoWay);
        picker.SelectedIndexChanged += OnLanguageChanged;
        systemLayout.Add(picker);
        var syncRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        syncRow.Add(CreateActionButton("Tai du lieu", OnBootstrapClicked, "#17324D", "White"));
        var syncButton = CreateActionButton("Dong bo visitor", OnSyncVisitorClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(syncButton, 1);
        syncRow.Add(syncButton);
        systemLayout.Add(syncRow);
        systemLayout.Add(CreateActionButton("Reset API ve mac dinh emulator", OnResetApiClicked, "#EEF3F8", "#17324D"));
        systemCard.Content = systemLayout;
        root.Add(systemCard);

        var visitorCard = CreateCard();
        var visitorLayout = new VerticalStackLayout { Spacing = 14 };
        visitorLayout.Add(new Label { Text = "Visitor mobile", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        visitorLayout.Add(new Entry { Placeholder = "Ten visitor hien thi trong admin", BackgroundColor = Color.FromArgb("#F9FBFD"), TextColor = Color.FromArgb("#17324D") }
            .Bind(Entry.TextProperty, nameof(MainViewModel.VisitorDisplayName), BindingMode.TwoWay));

        var optionRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 12
        };
        optionRow.Add(CreateSwitchCard("Auto-play", "Cho phep tu dong doc", nameof(MainViewModel.AllowAutoPlay)));
        var bgCard = CreateSwitchCard("Background", "Cho phep tracking nen", nameof(MainViewModel.AllowBackgroundTracking));
        Grid.SetColumn(bgCard, 1);
        optionRow.Add(bgCard);
        visitorLayout.Add(optionRow);
        visitorLayout.Add(new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.Status)));
        visitorCard.Content = visitorLayout;
        root.Add(visitorCard);

        return new ScrollView { Content = root };
    }

    private static Border CreateCard() => new()
    {
        Stroke = Color.FromArgb("#D9E3EE"),
        BackgroundColor = Colors.White,
        Padding = 16,
        StrokeShape = new RoundRectangle { CornerRadius = 24 }
    };

    private static Border CreateSwitchCard(string title, string subtitle, string bindingPath)
    {
        var card = new Border
        {
            BackgroundColor = Color.FromArgb("#F5F8FB"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Padding = 14
        };
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        grid.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") },
                new Label { Text = subtitle, FontSize = 12, TextColor = Color.FromArgb("#6F8397") }
            }
        });
        var toggle = new Switch
        {
            HorizontalOptions = LayoutOptions.End,
            ThumbColor = Color.FromArgb("#17324D"),
            OnColor = Color.FromArgb("#E4B43C")
        };
        toggle.SetBinding(Switch.IsToggledProperty, bindingPath, BindingMode.TwoWay);
        Grid.SetColumn(toggle, 1);
        grid.Add(toggle);
        card.Content = grid;
        return card;
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
