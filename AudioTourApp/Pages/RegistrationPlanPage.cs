using AudioTourApp.Models;
using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace AudioTourApp.Pages;

public class RegistrationPlanPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public RegistrationPlanPage(MainViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        Title = "Chọn gói";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        Content = BuildContent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshRegistrationBootstrapAsync();
    }

    private async void OnCreatePaymentClicked(object? sender, EventArgs e)
    {
        await _viewModel.CreateRegistrationPaymentAsync();
    }

    private async void OnOpenCheckoutClicked(object? sender, EventArgs e)
    {
        await _viewModel.OpenRegistrationCheckoutAsync();
    }

    private async void OnRefreshPaymentClicked(object? sender, EventArgs e)
    {
        await _viewModel.RefreshRegistrationPaymentAsync();
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
                    new(Color.FromArgb("#4C2C19"), 0f),
                    new(Color.FromArgb("#D48B25"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "Chọn gói đăng ký", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = "Hệ thống yêu cầu chọn gói tối thiểu 20.000đ trước khi xác nhận đăng ký thành công.", TextColor = Color.FromArgb("#FFF2DC") }
                }
            }
        });

        var summaryCard = CreateCard();
        var summaryLayout = new VerticalStackLayout { Spacing = 8 };
        summaryLayout.Add(new Label { Text = "Trạng thái hồ sơ", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });
        summaryLayout.Add(new Label { TextColor = Color.FromArgb("#17324D"), FontAttributes = FontAttributes.Bold }.Bind(Label.TextProperty, nameof(MainViewModel.RegistrationSummaryText)));
        summaryLayout.Add(new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.RegistrationPaymentStatusText)));
        summaryLayout.Add(new Label { TextColor = Color.FromArgb("#8AA0B6"), FontSize = 12 }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedRegistrationPlanPriceText)));
        summaryCard.Content = summaryLayout;
        root.Add(summaryCard);

        var plansCard = CreateCard();
        var plansLayout = new VerticalStackLayout { Spacing = 12 };
        plansLayout.Add(new Label { Text = "Danh sách gói", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") });

        var plansCollection = new CollectionView { SelectionMode = SelectionMode.Single };
        plansCollection.SetBinding(ItemsView.ItemsSourceProperty, nameof(MainViewModel.RegistrationPlans));
        plansCollection.SetBinding(SelectableItemsView.SelectedItemProperty, nameof(MainViewModel.SelectedRegistrationPlan), BindingMode.TwoWay);
        plansCollection.ItemTemplate = new DataTemplate(() =>
        {
            var card = new Border
            {
                Stroke = Color.FromArgb("#E2EAF3"),
                StrokeThickness = 1,
                BackgroundColor = Colors.White,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = 14,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var badge = new Border
            {
                BackgroundColor = Color.FromArgb("#17324D"),
                StrokeThickness = 0,
                Padding = new Thickness(10, 4),
                StrokeShape = new RoundRectangle { CornerRadius = 999 },
                HorizontalOptions = LayoutOptions.Start,
                Content = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
                    .Bind(Label.TextProperty, nameof(RegistrationPlanItem.HighlightText))
            };

            var stack = new VerticalStackLayout { Spacing = 6 };
            stack.Add(badge);
            stack.Add(new Label { FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") }
                .Bind(Label.TextProperty, nameof(RegistrationPlanItem.Name)));
            stack.Add(new Label { TextColor = Color.FromArgb("#667C92") }
                .Bind(Label.TextProperty, nameof(RegistrationPlanItem.Description)));
            stack.Add(new Label { FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#C97732") }
                .Bind(Label.TextProperty, nameof(RegistrationPlanItem.Price), stringFormat: "{0:N0} VND"));
            stack.Add(new Label { TextColor = Color.FromArgb("#8AA0B6"), FontSize = 12 }
                .Bind(Label.TextProperty, nameof(RegistrationPlanItem.DurationDays), stringFormat: "Hiệu lực {0} ngày"));

            card.Content = stack;
            return card;
        });

        plansLayout.Add(plansCollection);
        var buttonRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        buttonRow.Add(CreateActionButton("Tạo thanh toán", OnCreatePaymentClicked, "#17324D", "White"));
        var checkoutButton = CreateActionButton("Mở payOS", OnOpenCheckoutClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(checkoutButton, 1);
        buttonRow.Add(checkoutButton);
        plansLayout.Add(buttonRow);
        plansLayout.Add(CreateActionButton("Tôi đã thanh toán / Kiểm tra lại", OnRefreshPaymentClicked, "#EEF3F8", "#17324D"));
        plansCard.Content = plansLayout;
        root.Add(plansCard);

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
