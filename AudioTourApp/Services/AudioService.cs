using Plugin.Maui.Audio;

namespace AudioTourApp.Services;

public class AudioService
{
    private IAudioPlayer _player;
    private readonly IAudioManager _audioManager;

    public AudioService()
    {
        _audioManager = AudioManager.Current;
    }

    public async Task PlayAsync(string url)
    {
        try
        {
            // 🔥 dừng audio cũ
            _player?.Stop();

            using var httpClient = new HttpClient();
            var stream = await httpClient.GetStreamAsync(url);

            _player = _audioManager.CreatePlayer(stream);
            _player.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }
}