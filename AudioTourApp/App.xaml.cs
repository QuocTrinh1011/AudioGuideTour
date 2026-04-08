using AudioTourApp.Services;
using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls;

namespace AudioTourApp;

public class App : Application
{
    private readonly Page _rootPage;
    private readonly MainWindowLifecycle _lifecycle;

    public App(AppShell shell, MainViewModel mainViewModel, AudioQueueService audioQueueService, LocationTrackingService locationTrackingService)
    {
        Resources = BuildResources();
        _rootPage = shell;
        MainPage = _rootPage;
        _lifecycle = new MainWindowLifecycle(mainViewModel, audioQueueService, locationTrackingService);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return _lifecycle.CreateWindow(_rootPage);
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
