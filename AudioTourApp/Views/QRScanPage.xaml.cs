using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;

namespace AudioTourApp.Views;

public partial class QRScanPage : ContentPage
{
    public QRScanPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var scanPage = new ZXingScannerPage();

        scanPage.OnScanResult += (result) =>
        {
            scanPage.IsScanning = false;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Navigation.PopAsync();

                await DisplayAlert("QR", result.Text, "OK");
            });
        };

        await Navigation.PushAsync(scanPage);
    }
}