using AudioTourApp.Models;
using System.Collections.Concurrent;

namespace AudioTourApp.Services;

public class AudioQueueService
{
    private readonly AudioFallbackPlayer _audioFallbackPlayer;
    private readonly AudioInterruptionService _audioInterruptionService;
    private readonly NarrationService _narrationService;
    private readonly ConcurrentQueue<PoiItem> _queue = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<int, DateTime> _recentPlay = new();
    private CancellationTokenSource? _playbackCts;
    private bool _processing;
    private PoiItem? _currentItem;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler? QueueChanged;

    public IReadOnlyCollection<PoiItem> PendingItems => _queue.ToArray();
    public PoiItem? CurrentItem => _currentItem;
    public bool IsPlaying => _currentItem != null;

    public AudioQueueService(AudioFallbackPlayer audioFallbackPlayer, AudioInterruptionService audioInterruptionService, NarrationService narrationService)
    {
        _audioFallbackPlayer = audioFallbackPlayer;
        _audioInterruptionService = audioInterruptionService;
        _narrationService = narrationService;
        _audioInterruptionService.Interrupted += async (_, reason) => await StopForInterruptionAsync(reason);
    }

    public async Task EnqueueAsync(PoiItem poi, CancellationToken cancellationToken = default)
    {
        if (_recentPlay.TryGetValue(poi.Id, out var lastPlay) &&
            (DateTime.UtcNow - lastPlay).TotalSeconds < Math.Max(poi.CooldownSeconds, 1))
        {
            StatusChanged?.Invoke(this, $"Bo qua {poi.Title} do dang cooldown.");
            return;
        }

        if (_queue.Any(x => x.Id == poi.Id))
        {
            StatusChanged?.Invoke(this, $"Hang doi da co {poi.Title}.");
            return;
        }

        _queue.Enqueue(poi);
        RaiseQueueChanged();
        StatusChanged?.Invoke(this, $"Da them {poi.Title} vao hang doi.");
        await ProcessQueueAsync(cancellationToken);
    }

    public Task StopAsync()
    {
        _playbackCts?.Cancel();
        _ = _narrationService.StopAsync();
        _ = _audioFallbackPlayer.StopAsync();
        _currentItem = null;
        RaiseQueueChanged();
        StatusChanged?.Invoke(this, "Dung phat audio hien tai.");
        return Task.CompletedTask;
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_processing)
            {
                return;
            }

            _processing = true;
            while (_queue.TryDequeue(out var poi))
            {
                _playbackCts?.Cancel();
                _playbackCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _currentItem = poi;
                RaiseQueueChanged();
                StatusChanged?.Invoke(this, $"Dang phat {poi.Title}...");

                var played = false;
                var lastFailure = "";
                var hasFocus = await _audioInterruptionService.BeginPlaybackAsync();

                if (!hasFocus)
                {
                    _currentItem = null;
                    RaiseQueueChanged();
                    StatusChanged?.Invoke(this, $"Khong lay duoc audio focus de phat {poi.Title}.");
                    continue;
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(poi.TtsScript))
                    {
                        var result = await _narrationService.SpeakAsync(poi.TtsScript, poi.Language, _playbackCts.Token);
                        played = result.Played;
                        lastFailure = result.Message;
                        if (played)
                        {
                            StatusChanged?.Invoke(this, $"Dang doc TTS {poi.Title} bang {poi.Language}.");
                        }
                    }

                    if (!played && !string.IsNullOrWhiteSpace(poi.Description))
                    {
                        var result = await _narrationService.SpeakAsync(poi.Description, poi.Language, _playbackCts.Token);
                        played = result.Played;
                        lastFailure = result.Message;
                        if (played)
                        {
                            StatusChanged?.Invoke(this, $"Dang doc mo ta du phong cho {poi.Title}.");
                        }
                    }

                    if (!played && !string.IsNullOrWhiteSpace(poi.AudioUrl))
                    {
                        played = await _audioFallbackPlayer.TryPlayAsync(poi.AudioUrl, _playbackCts.Token);
                        if (played)
                        {
                            StatusChanged?.Invoke(this, $"TTS khong dung duoc, dang phat audio du phong cho {poi.Title}.");
                        }
                    }
                }
                finally
                {
                    await _audioInterruptionService.EndPlaybackAsync();
                }

                _recentPlay[poi.Id] = DateTime.UtcNow;
                _currentItem = null;
                RaiseQueueChanged();
                StatusChanged?.Invoke(this, played
                    ? $"Da xong {poi.Title}."
                    : $"Khong the phat {poi.Title}. {lastFailure}".Trim());
            }
        }
        finally
        {
            _processing = false;
            _currentItem = null;
            RaiseQueueChanged();
            _gate.Release();
        }
    }
    private void RaiseQueueChanged()
    {
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task StopForInterruptionAsync(string reason)
    {
        _playbackCts?.Cancel();
        await _narrationService.StopAsync();
        await _audioFallbackPlayer.StopAsync();
        _currentItem = null;
        RaiseQueueChanged();
        StatusChanged?.Invoke(this, reason);
    }
}
