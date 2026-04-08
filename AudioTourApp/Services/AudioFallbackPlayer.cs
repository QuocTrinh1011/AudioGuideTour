#if ANDROID
using Android.Media;
#endif
using Microsoft.Maui.Storage;

namespace AudioTourApp.Services;

public class AudioFallbackPlayer
{
    private readonly ApiClient _apiClient;
    public string LastErrorMessage { get; private set; } = "";

#if ANDROID
    private MediaPlayer? _player;
    private TaskCompletionSource<bool>? _playbackCompletion;
    private TaskCompletionSource<bool>? _preparedCompletion;
    private string? _cachedAudioPath;
#endif

    public AudioFallbackPlayer(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<AudioFallbackPlaybackResult> TryPlayAsync(string? audioUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            LastErrorMessage = "Không có audio fallback URL.";
            return AudioFallbackPlaybackResult.Failed(LastErrorMessage);
        }

#if ANDROID
        try
        {
            await StopAsync();

            var source = await ResolvePlaybackSourceAsync(audioUrl, cancellationToken);
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var player = new MediaPlayer();
            _player = player;
            _playbackCompletion = completion;
            _preparedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var builder = new AudioAttributes.Builder();
            if (builder != null)
            {
                builder.SetUsage(AudioUsageKind.Media);
                builder.SetContentType(AudioContentType.Speech);
                var audioAttributes = builder.Build();
                if (audioAttributes != null)
                {
                    player.SetAudioAttributes(audioAttributes);
                }
            }
            player.Prepared += OnPlaybackPrepared;
            player.Completion += OnPlaybackCompleted;
            player.Error += OnPlaybackError;
            player.SetDataSource(source);
            player.PrepareAsync();

            using var ctr = cancellationToken.Register(() =>
            {
                LastErrorMessage = "Đã dừng audio fallback.";
                _preparedCompletion?.TrySetCanceled(cancellationToken);
                _playbackCompletion?.TrySetCanceled(cancellationToken);
                SafeReleasePlayer();
            });

            var prepared = await _preparedCompletion.Task.WaitAsync(cancellationToken);
            if (!prepared)
            {
                if (string.IsNullOrWhiteSpace(LastErrorMessage))
                {
                    LastErrorMessage = "Không chuẩn bị được audio fallback.";
                }

                return AudioFallbackPlaybackResult.Failed(LastErrorMessage);
            }

            var completed = await completion.Task.WaitAsync(cancellationToken);
            LastErrorMessage = completed ? string.Empty : LastErrorMessage;
            return completed
                ? AudioFallbackPlaybackResult.Success()
                : AudioFallbackPlaybackResult.Interrupted(LastErrorMessage);
        }
        catch (OperationCanceledException)
        {
            if (string.IsNullOrWhiteSpace(LastErrorMessage))
            {
                LastErrorMessage = "Đã dừng audio fallback.";
            }

            return AudioFallbackPlaybackResult.Interrupted(LastErrorMessage);
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.GetBaseException().Message;
            await StopAsync();
            return AudioFallbackPlaybackResult.Failed(LastErrorMessage);
        }
        finally
        {
            SafeReleasePlayer();
            CleanupCachedAudio();
        }
#else
        LastErrorMessage = "Nền tảng hiện tại chưa hỗ trợ audio fallback native.";
        return AudioFallbackPlaybackResult.Failed(LastErrorMessage);
#endif
    }

    public Task StopAsync()
    {
#if ANDROID
        if (string.IsNullOrWhiteSpace(LastErrorMessage))
        {
            LastErrorMessage = "Đã dừng audio fallback.";
        }

        _playbackCompletion?.TrySetCanceled();
        SafeReleasePlayer();
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

    private async Task<string> ResolvePlaybackSourceAsync(string audioUrl, CancellationToken cancellationToken)
    {
        var normalizedSource = NormalizeAudioSource(audioUrl);
        if (!Uri.TryCreate(normalizedSource, UriKind.Absolute, out var absolute) ||
            !(absolute.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
              absolute.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return normalizedSource;
        }

        var download = await _apiClient.DownloadFileToCacheAsync(normalizedSource, "audio-tour", cancellationToken);
        _cachedAudioPath = download.LocalPath;
        return download.LocalPath;
    }

#if ANDROID
    private void OnPlaybackPrepared(object? sender, EventArgs e)
    {
        try
        {
            _player?.Start();
            _preparedCompletion?.TrySetResult(true);
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.GetBaseException().Message;
            _preparedCompletion?.TrySetException(ex);
        }
    }

    private void OnPlaybackCompleted(object? sender, EventArgs e)
    {
        _playbackCompletion?.TrySetResult(true);
    }

    private void OnPlaybackError(object? sender, MediaPlayer.ErrorEventArgs e)
    {
        LastErrorMessage = $"MediaPlayer loi: {e.What}";
        e.Handled = true;
        _preparedCompletion?.TrySetResult(false);
        _playbackCompletion?.TrySetResult(false);
    }

    private void SafeReleasePlayer()
    {
        if (_player == null)
        {
            return;
        }

        try
        {
            if (_player.IsPlaying)
            {
                _player.Stop();
            }
        }
        catch
        {
        }

        _player.Prepared -= OnPlaybackPrepared;
        _player.Completion -= OnPlaybackCompleted;
        _player.Error -= OnPlaybackError;
        _player.Release();
        _player.Dispose();
        _player = null;
        _preparedCompletion = null;
        _playbackCompletion = null;
    }

    private void CleanupCachedAudio()
    {
        if (string.IsNullOrWhiteSpace(_cachedAudioPath))
        {
            return;
        }

        try
        {
            if (File.Exists(_cachedAudioPath))
            {
                File.Delete(_cachedAudioPath);
            }
        }
        catch
        {
        }

        _cachedAudioPath = null;
    }
#endif
}

public readonly record struct AudioFallbackPlaybackResult(bool Played, bool WasCompleted, string Message)
{
    public static AudioFallbackPlaybackResult Success(string message = "Đã phát xong audio fallback.")
        => new(true, true, message);

    public static AudioFallbackPlaybackResult Interrupted(string message)
        => new(true, false, message);

    public static AudioFallbackPlaybackResult Failed(string message)
        => new(false, false, message);
}
