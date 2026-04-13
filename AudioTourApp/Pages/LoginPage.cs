using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class LoginPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public LoginPage(MainViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        Title = "Đăng nhập";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        Content = BuildContent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var notice = _viewModel.TakePendingUserNotice();
        if (!string.IsNullOrWhiteSpace(notice))
        {
            await DisplayAlert("Thông báo", notice, "OK");
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        try
        {
            await _viewModel.LoginAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Đăng nhập chưa thành công", ex.Message, "OK");
        }
    }

    private async void OnOpenRegistrationClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistrationPage(_viewModel));
    }

    private View BuildContent()
    {
        var root = new VerticalStackLayout
        {
            Padding = new Thickness(20, 30, 20, 30),
            Spacing = 20
        };

        root.Add(new Border
        {
            StrokeThickness = 0,
            Padding = 22,
            StrokeShape = new RoundRectangle { CornerRadius = 28 },
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new(Color.FromArgb("#17324D"), 0f),
                    new(Color.FromArgb("#2B6F8F"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label
                    {
                        Text = "Thuyết minh phố ẩm thực Vĩnh Khánh",
                        FontSize = 28,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White
                    },
                    new Label
                    {
                        Text = "Đăng nhập để bắt đầu nghe thuyết minh, dùng bản đồ và quét QR tại các điểm dừng.",
                        TextColor = Color.FromArgb("#E4EEF7")
                    }
                }
            }
        });

        var loginCard = new Border
        {
            Stroke = Color.FromArgb("#D9E3EE"),
            BackgroundColor = Colors.White,
            Padding = 18,
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Content = new VerticalStackLayout
            {
                Spacing = 14,
                Children =
                {
                    new Label
                    {
                        Text = "Đăng nhập tài khoản",
                        FontSize = 22,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#17324D")
                    },
                    CreateEntry("Số điện thoại hoặc email", nameof(MainViewModel.LoginIdentifier), false),
                    CreateEntry("Mật khẩu", nameof(MainViewModel.LoginPassword), true),
                    new Label
                    {
                        TextColor = Color.FromArgb("#667C92"),
                        FontSize = 13
                    }.Bind(Label.TextProperty, nameof(MainViewModel.Status)),
                    new Button
                    {
                        Text = "Đăng nhập",
                        BackgroundColor = Color.FromArgb("#17324D"),
                        TextColor = Colors.White,
                        CornerRadius = 18
                    },
                    new Button
                    {
                        Text = "Chưa có tài khoản? Đăng ký và thanh toán",
                        BackgroundColor = Color.FromArgb("#E4B43C"),
                        TextColor = Color.FromArgb("#17324D"),
                        CornerRadius = 18
                    }
                }
            }
        };

        var stack = (VerticalStackLayout)loginCard.Content;
        ((Button)stack.Children[4]).Clicked += OnLoginClicked;
        ((Button)stack.Children[5]).Clicked += OnOpenRegistrationClicked;

        root.Add(loginCard);

        root.Add(new Border
        {
            StrokeThickness = 0,
            Padding = 16,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            BackgroundColor = Color.FromArgb("#EEF3F8"),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "Quy trình sử dụng", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") },
                    new Label { Text = "1. Đăng ký tài khoản mới.", TextColor = Color.FromArgb("#4F657C") },
                    new Label { Text = "2. Chọn gói tối thiểu 20.000đ và thanh toán bằng QR/payOS.", TextColor = Color.FromArgb("#4F657C") },
                    new Label { Text = "3. Sau khi admin/API xác nhận thanh toán, quay lại đăng nhập để dùng app.", TextColor = Color.FromArgb("#4F657C") }
                }
            }
        });

        return new ScrollView { Content = root };
    }

    private static Entry CreateEntry(string placeholder, string bindingPath, bool isPassword)
    {
        var entry = new Entry
        {
            Placeholder = placeholder,
            IsPassword = isPassword,
            BackgroundColor = Color.FromArgb("#F8FBFF"),
            TextColor = Color.FromArgb("#17324D")
        };
        entry.SetBinding(Entry.TextProperty, bindingPath, BindingMode.TwoWay);
        return entry;
    }
}
