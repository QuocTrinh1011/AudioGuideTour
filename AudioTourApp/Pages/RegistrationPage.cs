using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class RegistrationPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public RegistrationPage(MainViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        Title = "Đăng ký";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        Content = BuildContent();
    }

    private async void OnContinueClicked(object? sender, EventArgs e)
    {
        await _viewModel.SubmitRegistrationFormAsync();
        if (_viewModel.HasCurrentRegistration)
        {
            await Navigation.PushAsync(new RegistrationPlanPage(_viewModel));
        }
    }

    private async void OnOpenPlansClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistrationPlanPage(_viewModel));
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
                    new(Color.FromArgb("#2E6B8A"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "Đăng ký người dùng", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = "Điền thông tin bắt buộc trước, sau đó chọn gói từ 20.000đ để hệ thống tạo thanh toán payOS.", TextColor = Color.FromArgb("#E3EEF7") }
                }
            }
        });

        var formCard = CreateCard();
        var formLayout = new VerticalStackLayout { Spacing = 12 };
        formLayout.Add(new Label { Text = "Thông tin đăng ký", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        formLayout.Add(CreateEntry("Họ và tên", nameof(MainViewModel.RegistrationFullName), Keyboard.Text));
        formLayout.Add(CreateEntry("Số điện thoại", nameof(MainViewModel.RegistrationPhone), Keyboard.Telephone));
        formLayout.Add(CreateEntry("Email", nameof(MainViewModel.RegistrationEmail), Keyboard.Email));
        formLayout.Add(CreatePasswordEntry("Tạo mật khẩu", nameof(MainViewModel.RegistrationPassword)));
        formLayout.Add(CreatePasswordEntry("Nhập lại mật khẩu", nameof(MainViewModel.RegistrationConfirmPassword)));
        formLayout.Add(CreateEditor("Ghi chú thêm (nếu có)", nameof(MainViewModel.RegistrationNote)));
        formLayout.Add(new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.RegistrationSummaryText)));
        formLayout.Add(new Label { TextColor = Color.FromArgb("#8AA0B6"), FontSize = 12 }.Bind(Label.TextProperty, nameof(MainViewModel.Status)));

        var buttonRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        buttonRow.Add(CreateActionButton("Tiếp tục chọn gói", OnContinueClicked, "#17324D", "White"));
        var openPlansButton = CreateActionButton("Xem gói đã chọn", OnOpenPlansClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(openPlansButton, 1);
        buttonRow.Add(openPlansButton);
        formLayout.Add(buttonRow);
        formCard.Content = formLayout;
        root.Add(formCard);

        return new ScrollView { Content = root };
    }

    private static Border CreateCard() => new()
    {
        Stroke = Color.FromArgb("#D9E3EE"),
        BackgroundColor = Colors.White,
        Padding = 16,
        StrokeShape = new RoundRectangle { CornerRadius = 24 }
    };

    private static Entry CreateEntry(string placeholder, string bindingPath, Keyboard keyboard)
    {
        var entry = new Entry
        {
            Placeholder = placeholder,
            Keyboard = keyboard,
            BackgroundColor = Color.FromArgb("#F8FBFF"),
            TextColor = Color.FromArgb("#17324D")
        };
        entry.SetBinding(Entry.TextProperty, bindingPath, BindingMode.TwoWay);
        return entry;
    }

    private static Entry CreatePasswordEntry(string placeholder, string bindingPath)
    {
        var entry = new Entry
        {
            Placeholder = placeholder,
            IsPassword = true,
            BackgroundColor = Color.FromArgb("#F8FBFF"),
            TextColor = Color.FromArgb("#17324D")
        };
        entry.SetBinding(Entry.TextProperty, bindingPath, BindingMode.TwoWay);
        return entry;
    }

    private static Editor CreateEditor(string placeholder, string bindingPath)
    {
        var editor = new Editor
        {
            Placeholder = placeholder,
            AutoSize = EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = 100,
            BackgroundColor = Color.FromArgb("#F8FBFF"),
            TextColor = Color.FromArgb("#17324D")
        };
        editor.SetBinding(Editor.TextProperty, bindingPath, BindingMode.TwoWay);
        return editor;
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
