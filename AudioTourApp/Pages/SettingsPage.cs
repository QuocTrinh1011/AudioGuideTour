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
        Title = "Cài đặt";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        Content = BuildContent();
    }

    private async void OnBootstrapClicked(object? sender, EventArgs e)
    {
        await _viewModel.BootstrapAsync();
    }

    private async void OnSyncVisitorClicked(object? sender, EventArgs e)
    {
        await _viewModel.SyncVisitorSettingsAsync();
    }

    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        await _viewModel.ChangeLanguageAsync();
    }

    private async void OnRefreshPermissionsClicked(object? sender, EventArgs e)
    {
        await _viewModel.RefreshPermissionStatusAsync();
    }

    private async void OnRequestTrackingPermissionsClicked(object? sender, EventArgs e)
    {
        await _viewModel.RequestTrackingPermissionsAsync();
    }

    private async void OnRequestCameraPermissionClicked(object? sender, EventArgs e)
    {
        await _viewModel.RequestCameraPermissionAsync();
    }

    private async void OnOpenSystemSettingsClicked(object? sender, EventArgs e)
    {
        await _viewModel.OpenSystemSettingsAsync();
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
                    new Label { Text = "Cài đặt trải nghiệm", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = "Chọn ngôn ngữ, bật các quyền cần thiết và làm mới nội dung khi cần.", TextColor = Color.FromArgb("#E6EDF3") }
                }
            }
        });

        var languageCard = CreateCard();
        var languageLayout = new VerticalStackLayout { Spacing = 14 };
        languageLayout.Add(new Label { Text = "Ngôn ngữ và nội dung", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        var picker = new Picker
        {
            Title = "Chọn ngôn ngữ",
            ItemDisplayBinding = new Binding(nameof(AudioTourApp.Models.LanguageItem.NativeName)),
            BackgroundColor = Color.FromArgb("#F9FBFD"),
            TextColor = Color.FromArgb("#17324D")
        };
        picker.SetBinding(Picker.ItemsSourceProperty, nameof(MainViewModel.Languages));
        picker.SetBinding(Picker.SelectedItemProperty, nameof(MainViewModel.SelectedLanguage), BindingMode.TwoWay);
        picker.SelectedIndexChanged += OnLanguageChanged;
        languageLayout.Add(picker);

        var syncRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        syncRow.Add(CreateActionButton("Làm mới nội dung", OnBootstrapClicked, "#17324D", "White"));
        var syncButton = CreateActionButton("Lưu cài đặt", OnSyncVisitorClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(syncButton, 1);
        syncRow.Add(syncButton);
        languageLayout.Add(syncRow);
        languageLayout.Add(new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.Status)));
        languageCard.Content = languageLayout;
        root.Add(languageCard);

        var visitorCard = CreateCard();
        var visitorLayout = new VerticalStackLayout { Spacing = 14 };
        visitorLayout.Add(new Label { Text = "Tùy chọn nghe và định vị", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });

        var optionRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 12
        };
        optionRow.Add(CreateSwitchCard("Tự động phát", "Tự phát khi bạn đến đúng điểm thuyết minh", nameof(MainViewModel.AllowAutoPlay)));
        var bgCard = CreateSwitchCard("Chạy nền", "Giữ định vị hoạt động để app nhắc đúng lúc", nameof(MainViewModel.AllowBackgroundTracking));
        Grid.SetColumn(bgCard, 1);
        optionRow.Add(bgCard);
        visitorLayout.Add(optionRow);
        visitorCard.Content = visitorLayout;
        root.Add(visitorCard);

        var permissionsCard = CreateCard();
        var permissionsLayout = new VerticalStackLayout { Spacing = 12 };
        permissionsLayout.Add(new Label { Text = "Quyền truy cập", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        permissionsLayout.Add(new Label { TextColor = Color.FromArgb("#445D75") }.Bind(Label.TextProperty, nameof(MainViewModel.LocationPermissionText)));
        permissionsLayout.Add(new Label { TextColor = Color.FromArgb("#445D75") }.Bind(Label.TextProperty, nameof(MainViewModel.BackgroundPermissionText)));
        permissionsLayout.Add(new Label { TextColor = Color.FromArgb("#445D75") }.Bind(Label.TextProperty, nameof(MainViewModel.CameraPermissionText)));
        permissionsLayout.Add(new Label { TextColor = Color.FromArgb("#445D75") }.Bind(Label.TextProperty, nameof(MainViewModel.NotificationPermissionText)));

        var permissionRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        permissionRow.Add(CreateActionButton("Cấp quyền định vị", OnRequestTrackingPermissionsClicked, "#17324D", "White"));
        var cameraButton = CreateActionButton("Cấp quyền camera", OnRequestCameraPermissionClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(cameraButton, 1);
        permissionRow.Add(cameraButton);
        permissionsLayout.Add(permissionRow);

        var permissionRow2 = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        permissionRow2.Add(CreateActionButton("Kiểm tra lại", OnRefreshPermissionsClicked, "#EEF3F8", "#17324D"));
        var settingsButton = CreateActionButton("Mở cài đặt hệ thống", OnOpenSystemSettingsClicked, "#F3F7FB", "#17324D");
        Grid.SetColumn(settingsButton, 1);
        permissionRow2.Add(settingsButton);
        permissionsLayout.Add(permissionRow2);

        permissionsCard.Content = permissionsLayout;
        root.Add(permissionsCard);

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
            BackgroundColor = Color.FromArgb(backgroundColor),
            TextColor = textColor.Equals("White", StringComparison.OrdinalIgnoreCase) ? Colors.White : Color.FromArgb(textColor),
            CornerRadius = 18
        };
        button.Clicked += handler;
        return button;
    }
}
