using AudioTourApp.Pages;

namespace AudioTourApp;

public class AppShell : Shell
{
    public AppShell(HomePage homePage, QrPage qrPage, MapPage mapPage, ToursPage toursPage)
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;

        var tabs = new TabBar();
        tabs.Items.Add(CreateShellContent("Trang chủ", "home", homePage));
        tabs.Items.Add(CreateShellContent("Bản đồ", "map", mapPage));
        tabs.Items.Add(CreateShellContent("QR", "qr", qrPage));
        tabs.Items.Add(CreateShellContent("Tour", "tours", toursPage));

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
