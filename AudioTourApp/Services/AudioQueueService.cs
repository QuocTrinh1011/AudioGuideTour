using AudioTourApp.Models;
using System.Collections.Concurrent;

namespace AudioTourApp.Services;

public class AudioQueueService
{
    private readonly ApiClient _apiClient;
    private readonly AudioFallbackPlayer _audioFallbackPlayer;
    private readonly AudioInterruptionService _audioInterruptionService;
    private readonly NarrationService _narrationService;
    private readonly ConcurrentQueue<AudioPlaybackRequest> _queue = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<int, DateTime> _recentPlay = new();
    private CancellationTokenSource? _playbackCts;
    private bool _processing;
    private AudioPlaybackRequest? _currentRequest;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler? QueueChanged;

    public IReadOnlyCollection<PoiItem> PendingItems => _queue.Select(x => x.Poi).ToArray();
    public PoiItem? CurrentItem => _currentRequest?.Poi;
    public bool IsPlaying => _currentRequest != null;

    public AudioQueueService(ApiClient apiClient, AudioFallbackPlayer audioFallbackPlayer, AudioInterruptionService audioInterruptionService, NarrationService narrationService)
    {
        _apiClient = apiClient;
        _audioFallbackPlayer = audioFallbackPlayer;
        _audioInterruptionService = audioInterruptionService;
        _narrationService = narrationService;
        _audioInterruptionService.Interrupted += async (_, reason) => await StopForInterruptionAsync(reason);
    }

    public Task EnqueueAsync(PoiItem poi, CancellationToken cancellationToken = default)
        => EnqueueAsync(new AudioPlaybackRequest
        {
            Poi = poi,
            Language = poi.Language
        }, cancellationToken);

    public async Task EnqueueAsync(AudioPlaybackRequest request, CancellationToken cancellationToken = default)
    {
        var poi = request.Poi;
        if (_recentPlay.TryGetValue(poi.Id, out var lastPlay) &&
            (DateTime.UtcNow - lastPlay).TotalSeconds < Math.Max(poi.CooldownSeconds, 1))
        {
            StatusChanged?.Invoke(this, $"Bỏ qua {poi.Title} do đang cooldown.");
            return;
        }

        if (_currentRequest?.Poi.Id == poi.Id || _queue.Any(x => x.Poi.Id == poi.Id))
        {
            StatusChanged?.Invoke(this, $"Hàng đợi đã có hoặc đang phát {poi.Title}.");
            return;
        }

        _queue.Enqueue(request);
        RaiseQueueChanged();
        StatusChanged?.Invoke(this, $"Đã thêm {poi.Title} vào hàng đợi.");
        await ProcessQueueAsync(cancellationToken);
    }

    public Task StopAsync()
    {
        _playbackCts?.Cancel();
        _queue.Clear();
        _ = _narrationService.StopAsync();
        _ = _audioFallbackPlayer.StopAsync();
        _currentRequest = null;
        RaiseQueueChanged();
        StatusChanged?.Invoke(this, "Đã dừng phát audio và xóa hàng đợi.");
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
            while (_queue.TryDequeue(out var request))
            {
                var poi = request.Poi;
                _playbackCts?.Cancel();
                _playbackCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _currentRequest = request;
                RaiseQueueChanged();
                StatusChanged?.Invoke(this, $"Đang phat {poi.Title}...");

                var played = false;
                var wasCompleted = false;
                var playbackMode = !string.IsNullOrWhiteSpace(poi.AudioUrl) ? "audio" : "tts";
                var failureReasons = new List<string>();
                var playbackStartedAt = DateTime.UtcNow;
                var hasFocus = await _audioInterruptionService.BeginPlaybackAsync();
                var audioMode = poi.AudioMode?.Trim() ?? string.Empty;
                var audioOnly = string.Equals(audioMode, "audio", StringComparison.OrdinalIgnoreCase);
                var preferAudioFirst = !string.IsNullOrWhiteSpace(poi.AudioUrl) &&
                    (string.Equals(audioMode, "audio", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(audioMode, "audio-priority", StringComparison.OrdinalIgnoreCase));

                if (!hasFocus)
                {
                    _currentRequest = null;
                    RaiseQueueChanged();
                    StatusChanged?.Invoke(this, $"Không lấy được audio focus để phát {poi.Title}.");
                    continue;
                }

                try
                {
                    if (preferAudioFirst)
                    {
                        var result = await _audioFallbackPlayer.TryPlayAsync(poi.AudioUrl, _playbackCts.Token);
                        played = result.Played;
                        wasCompleted = result.WasCompleted;
                        playbackMode = "audio";
                        if (played)
                        {
                            StatusChanged?.Invoke(this, $"Đang phát audio ưu tiên cho {poi.Title}.");
                        }
                        else if (!string.IsNullOrWhiteSpace(result.Message))
                        {
                            failureReasons.Add($"Audio: {result.Message}");
                            StatusChanged?.Invoke(this, $"Audio cua {poi.Title} gap loi. {(audioOnly ? "Không có fallback TTS cho POI này." : "Đang thử TTS dự phòng...")}");
                        }
                    }

                    var ttsText = BuildTtsText(poi, out var ttsSourceLabel);

                    if (!played && !audioOnly && !string.IsNullOrWhiteSpace(ttsText))
                    {
                        var result = await _narrationService.SpeakAsync(ttsText, poi.Language, poi.VoiceName, _playbackCts.Token);
                        played = result.Played;
                        wasCompleted = result.WasCompleted;
                        playbackMode = "tts";
                        if (played)
                        {
                            StatusChanged?.Invoke(this, $"Đang doc {ttsSourceLabel} cho {poi.Title} bang {poi.Language}.");
                        }
                        else if (!string.IsNullOrWhiteSpace(result.Message))
                        {
                            failureReasons.Add($"TTS: {result.Message}");
                        }
                    }

                    if (!played && !preferAudioFirst && !string.IsNullOrWhiteSpace(poi.AudioUrl))
                    {
                        var result = await _audioFallbackPlayer.TryPlayAsync(poi.AudioUrl, _playbackCts.Token);
                        played = result.Played;
                        wasCompleted = result.WasCompleted;
                        playbackMode = "audio";
                        if (played)
                        {
                            StatusChanged?.Invoke(this, $"TTS không dùng được, đang phát audio dự phòng cho {poi.Title}.");
                        }
                        else if (!string.IsNullOrWhiteSpace(result.Message))
                        {
                            failureReasons.Add($"Audio: {result.Message}");
                        }
                    }
                }
                finally
                {
                    await _audioInterruptionService.EndPlaybackAsync();
                }

                if (played)
                {
                    _recentPlay[poi.Id] = DateTime.UtcNow;
                }

                _currentRequest = null;
                RaiseQueueChanged();

                await SaveVisitAsync(request, playbackStartedAt, DateTime.UtcNow, played, wasCompleted, playbackMode);
                var lastFailure = failureReasons.Count == 0
                    ? string.Empty
                    : string.Join(" | ", failureReasons.Distinct());
                StatusChanged?.Invoke(this, played
                    ? wasCompleted
                        ? $"Đã xong {poi.Title}."
                        : $"Đã dừng giữa chừng {poi.Title}."
                    : $"Không the phat {poi.Title}. {lastFailure}".Trim());
            }
        }
        finally
        {
            _processing = false;
            _currentRequest = null;
            RaiseQueueChanged();
            _gate.Release();
        }
    }
    private void RaiseQueueChanged()
    {
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string? BuildTtsText(PoiItem poi, out string sourceLabel)
    {
        if (!string.IsNullOrWhiteSpace(poi.TtsScript))
        {
            sourceLabel = "script TTS";
            return poi.TtsScript;
        }

        if (!string.IsNullOrWhiteSpace(poi.Description))
        {
            sourceLabel = "mô tả dự phòng";
            return poi.Description;
        }

        if (!string.IsNullOrWhiteSpace(poi.Summary))
        {
            sourceLabel = "tóm tắt dự phòng";
            return poi.Summary;
        }

        sourceLabel = "TTS";
        return null;
    }

    private async Task StopForInterruptionAsync(string reason)
    {
        _playbackCts?.Cancel();
        _queue.Clear();
        await _narrationService.StopAsync();
        await _audioFallbackPlayer.StopAsync();
        _currentRequest = null;
        RaiseQueueChanged();
        StatusChanged?.Invoke(this, $"{reason} Hang doi audio da duoc dung.");
    }

    private async Task SaveVisitAsync(AudioPlaybackRequest request, DateTime startedAt, DateTime endedAt, bool played, bool wasCompleted, string playbackMode)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return;
        }

        var duration = Math.Max((int)(endedAt - startedAt).TotalSeconds, 0);
        if (!played && duration <= 0)
        {
            return;
        }

        try
        {
            await _apiClient.SaveVisitAsync(new VisitHistoryRequest
            {
                UserId = request.UserId,
                PoiId = request.Poi.Id,
                Language = string.IsNullOrWhiteSpace(request.Language) ? request.Poi.Language : request.Language,
                StartTime = startedAt,
                EndTime = endedAt,
                Duration = duration,
                TriggerType = string.IsNullOrWhiteSpace(request.TriggerType) ? "manual" : request.TriggerType,
                PlaybackMode = string.IsNullOrWhiteSpace(playbackMode) ? "tts" : playbackMode,
                WasAutoPlayed = request.WasAutoPlayed,
                WasCompleted = wasCompleted,
                ActivationDistanceMeters = request.Poi.DistanceMeters
            });
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Không lưu được lịch sử nghe cho {request.Poi.Title}: {ex.Message}");
        }
    }
}
