using System.Collections;
using AudioTourApp.ViewModels;
using BarcodeScanning;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class QrScannerPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly CameraView _cameraView;
    private readonly Label _statusLabel;
    private bool _isHandlingScan;

    public QrScannerPage(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        Title = "Scan QR";
        BackgroundColor = Color.FromArgb("#081320");

        _cameraView = new CameraView
        {
            CameraEnabled = false,
            VibrationOnDetected = true,
            TapToFocusEnabled = true,
            ViewfinderMode = true,
            AimMode = false,
            HeightRequest = 460
        };
        _cameraView.OnDetectionFinishedCommand = new Command<object>(async result => await OnDetectionFinishedAsync(result));

        _statusLabel = new Label
        {
            Text = "Dua ma QR vao khung camera de mo ngay noi dung POI.",
            TextColor = Color.FromArgb("#DCE9F5"),
            HorizontalTextAlignment = TextAlignment.Center
        };

        Content = BuildContent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isHandlingScan = false;
        await _viewModel.RefreshPermissionStatusAsync();

        if (!_viewModel.CanUseCamera)
        {
            _statusLabel.Text = "App chua co quyen camera. Hay cap quyen roi quay lai man hinh nay.";
            return;
        }

        _statusLabel.Text = "Dang mo camera...";
        _cameraView.CameraEnabled = true;
    }

    protected override void OnDisappearing()
    {
        _cameraView.CameraEnabled = false;
        base.OnDisappearing();
    }

    private View BuildContent()
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        root.Add(new VerticalStackLayout
        {
            Padding = new Thickness(18, 18, 18, 12),
            Spacing = 10,
            Children =
            {
                new Label
                {
                    Text = "Quet ma QR",
                    FontSize = 28,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White
                },
                _statusLabel
            }
        });

        var frame = new Border
        {
            Margin = new Thickness(18, 0, 18, 0),
            Padding = 0,
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#101D2A"),
            StrokeShape = new RoundRectangle { CornerRadius = 28 },
            Content = new Grid
            {
                Children =
                {
                    _cameraView,
                    new Border
                    {
                        Stroke = Color.FromArgb("#E4B43C"),
                        StrokeThickness = 3,
                        BackgroundColor = Colors.Transparent,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        WidthRequest = 240,
                        HeightRequest = 240,
                        StrokeShape = new RoundRectangle { CornerRadius = 22 }
                    }
                }
            }
        };
        Grid.SetRow(frame, 1);
        root.Add(frame);

        var actions = new Grid
        {
            Padding = new Thickness(18, 12, 18, 22),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };

        actions.Add(CreateButton("Bat/Tat den", OnToggleTorchClicked, "#E4B43C", "#17324D"));
        var refreshButton = CreateButton("Kiem tra quyen", OnRefreshPermissionClicked, "#EEF3F8", "#17324D");
        Grid.SetColumn(refreshButton, 1);
        actions.Add(refreshButton);
        var closeButton = CreateButton("Dong", OnCloseClicked, "#17324D", "White");
        Grid.SetColumn(closeButton, 2);
        actions.Add(closeButton);
        Grid.SetRow(actions, 2);
        root.Add(actions);

        return root;
    }

    private async Task OnDetectionFinishedAsync(object? payload)
    {
        if (_isHandlingScan)
        {
            return;
        }

        var code = ExtractFirstBarcodeValue(payload);
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        _isHandlingScan = true;
        _cameraView.CameraEnabled = false;
        _statusLabel.Text = $"Da quet: {code}. Dang mo noi dung...";

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await _viewModel.LookupQrAsync(code);
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Khong mo duoc QR: {ex.Message}";
                _cameraView.CameraEnabled = true;
                _isHandlingScan = false;
            }
        });
    }

    private static string? ExtractFirstBarcodeValue(object? payload)
    {
        var results = payload?.GetType().GetProperty("BarcodeResults")?.GetValue(payload) as IEnumerable;
        if (results is null)
        {
            return null;
        }

        foreach (var result in results)
        {
            var raw = result?.GetType().GetProperty("RawValue")?.GetValue(result)?.ToString();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                return raw;
            }

            var display = result?.GetType().GetProperty("DisplayValue")?.GetValue(result)?.ToString();
            if (!string.IsNullOrWhiteSpace(display))
            {
                return display;
            }
        }

        return null;
    }

    private void OnToggleTorchClicked(object? sender, EventArgs e)
    {
        _cameraView.TorchOn = !_cameraView.TorchOn;
        _statusLabel.Text = _cameraView.TorchOn ? "Da bat den flash." : "Da tat den flash.";
    }

    private async void OnRefreshPermissionClicked(object? sender, EventArgs e)
    {
        await _viewModel.RequestCameraPermissionAsync();
        await _viewModel.RefreshPermissionStatusAsync();

        if (_viewModel.CanUseCamera)
        {
            _statusLabel.Text = "Da co quyen camera. Dang mo camera...";
            _cameraView.CameraEnabled = true;
        }
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private static Button CreateButton(string text, EventHandler handler, string backgroundColor, string textColor)
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
