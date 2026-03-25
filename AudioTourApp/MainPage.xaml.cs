using AudioTourApp.Models;
using AudioTourApp.Services;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using AudioTourApp.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
namespace AudioTourApp;

public partial class MainPage : ContentPage
{
    LocationService locationService = new LocationService();
    GeofenceService geofenceService = new GeofenceService();
    AudioService audioService = new AudioService();
    public MainPage()
    {
        InitializeComponent();

        // 👉 fake dữ liệu POI
        geofenceService.SetPOIs(new List<POI>
        {
            new POI
            {
                Id = 1,
                Name = "Quán Ốc A",
                Latitude = 10.762622,
                Longitude = 106.660172,
                RadiusMeters = 50,
                Priority = 2,
                Description = "Quán ốc nổi tiếng"
            },
            new POI
            {
                Id = 2,
                Name = "Quán Nướng B",
                Latitude = 10.762800,
                Longitude = 106.660300,
                RadiusMeters = 40,
                Priority = 1,
                Description = "Quán nướng BBQ"
            }
        });

        geofenceService.OnPOIEnter += (poi) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // 🔊 phát audio
                await audioService.PlayAsync("URL_MP3");
            });
        };

        // 👉 EXIT: rời khỏi vùng
        geofenceService.OnPOIExit += (poi) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Rời khỏi: {poi.Name}");
            });
        };

        // 👉 NEAR: đến gần
        geofenceService.OnPOINear += (poi) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Đang đến gần: {poi.Name}");
            });
        };
    }

    private async void OnScanQRClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new QRScanPage());
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        if (status != PermissionStatus.Granted)
            return;

        locationService.OnLocationChanged += (loc) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblLocation.Text = $"{loc.Latitude} - {loc.Longitude}";
            });

            // 👉 check geofence
            geofenceService.CheckLocation(loc);
        };

        await locationService.StartTrackingAsync();
    }
}