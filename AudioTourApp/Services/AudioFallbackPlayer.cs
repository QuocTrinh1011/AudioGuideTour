#if ANDROID
using Android.Media;
#endif

namespace AudioTourApp.Services;

public class AudioFallbackPlayer
{
    private readonly ApiClient _apiClient;

#if ANDROID
    private MediaPlayer? _player;
#endif

    public AudioFallbackPlayer(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<bool> TryPlayAsync(string? audioUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            return false;
        }

#if ANDROID
        await StopAsync();

        var source = NormalizeAudioSource(audioUrl);

        _player = new MediaPlayer();
        _player.SetDataSource(source);
        _player.Prepare();
        _player.Start();
        return true;
#else
        return false;
#endif
    }

    public Task StopAsync()
    {
#if ANDROID
        if (_player != null)
        {
            if (_player.IsPlaying)
            {
                _player.Stop();
            }

            _player.Release();
            _player.Dispose();
            _player = null;
        }
#endif
        return Task.CompletedTask;
    }

    private string NormalizeAudioSource(string audioUrl)
    {
        if (Uri.TryCreate(audioUrl, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             absolute.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return absolute.ToString();
        }

        var baseUrl = string.IsNullOrWhiteSpace(_apiClient.BaseUrl)
            ? "http://10.0.2.2:5297"
            : _apiClient.BaseUrl.TrimEnd('/');

        return $"{baseUrl}/{audioUrl.TrimStart('/')}";
    }
}
