using AudioTourApp.Pages;
using AudioTourApp.Services;
using AudioTourApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace AudioTourApp;

public class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AppShell _shell;
    private readonly MainWindowLifecycle _lifecycle;
    private readonly MainViewModel _mainViewModel;
    private Window? _window;

    public App(IServiceProvider serviceProvider, AppShell shell, MainViewModel mainViewModel, AudioQueueService audioQueueService, LocationTrackingService locationTrackingService)
    {
        Resources = BuildResources();
        _serviceProvider = serviceProvider;
        _shell = shell;
        _mainViewModel = mainViewModel;
        _lifecycle = new MainWindowLifecycle(mainViewModel, audioQueueService, locationTrackingService);
        _mainViewModel.FlowStateChanged += OnFlowStateChanged;
        MainPage = BuildLoadingPage();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _window = _lifecycle.CreateWindow(MainPage ?? BuildLoadingPage());
        _ = InitializeAsync();
        return _window;
    }

    protected override async void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);

        if (uri == null)
        {
            return;
        }

        if (string.Equals(uri.Host, "registration", StringComparison.OrdinalIgnoreCase))
        {
            await _mainViewModel.HandleRegistrationAppLinkAsync(uri);
            return;
        }

        await _mainViewModel.LookupQrAsync(uri.AbsoluteUri);
    }

    private async Task InitializeAsync()
    {
        await _mainViewModel.RestoreCustomerSessionAsync();
        ApplyCurrentRoot();
    }

    private void OnFlowStateChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(ApplyCurrentRoot);
    }

    private void ApplyCurrentRoot()
    {
        var nextRoot = _mainViewModel.IsAuthenticated
            ? (Page)_shell
            : BuildLoggedOutRoot();

        if (_window != null)
        {
            _window.Page = nextRoot;
            return;
        }

        MainPage = nextRoot;
    }

    private Page BuildLoggedOutRoot()
    {
        var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
        return new NavigationPage(loginPage)
        {
            BarBackgroundColor = Color.FromArgb("#17324D"),
            BarTextColor = Colors.White
        };
    }

    private static Page BuildLoadingPage()
    {
        return new ContentPage
        {
            BackgroundColor = Color.FromArgb("#F6F8FB"),
            Content = new Grid
            {
                Children =
                {
                    new ActivityIndicator
                    {
                        IsRunning = true,
                        Color = Color.FromArgb("#17324D"),
                        WidthRequest = 48,
                        HeightRequest = 48,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                }
            }
        };
    }

    private static ResourceDictionary BuildResources()
    {
        var resources = new ResourceDictionary();

        resources.Add(new Style(typeof(ContentPage))
        {
            Setters =
            {
                new Setter { Property = ContentPage.BackgroundColorProperty, Value = Color.FromArgb("#F6F8FB") }
            }
        });

        resources.Add(new Style(typeof(Label))
        {
            Setters =
            {
                new Setter { Property = Label.TextColorProperty, Value = Color.FromArgb("#14273D") }
            }
        });

        resources.Add(new Style(typeof(Button))
        {
            Setters =
            {
                new Setter { Property = Button.CornerRadiusProperty, Value = 12 },
                new Setter { Property = Button.BackgroundColorProperty, Value = Color.FromArgb("#1F5FAA") },
                new Setter { Property = Button.TextColorProperty, Value = Colors.White },
                new Setter { Property = Button.PaddingProperty, Value = new Thickness(14, 10) }
            }
        });

        return resources;
    }
}

internal sealed class MainWindowLifecycle
{
    private readonly MainViewModel _mainViewModel;
    private readonly AudioQueueService _audioQueueService;
    private readonly LocationTrackingService _locationTrackingService;

    public MainWindowLifecycle(MainViewModel mainViewModel, AudioQueueService audioQueueService, LocationTrackingService locationTrackingService)
    {
        _mainViewModel = mainViewModel;
        _audioQueueService = audioQueueService;
        _locationTrackingService = locationTrackingService;
    }

    public Window CreateWindow(Page rootPage)
    {
        var window = new Window(rootPage);
        window.Activated += (_, _) => _mainViewModel.OnAppForegroundChanged(true);
        window.Resumed += (_, _) => _mainViewModel.OnAppForegroundChanged(true);
        window.Stopped += (_, _) => _mainViewModel.OnAppForegroundChanged(false);
        window.Destroying += (_, _) =>
        {
            _locationTrackingService.SetForegroundState(false);
            _ = _audioQueueService.StopAsync();
        };

        return window;
    }
}
