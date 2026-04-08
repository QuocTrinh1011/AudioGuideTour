using AudioTourApp.Pages;

namespace AudioTourApp;

public class AppShell : Shell
{
    public AppShell(HomePage homePage, PoiPage poiPage, QrPage qrPage, MapPage mapPage, ToursPage toursPage, SettingsPage settingsPage)
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;

        var tabs = new TabBar();
        tabs.Items.Add(CreateShellContent("Home", "home", homePage));
        tabs.Items.Add(CreateShellContent("POI", "poi", poiPage));
        tabs.Items.Add(CreateShellContent("QR", "qr", qrPage));
        tabs.Items.Add(CreateShellContent("Map", "map", mapPage));
        tabs.Items.Add(CreateShellContent("Tours", "tours", toursPage));
        tabs.Items.Add(CreateShellContent("Settings", "settings", settingsPage));

        Items.Add(tabs);
    }

    private static ShellContent CreateShellContent(string title, string route, Page page)
    {
        return new ShellContent
        {
            Title = title,
            Route = route,
            Content = page
        };
    }
}
