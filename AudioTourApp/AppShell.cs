using AudioTourApp.Pages;

namespace AudioTourApp;

public class AppShell : Shell
{
    private readonly IServiceProvider _serviceProvider;

    public AppShell(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        FlyoutBehavior = FlyoutBehavior.Disabled;

        var tabs = new TabBar();
        tabs.Items.Add(CreateShellContent("Trang chủ", "home", typeof(HomePage)));
        tabs.Items.Add(CreateShellContent("Bản đồ", "map", typeof(MapPage)));
        tabs.Items.Add(CreateShellContent("QR", "qr", typeof(QrPage)));
        tabs.Items.Add(CreateShellContent("Tour", "tours", typeof(ToursPage)));

        Items.Add(tabs);
    }

    private ShellContent CreateShellContent(string title, string route, Type pageType)
    {
        return new ShellContent
        {
            Title = title,
            Route = route,
            ContentTemplate = new DataTemplate(() => (Page)_serviceProvider.GetRequiredService(pageType))
        };
    }
}
