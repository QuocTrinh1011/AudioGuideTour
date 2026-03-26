using AudioTourApp.Models;
using Microsoft.Maui.Media;
using System.Collections.Concurrent;

namespace AudioTourApp.Services;

public class AudioQueueService
{
    private readonly ConcurrentQueue<PoiItem> _queue = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<int, DateTime> _recentPlay = new();
    private bool _processing;

    public event EventHandler<string>? StatusChanged;

    public IReadOnlyCollection<PoiItem> PendingItems => _queue.ToArray();

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
        StatusChanged?.Invoke(this, $"Da them {poi.Title} vao hang doi.");
        await ProcessQueueAsync(cancellationToken);
    }

    public Task StopAsync()
    {
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
                StatusChanged?.Invoke(this, $"Dang phat {poi.Title}...");

                if (!string.IsNullOrWhiteSpace(poi.TtsScript))
                {
                    var options = new SpeechOptions
                    {
                        Locale = await ResolveLocaleAsync(poi.Language, cancellationToken)
                    };

                    await TextToSpeech.Default.SpeakAsync(poi.TtsScript, options, cancellationToken);
                }
                else if (!string.IsNullOrWhiteSpace(poi.Description))
                {
                    var options = new SpeechOptions
                    {
                        Locale = await ResolveLocaleAsync(poi.Language, cancellationToken)
                    };

                    await TextToSpeech.Default.SpeakAsync(poi.Description, options, cancellationToken);
                }

                _recentPlay[poi.Id] = DateTime.UtcNow;
                StatusChanged?.Invoke(this, $"Da xong {poi.Title}.");
            }
        }
        finally
        {
            _processing = false;
            _gate.Release();
        }
    }

    private static async Task<Locale?> ResolveLocaleAsync(string language, CancellationToken cancellationToken)
    {
        var locales = await TextToSpeech.Default.GetLocalesAsync();
        return locales.FirstOrDefault(x => x.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
            ?? locales.FirstOrDefault(x => x.Language.StartsWith(language.Split('-')[0], StringComparison.OrdinalIgnoreCase));
    }
}
